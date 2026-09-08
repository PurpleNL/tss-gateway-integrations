using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TSS.Gateway.Sdk.Requests;

// ReSharper disable UnusedMember.Global

namespace TSS.Gateway.Sdk
{
    // The queries side of a GatewayClient: publishes read queries on tss-api-publish and awaits the
    // player's response on tss-response-listen, matched by correlation id, over an own channel. Each
    // client binds a unique reply queue so two clients never share responses. Publishes are
    // serialized because a RabbitMQ channel does not support concurrent use, so callers may fire
    // queries concurrently. An error response throws a GatewayException.
    public class GatewayQueries
    {
        private const string RequestExchange = "tss-api-publish";
        private const string ResponseExchange = "tss-response-listen";
        private const string ContentType = "application/x-protobuf";
        private const string ReplyToHeader = "tss-reply-to";
        private const string Expiration = "15000";

        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
        private static readonly Empty EmptyMessage = new Empty();

        private readonly IChannel _channel;
        private readonly SemaphoreSlim _publishLock = new SemaphoreSlim(1, 1);
        private readonly string _routingPrefix;
        private readonly string _replyTo;
        private readonly Action<string>? _onDiagnosticMessage;

        private readonly ConcurrentDictionary<string, TaskCompletionSource<byte[]>> _pending = new ConcurrentDictionary<string, TaskCompletionSource<byte[]>>();

        private GatewayQueries(IChannel channel, string routingPrefix, string replyTo, Action<string>? onDiagnosticMessage)
        {
            _channel = channel;
            _routingPrefix = routingPrefix;
            _replyTo = replyTo;
            _onDiagnosticMessage = onDiagnosticMessage;
        }

        internal static async Task<GatewayQueries> CreateAsync(IConnection connection, GatewayClientOptions options)
        {
            IChannel channel = await connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync(RequestExchange, ExchangeType.Topic, true);
            await channel.ExchangeDeclareAsync(ResponseExchange, ExchangeType.Topic, true);

            string routingPrefix = $"tss.{options.ClientId}.{options.LocationUuid}.{options.SetupUuid}.{options.ScreenId}";
            string replyTo = $"tss.response.{options.ClientId}.{options.LocationUuid}.{options.SetupUuid}.{options.ScreenId}.{Guid.NewGuid():N}";

            QueueDeclareOk queue = await channel.QueueDeclareAsync(string.Empty, false, true, true);
            await channel.QueueBindAsync(queue.QueueName, ResponseExchange, replyTo);

            GatewayQueries queries = new GatewayQueries(channel, routingPrefix, replyTo, options.OnDiagnosticMessage);

            AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += queries.OnResponseReceived;

            await channel.BasicConsumeAsync(queue.QueueName, true, string.Empty, false, true, null, consumer);

            return queries;
        }

        // Story
        public async Task<StoryStatusResponse> GetStoryStatusAsync() => (await CallAsync("story", "status", EmptyMessage)).StoryStatus;

        public async Task<AvailableStoriesResponse> GetAvailableStoriesAsync(AvailableStoriesRequest request) => (await CallAsync("story", "available-stories", request)).AvailableStories;

        public async Task<AvailableStory> GetAvailableStoryAsync(string uuid) => (await CallAsync("story", "available-story", new AvailableStoryRequest {Uuid = uuid})).AvailableStory;

        public async Task<AvailableScenesResponse> GetAvailableScenesAsync() => (await CallAsync("story", "available-scenes", EmptyMessage)).AvailableScenes;

        public async Task<CorruptedAssetsResponse> GetCorruptedAssetsAsync() => (await CallAsync("story", "corrupted-assets", EmptyMessage)).CorruptedAssets;

        public async Task<StoryDataResponse> GetStoryDataAsync(string uuid, int version) => (await CallAsync("story", "data", new StoryDataRequest {Uuid = uuid, Version = version})).StoryData;

        // Scene
        public async Task<CurrentStateResponse> GetSceneStatusAsync() => (await CallAsync("scene", "status", EmptyMessage)).CurrentState;

        // Instance
        public async Task<InstanceInfo> GetInstanceInfoAsync() => (await CallAsync("instance", "info", EmptyMessage)).InstanceInfo;

        private async Task<Response> CallAsync(string entity, string action, IMessage request)
        {
            string correlationId = Guid.NewGuid().ToString("N");
            TaskCompletionSource<byte[]> tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[correlationId] = tcs;

            try
            {
                await PublishQueryAsync(entity, action, request, correlationId);

                return await AwaitResponseAsync(tcs, entity, action);
            }
            finally
            {
                _pending.TryRemove(correlationId, out _);
            }
        }

        private async Task PublishQueryAsync(string entity, string action, IMessage request, string correlationId)
        {
            string routingKey = $"{_routingPrefix}.{entity}.{action}";

            BasicProperties properties = new BasicProperties
            {
                ContentType = ContentType,
                CorrelationId = correlationId,
                Expiration = Expiration,
                Headers = new Dictionary<string, object?>
                {
                    {ReplyToHeader, _replyTo}
                }
            };

            await _publishLock.WaitAsync();

            try
            {
                await _channel.BasicPublishAsync(RequestExchange, routingKey, true, properties, request.ToByteArray());
            }
            finally
            {
                _publishLock.Release();
            }
        }

        private static async Task<Response> AwaitResponseAsync(TaskCompletionSource<byte[]> tcs, string entity, string action)
        {
            using CancellationTokenSource cts = new CancellationTokenSource(Timeout);
            using CancellationTokenRegistration registration = cts.Token.Register(() => tcs.TrySetCanceled());

            byte[] responseBytes;

            try
            {
                responseBytes = await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Timeout waiting for query response: {entity}.{action}");
            }

            Response response = Response.Parser.ParseFrom(responseBytes);

            return response.ResponseCase == Response.ResponseOneofCase.Error ? throw new GatewayException(response.Error.Code, response.Error.Message) : response;
        }

        private Task OnResponseReceived(object sender, BasicDeliverEventArgs ea)
        {
            string? correlationId = ea.BasicProperties.CorrelationId;

            if (!string.IsNullOrEmpty(correlationId) && _pending.TryRemove(correlationId, out TaskCompletionSource<byte[]>? tcs))
            {
                tcs.TrySetResult(ea.Body.ToArray());
            }
            else
            {
                _onDiagnosticMessage?.Invoke($"Received query response with unknown or missing correlation id '{correlationId ?? "<none>"}'.");
            }

            return Task.CompletedTask;
        }
    }
}