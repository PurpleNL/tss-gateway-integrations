using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace TSS.Gateway.Sdk
{
    // Entry point of the SDK: owns the connection to one gateway. Flow: client application > SDK >
    // tss-gateway > tss-play. Only the sub-clients enabled in the options are set up. The client
    // connects through EnsureConnectionAsync (or the ConnectAsync convenience); once connected, a
    // dropped connection recovers itself.
    public class GatewayClient : IAsyncDisposable
    {
        private readonly GatewayClientOptions _options;
        private readonly SemaphoreSlim _connectLock = new SemaphoreSlim(1, 1);

        private IConnection? _connection;
        private GatewayRequests? _requests;
        private GatewayEvents? _events;
        private GatewayQueries? _queries;

        public GatewayClient(GatewayClientOptions options)
        {
            options.Validate();
            _options = options;
        }

        public GatewayRequests Requests => _requests ?? throw NotAvailable(_options.EnableRequests, "requests", nameof(GatewayClientOptions.EnableRequests));

        public GatewayEvents Events => _events ?? throw NotAvailable(_options.EnableEvents, "events", nameof(GatewayClientOptions.EnableEvents));

        public GatewayQueries Queries => _queries ?? throw NotAvailable(_options.EnableQueries, "queries", nameof(GatewayClientOptions.EnableQueries));

        public static async Task<GatewayClient> ConnectAsync(GatewayClientOptions options)
        {
            GatewayClient client = new GatewayClient(options);

            await client.EnsureConnectionAsync();

            return client;
        }

        // Connects on the first call and is a cheap no-op once connected. A failed attempt leaves
        // the client unconnected, so the next call simply tries again. Concurrent calls share one
        // attempt.
        public async Task EnsureConnectionAsync()
        {
            // Deliberately no IsOpen check: a connection that was established once recovers
            // itself, and connecting again during a recovery window would create a duplicate
            // connection whose event consumers compete for the same queue.
            if (_connection != null)
            {
                return;
            }

            await _connectLock.WaitAsync();

            try
            {
                if (_connection == null)
                {
                    await ConnectCoreAsync();
                }
            }
            finally
            {
                _connectLock.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_connection != null)
            {
                await _connection.DisposeAsync();
            }
        }

        private async Task ConnectCoreAsync()
        {
            ConnectionFactory factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                ClientProvidedName = _options.ClientId,
                AutomaticRecoveryEnabled = true
            };

            IConnection connection = await factory.CreateConnectionAsync();

            if (_options.OnDiagnosticMessage != null)
            {
                ReportConnectionEvents(connection, _options.OnDiagnosticMessage);
            }

            try
            {
                _requests = _options.EnableRequests ? await GatewayRequests.CreateAsync(connection, _options) : null;
                _events = _options.EnableEvents ? await GatewayEvents.CreateAsync(connection, _options) : null;
                _queries = _options.EnableQueries ? await GatewayQueries.CreateAsync(connection, _options) : null;
                _connection = connection;
            }
            catch
            {
                // The client stays unconnected, so the connection is closed here instead of
                // leaking until process exit.
                _requests = null;
                _events = null;
                _queries = null;

                await connection.DisposeAsync();

                throw;
            }
        }

        private static void ReportConnectionEvents(IConnection connection, Action<string> onDiagnosticMessage)
        {
            connection.ConnectionShutdownAsync += (sender, args) =>
            {
                if (args.Initiator != ShutdownInitiator.Application)
                {
                    onDiagnosticMessage($"Gateway connection lost: {args.ReplyText}");
                }

                return Task.CompletedTask;
            };

            connection.RecoverySucceededAsync += (sender, args) =>
            {
                onDiagnosticMessage("Gateway connection recovered.");

                return Task.CompletedTask;
            };

            connection.ConnectionRecoveryErrorAsync += (sender, args) =>
            {
                onDiagnosticMessage($"Gateway connection recovery failed, retrying: {args.Exception.Message}");

                return Task.CompletedTask;
            };
        }

        private static InvalidOperationException NotAvailable(bool enabled, string subClient, string option) => new InvalidOperationException(enabled ? "The client is not connected. Call EnsureConnectionAsync or ConnectAsync first." : $"The {subClient} sub-client is disabled. Set GatewayClientOptions.{option} to use it.");
    }
}