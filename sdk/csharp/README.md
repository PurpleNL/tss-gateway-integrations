# TSS.Gateway.Sdk

Client SDK for the TSS gateway. A `GatewayClient` connects to the gateway of one player setup and exposes three sub-clients:

- `Requests`: fire-and-forget requests, for example starting a story or firing a trigger.
- `Events`: awaiting the player events that requests cause, with a timeout.
- `Queries`: request/response reads such as the story status or the available stories.

Flow: client application > SDK > tss-gateway > tss-play.

## Usage

```csharp
await using GatewayClient client = await GatewayClient.ConnectAsync(new GatewayClientOptions
{
    Host = "localhost",
    UserName = "user",
    Password = "pass",
    ClientId = "my-app",
    LocationUuid = "location-uuid",
    SetupUuid = "setup-uuid",
    ScreenId = 1
});

await client.Requests.StartStoryAsync(storyUuid);
await client.Events.WaitForStoryStartedAsync(storyUuid);

StoryStatusResponse status = await client.Queries.GetStoryStatusAsync();
```

`ClientId` identifies the connecting application; it becomes the source segment of every published routing key.

A client can also be constructed directly. `EnsureConnectionAsync` connects on the first call, is a no-op once connected, and simply tries again on the next call after a failed attempt:

```csharp
GatewayClient client = new GatewayClient(options);

await client.EnsureConnectionAsync();
await client.Requests.FireTriggerAsync(triggerUuid, state: true);
```

Once connected, a dropped connection recovers itself; requests during the outage fail and succeed again after recovery.

## Options

- `EnableRequests`, `EnableEvents` and `EnableQueries` (all default `true`) control which sub-clients are set up on the broker. Disable what you do not use; a disabled sub-client throws `InvalidOperationException` when accessed.
- `OnDiagnosticMessage` receives human-readable messages for conditions the client survives, such as the connection dropping and recovering, or a query response arriving after its wait timed out. The SDK has no logging dependency; wire this to your own logger.

## Errors

- An error response to a query throws a `GatewayException` with the error code and message from the player.
- A wait that does not complete in time throws a `TimeoutException`. Event waits default to 5 seconds and accept a custom timeout; queries time out after 15 seconds.
