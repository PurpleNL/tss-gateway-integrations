# Getting started

This guide takes you from nothing to a first request answered by a TSS player.

## What you need

- The [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
- Access to a TSS gateway: a host, a username and password, and the location and setup uuids of the player you want to drive. These are provided by Storytelling Company.
- The `TSS.Gateway.Sdk` NuGet package, also provided by Storytelling Company.
- A running player with at least one story synced to it.

## Connect and start a story

Create a console project, reference the `TSS.Gateway.Sdk` package, and connect:

```csharp
await using GatewayClient client = await GatewayClient.ConnectAsync(new GatewayClientOptions
{
    Host = "gateway host",
    UserName = "username",
    Password = "password",
    ClientId = "my-app",
    LocationUuid = "location uuid",
    SetupUuid = "setup uuid",
    ScreenId = 0
});

await client.Requests.StartStoryAsync(storyUuid);
await client.Events.WaitForStoryStartedAsync(storyUuid);
```

`ClientId` names your application. Pick one value and keep it, the gateway uses it to keep your messages and responses separate from other clients.

The start call returns when the request is published, not when the story runs. The player reports what it actually did through events, which is what the second line waits for. [Concepts](concepts.md) explains this model.

## The example integration

A complete, commented walkthrough of connecting, querying, starting, and stopping lives in [`sdk/csharp/examples/`](../sdk/csharp/examples/). Its [readme](../sdk/csharp/examples/README.md) shows the exact commands and the output you should see.

## Where to go from here

- [Concepts](concepts.md) for the request > player > event model, addressing, and versioning.
- The [SDK readme](../sdk/csharp/README.md) for the full client surface: requests, events, queries, options, errors, and recovery.
- The [proto files](../protos/) for the message payloads and their field-level documentation.
