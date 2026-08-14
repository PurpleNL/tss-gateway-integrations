using System;

namespace TSS.Gateway.Sdk
{
    // Connection and addressing settings for a GatewayClient. ClientId identifies the connecting
    // application: it becomes the source segment of every published routing key and is woven into
    // queue names, so two clients with different ids never share queues or responses.
    public class GatewayClientOptions
    {
        public string Host { get; set; } = "localhost";

        public int Port { get; set; } = 5672;

        public string UserName { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string ClientId { get; set; } = string.Empty;

        public string LocationUuid { get; set; } = string.Empty;

        public string SetupUuid { get; set; } = string.Empty;

        public int ScreenId { get; set; }

        // Each sub-client is only set up on the broker when enabled, so a client that for example
        // only publishes requests never creates an event queue or a reply queue.
        public bool EnableRequests { get; set; } = true;

        public bool EnableEvents { get; set; } = true;

        public bool EnableQueries { get; set; } = true;

        // Invoked with a human-readable message for conditions the client survives but a developer
        // may want to see, such as a query response arriving after its wait already timed out. The
        // SDK has no logging dependency; wire this to your own logger.
        public Action<string>? OnDiagnosticMessage { get; set; }

        internal void Validate()
        {
            RequireValue(ClientId, nameof(ClientId));
            RequireValue(Host, nameof(Host));
            RequireValue(UserName, nameof(UserName));
            RequireValue(Password, nameof(Password));
            RequireValue(LocationUuid, nameof(LocationUuid));
            RequireValue(SetupUuid, nameof(SetupUuid));
        }

        private static void RequireValue(string value, string name)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException($"GatewayClientOptions.{name} is required.", name);
            }
        }
    }
}
