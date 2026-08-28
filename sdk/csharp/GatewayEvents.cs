using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TSS.Gateway.Sdk.Events;

// ReSharper disable UnusedMember.Global

namespace TSS.Gateway.Sdk
{
    // The events side of a GatewayClient: consumes the player's events from the tss-events-listen
    // exchange on an own channel and completes waiters that match the routing-key suffix and an
    // optional payload predicate.
    public class GatewayEvents
    {
        private const string EventExchange = "tss-events-listen";
        private const int DefaultTimeoutMs = 5000;

        // All waiter bookkeeping happens under one lock: registration, timeout removal, and the
        // consumer thread completing waiters. The completion sources run their continuations
        // asynchronously, so no caller code runs while the lock is held.
        private readonly object _waitersLock = new object();
        private readonly Dictionary<string, List<ICompletionSourceWrapper>> _waitingTasks = new Dictionary<string, List<ICompletionSourceWrapper>>();

        private GatewayEvents()
        {
        }

        internal static async Task<GatewayEvents> CreateAsync(IConnection connection, GatewayClientOptions options)
        {
            IChannel channel = await connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync(EventExchange, ExchangeType.Topic, true);

            QueueDeclareOk queue = await channel.QueueDeclareAsync($"{options.ClientId}-{options.SetupUuid}-{options.ScreenId}");

            await channel.QueueBindAsync(queue.QueueName, EventExchange, $"tss.*.{options.LocationUuid}.{options.SetupUuid}.{options.ScreenId}.#");

            GatewayEvents events = new GatewayEvents();

            AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += events.OnMessageReceived;

            await channel.BasicConsumeAsync(queue.QueueName, true, $"{options.ClientId}-{EventExchange}", false, false, null, consumer);

            return events;
        }

        // Story
        public Task<StoryEvent> WaitForStoryStartedAsync(string uuid, int timeoutMs = DefaultTimeoutMs) => WaitForEventAsync("story.started", StoryEvent.Parser, msg => msg.Uuid == uuid, timeoutMs: timeoutMs);

        public Task WaitForStoryStoppedAsync(int timeoutMs = DefaultTimeoutMs) => WaitForEventAsync("story.stopped", timeoutMs);

        public Task<MasterVolumeChangedEvent> WaitForMasterVolumeChangedAsync(int timeoutMs = DefaultTimeoutMs) => WaitForEventAsync("story.master-volume-changed", MasterVolumeChangedEvent.Parser, timeoutMs: timeoutMs);

        // Scene
        public Task<SceneEvent> WaitForSceneChangedByIntegrationIdAsync(string integrationId, int timeoutMs = DefaultTimeoutMs) => WaitForEventAsync("scene.changed", SceneEvent.Parser, msg => msg.IntegrationId == integrationId, integrationId, timeoutMs);

        public Task<SceneEvent> WaitForSceneChangingByIntegrationIdAsync(string integrationId, int timeoutMs = DefaultTimeoutMs) => WaitForEventAsync("scene.changing", SceneEvent.Parser, msg => msg.IntegrationId == integrationId, integrationId, timeoutMs);

        public Task<SceneEvent> WaitForSceneChangedByUuidAsync(string uuid, int timeoutMs = DefaultTimeoutMs) => WaitForEventAsync("scene.changed", SceneEvent.Parser, msg => msg.Uuid == uuid, uuid, timeoutMs);

        // Screensaver
        public Task<ScreensaverEvent> WaitForScreensaverEnteredAsync(int timeoutMs = DefaultTimeoutMs) => WaitForEventAsync("screensaver.entered", ScreensaverEvent.Parser, timeoutMs: timeoutMs);

        public Task<ScreensaverEvent> WaitForScreensaverExitedAsync(int timeoutMs = DefaultTimeoutMs) => WaitForEventAsync("screensaver.exited", ScreensaverEvent.Parser, timeoutMs: timeoutMs);

        // Asset
        public Task<AssetEvent> WaitForAssetShownAsync(string integrationId, int timeoutMs = DefaultTimeoutMs) => WaitForAssetEventAsync("asset.shown", integrationId, timeoutMs);

        public Task<AssetEvent> WaitForAssetHiddenAsync(string integrationId, int timeoutMs = DefaultTimeoutMs) => WaitForAssetEventAsync("asset.hidden", integrationId, timeoutMs);

        public Task<AssetEvent> WaitForAssetPlayingAsync(string integrationId, int timeoutMs = DefaultTimeoutMs) => WaitForAssetEventAsync("asset.playing", integrationId, timeoutMs);

        public Task<AssetEvent> WaitForAssetPausedAsync(string integrationId, int timeoutMs = DefaultTimeoutMs) => WaitForAssetEventAsync("asset.paused", integrationId, timeoutMs);

        public Task<AssetEvent> WaitForAssetMutedAsync(string integrationId, int timeoutMs = DefaultTimeoutMs) => WaitForAssetEventAsync("asset.muted", integrationId, timeoutMs);

        public Task<AssetEvent> WaitForAssetUnmutedAsync(string integrationId, int timeoutMs = DefaultTimeoutMs) => WaitForAssetEventAsync("asset.unmuted", integrationId, timeoutMs);

        public Task<AssetVolumeChangedEvent> WaitForAssetVolumeChangedAsync(string integrationId, int timeoutMs = DefaultTimeoutMs) => WaitForEventAsync("asset.volume-changed", AssetVolumeChangedEvent.Parser, msg => msg.IntegrationId == integrationId, integrationId, timeoutMs);

        public Task<AssetProgressChangedEvent> WaitForAssetSeekEndedAsync(string integrationId, int timeoutMs = DefaultTimeoutMs) => WaitForEventAsync("asset.seek-ended", AssetProgressChangedEvent.Parser, msg => msg.IntegrationId == integrationId, integrationId, timeoutMs);

        public Task<AssetResizedEvent> WaitForAssetResizedAsync(string integrationId, int timeoutMs = DefaultTimeoutMs) => WaitForEventAsync("asset.resized", AssetResizedEvent.Parser, msg => msg.IntegrationId == integrationId, integrationId, timeoutMs);

        public Task<AssetPageChangedEvent> WaitForAssetPageChangedAsync(string integrationId, int timeoutMs = DefaultTimeoutMs) => WaitForEventAsync("asset.page-changed", AssetPageChangedEvent.Parser, msg => msg.IntegrationId == integrationId, integrationId, timeoutMs);

        public Task<AssetMovedEvent> WaitForAssetMovedAsync(string integrationId, int timeoutMs = DefaultTimeoutMs) => WaitForEventAsync("asset.moved", AssetMovedEvent.Parser, msg => msg.IntegrationId == integrationId, integrationId, timeoutMs);

        public Task<AssetCroppedEvent> WaitForAssetCroppedAsync(string integrationId, int timeoutMs = DefaultTimeoutMs) => WaitForEventAsync("asset.cropped", AssetCroppedEvent.Parser, msg => msg.IntegrationId == integrationId, integrationId, timeoutMs);

        // Instance
        public Task<InstanceHeartbeatEvent> WaitForInstanceHeartbeatAsync(int timeoutMs = DefaultTimeoutMs) => WaitForEventAsync("instance.heartbeat", InstanceHeartbeatEvent.Parser, timeoutMs: timeoutMs);

        public async Task<T> WaitForEventAsync<T>(string eventSuffix, MessageParser<T> parser, Func<T, bool>? predicate = null, string name = "", int timeoutMs = DefaultTimeoutMs) where T : IMessage<T>
        {
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            CompletionSourceWrapper<T> wrapper = new CompletionSourceWrapper<T>(tcs, parser, predicate);

            AddWaiter(eventSuffix, wrapper);

            using CancellationTokenSource cts = new CancellationTokenSource(timeoutMs);
            using CancellationTokenRegistration registration = cts.Token.Register(() => tcs.TrySetCanceled());

            try
            {
                return await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                RemoveWaiter(eventSuffix, wrapper.Id);
                throw new TimeoutException($"Timeout waiting for event: {eventSuffix}{(!string.IsNullOrEmpty(name) ? $" for {name}" : "")}");
            }
        }

        public async Task WaitForEventAsync(string eventSuffix, int timeoutMs = DefaultTimeoutMs)
        {
            TaskCompletionSource<byte[]> tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            RawCompletionSourceWrapper wrapper = new RawCompletionSourceWrapper(tcs);

            AddWaiter(eventSuffix, wrapper);

            using CancellationTokenSource cts = new CancellationTokenSource(timeoutMs);
            using CancellationTokenRegistration registration = cts.Token.Register(() => tcs.TrySetCanceled());

            try
            {
                await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                RemoveWaiter(eventSuffix, wrapper.Id);
                throw new TimeoutException($"Timeout waiting for event: {eventSuffix}");
            }
        }

        private Task<AssetEvent> WaitForAssetEventAsync(string eventSuffix, string integrationId, int timeoutMs) => WaitForEventAsync(eventSuffix, AssetEvent.Parser, msg => msg.IntegrationId == integrationId, integrationId, timeoutMs);

        private void AddWaiter(string eventSuffix, ICompletionSourceWrapper wrapper)
        {
            lock (_waitersLock)
            {
                if (!_waitingTasks.TryGetValue(eventSuffix, out List<ICompletionSourceWrapper>? list))
                {
                    list = new List<ICompletionSourceWrapper>();
                    _waitingTasks[eventSuffix] = list;
                }

                list.Add(wrapper);
            }
        }

        private void RemoveWaiter(string eventSuffix, Guid waiterId)
        {
            lock (_waitersLock)
            {
                if (_waitingTasks.TryGetValue(eventSuffix, out List<ICompletionSourceWrapper>? list))
                {
                    list.RemoveAll(w => w.Id == waiterId);

                    if (list.Count == 0)
                    {
                        _waitingTasks.Remove(eventSuffix);
                    }
                }
            }
        }

        private Task OnMessageReceived(object sender, BasicDeliverEventArgs ea)
        {
            string routingKey = ea.RoutingKey;
            byte[] body = ea.Body.ToArray();

            lock (_waitersLock)
            {
                foreach (KeyValuePair<string, List<ICompletionSourceWrapper>> kvp in _waitingTasks.ToArray())
                {
                    if (routingKey.EndsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        List<ICompletionSourceWrapper> remaining = new List<ICompletionSourceWrapper>();

                        foreach (ICompletionSourceWrapper waiter in kvp.Value)
                        {
                            bool handled = waiter.TryHandle(body);

                            if (!handled)
                            {
                                remaining.Add(waiter);
                            }
                        }

                        if (remaining.Count == 0)
                        {
                            _waitingTasks.Remove(kvp.Key);
                        }
                        else
                        {
                            _waitingTasks[kvp.Key] = remaining;
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }

        private interface ICompletionSourceWrapper
        {
            Guid Id { get; }

            bool TryHandle(byte[] body);
        }

        private class RawCompletionSourceWrapper : ICompletionSourceWrapper
        {
            private readonly TaskCompletionSource<byte[]> _tcs;

            public RawCompletionSourceWrapper(TaskCompletionSource<byte[]> tcs) => _tcs = tcs;

            public Guid Id { get; } = Guid.NewGuid();

            public bool TryHandle(byte[] body)
            {
                _tcs.TrySetResult(body);
                return true;
            }
        }

        private class CompletionSourceWrapper<T> : ICompletionSourceWrapper where T : IMessage<T>
        {
            private readonly TaskCompletionSource<T> _tcs;
            private readonly MessageParser<T> _parser;
            private readonly Func<T, bool>? _predicate;

            public CompletionSourceWrapper(TaskCompletionSource<T> tcs, MessageParser<T> parser, Func<T, bool>? predicate)
            {
                _tcs = tcs;
                _parser = parser;
                _predicate = predicate;
            }

            public Guid Id { get; } = Guid.NewGuid();

            public bool TryHandle(byte[] body)
            {
                try
                {
                    T message = _parser.ParseFrom(body);

                    if (_predicate == null || _predicate(message))
                    {
                        _tcs.TrySetResult(message);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _tcs.TrySetException(ex);
                }

                return false;
            }
        }
    }
}