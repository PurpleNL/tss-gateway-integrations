using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using RabbitMQ.Client;
using TSS.Play.Shared.Requests;

// ReSharper disable UnusedMember.Global

namespace TSS.Gateway.Sdk
{
    // The requests side of a GatewayClient: fire-and-forget commands published on the
    // tss-requests-publish exchange. Publishes are serialized because a RabbitMQ channel does not
    // support concurrent use, so callers may fire requests concurrently.
    public class GatewayRequests
    {
        private const string RequestExchange = "tss-requests-publish";

        private static readonly BasicProperties Properties = new BasicProperties
        {
            ContentType = "application/x-protobuf",
            Expiration = "5000"
        };

        private readonly IChannel _channel;
        private readonly SemaphoreSlim _publishLock = new SemaphoreSlim(1, 1);
        private readonly GatewayClientOptions _options;

        private GatewayRequests(IChannel channel, GatewayClientOptions options)
        {
            _channel = channel;
            _options = options;
        }

        internal static async Task<GatewayRequests> CreateAsync(IConnection connection, GatewayClientOptions options)
        {
            IChannel channel = await connection.CreateChannelAsync();

            return new GatewayRequests(channel, options);
        }

        // Story
        public Task StartStoryAsync(string storyUuid, int? version = null, bool suppressPlaybackTracking = false, RequestScope scope = RequestScope.Screen)
        {
            StoryStartRequest request = new StoryStartRequest
            {
                StoryUuid = storyUuid,
                SuppressPlaybackTracking = suppressPlaybackTracking
            };

            if (version.HasValue)
            {
                request.Version = version.Value;
            }

            return RequestAsync("story", "start", request, scope);
        }

        public Task StopStoryAsync(RequestScope scope = RequestScope.Screen) => RequestAsync("story", "stop", new Empty(), scope);

        public Task ChangeMasterVolumeAsync(float volume, RequestScope scope = RequestScope.Screen) => RequestAsync("story", "volume", new VolumeChangeRequest {Volume = volume}, scope);

        // Scene
        public Task ChangeSceneByIntegrationIdAsync(string integrationId, RequestScope scope = RequestScope.Screen) => RequestAsync("scene", "change", new SceneChangeRequest {IntegrationId = integrationId}, scope);

        public Task ChangeSceneByUuidAsync(string uuid, RequestScope scope = RequestScope.Screen) => RequestAsync("scene", "change", new SceneChangeRequest {Uuid = uuid}, scope);

        public Task SkipTransitionAsync(RequestScope scope = RequestScope.Screen) => RequestAsync("scene", "skip-transition", new Empty(), scope);

        public Task NextSceneAsync(RequestScope scope = RequestScope.Screen) => RequestAsync("scene", "next", new Empty(), scope);

        public Task PreviousSceneAsync(RequestScope scope = RequestScope.Screen) => RequestAsync("scene", "previous", new Empty(), scope);

        // Asset
        public Task ShowAssetAsync(string integrationId, RequestScope scope = RequestScope.Screen) => RequestAsync("asset", "show", new AssetRequest {IntegrationId = integrationId}, scope);

        public Task HideAssetAsync(string integrationId, RequestScope scope = RequestScope.Screen) => RequestAsync("asset", "hide", new AssetRequest {IntegrationId = integrationId}, scope);

        public Task PlayAssetAsync(string integrationId, RequestScope scope = RequestScope.Screen) => RequestAsync("asset", "play", new AssetRequest {IntegrationId = integrationId}, scope);

        public Task PauseAssetAsync(string integrationId, RequestScope scope = RequestScope.Screen) => RequestAsync("asset", "pause", new AssetRequest {IntegrationId = integrationId}, scope);

        public Task MuteAssetAsync(string integrationId, RequestScope scope = RequestScope.Screen) => RequestAsync("asset", "mute", new AssetRequest {IntegrationId = integrationId}, scope);

        public Task UnmuteAssetAsync(string integrationId, RequestScope scope = RequestScope.Screen) => RequestAsync("asset", "unmute", new AssetRequest {IntegrationId = integrationId}, scope);

        public Task ChangeAssetVolumeAsync(string integrationId, float normalizedVolume, RequestScope scope = RequestScope.Screen) => RequestAsync("asset", "volume", new AssetVolumeChangeRequest {IntegrationId = integrationId, NormalizedVolume = normalizedVolume}, scope);

        public Task SeekAssetAsync(string integrationId, float normalizedProgress, RequestScope scope = RequestScope.Screen) => RequestAsync("asset", "seek", new AssetSeekRequest {IntegrationId = integrationId, NormalizedProgress = normalizedProgress}, scope);

        public Task ChangeAssetPageAsync(string integrationId, int page, RequestScope scope = RequestScope.Screen) => RequestAsync("asset", "page", new AssetPageChangeRequest {IntegrationId = integrationId, Page = page}, scope);

        public Task MoveAssetAsync(string integrationId, float bottom, float left, RequestScope scope = RequestScope.Screen) => RequestAsync("asset", "move", new AssetMoveRequest {IntegrationId = integrationId, Bottom = bottom, Left = left}, scope);

        public Task EnlargeAssetAsync(string integrationId, RequestScope scope = RequestScope.Screen) => RequestAsync("asset", "enlarge", new AssetRequest {IntegrationId = integrationId}, scope);

        public Task ShrinkAssetAsync(string integrationId, RequestScope scope = RequestScope.Screen) => RequestAsync("asset", "shrink", new AssetRequest {IntegrationId = integrationId}, scope);

        public Task CropAssetAsync(string integrationId, int? left = null, int? right = null, int? top = null, int? bottom = null, RequestScope scope = RequestScope.Screen)
        {
            AssetCropRequest request = new AssetCropRequest {IntegrationId = integrationId};

            if (left.HasValue)
            {
                request.Left = left.Value;
            }

            if (right.HasValue)
            {
                request.Right = right.Value;
            }

            if (top.HasValue)
            {
                request.Top = top.Value;
            }

            if (bottom.HasValue)
            {
                request.Bottom = bottom.Value;
            }

            return RequestAsync("asset", "crop", request, scope);
        }

        // Trigger
        public Task FireTriggerAsync(string triggerUuid, bool? state = null, RequestScope scope = RequestScope.Screen)
        {
            TriggerFireRequest request = new TriggerFireRequest
            {
                TriggerUuid = triggerUuid
            };

            if (state.HasValue)
            {
                request.State = state.Value;
            }

            return RequestAsync("trigger", "fire", request, scope);
        }

        private async Task RequestAsync(string topic, string action, IMessage message, RequestScope scope)
        {
            string routingKey = BuildRoutingKey(topic, action, scope);

            await _publishLock.WaitAsync();

            try
            {
                await _channel.BasicPublishAsync(RequestExchange, routingKey, true, Properties, message.ToByteArray());
            }
            finally
            {
                _publishLock.Release();
            }
        }

        private string BuildRoutingKey(string topic, string action, RequestScope scope)
        {
            switch (scope)
            {
                case RequestScope.Setup:
                    return $"tss.{_options.ClientId}.{_options.LocationUuid}.{_options.SetupUuid}.{topic}.{action}";
                case RequestScope.Location:
                    return $"tss.{_options.ClientId}.{_options.LocationUuid}.{topic}.{action}";
                default:
                    return $"tss.{_options.ClientId}.{_options.LocationUuid}.{_options.SetupUuid}.{_options.ScreenId}.{topic}.{action}";
            }
        }
    }
}
