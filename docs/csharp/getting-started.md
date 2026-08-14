# Getting started in C#

This guide takes you from nothing to a first request answered by a TSS player, using the C# SDK. Read the [general getting started](../getting-started.md) first for gateway access and network requirements.

## What you need

- The [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
- The `TSS.Gateway.Sdk` NuGet package, provided by TSC.
- Gateway access and a running player, see the [general getting started](../getting-started.md).

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

The start call returns when the request is published, not when the story runs. The player reports what it actually did through events, which is what the second line waits for. [Concepts](../concepts.md) explains this model.

## The example integration

A complete, commented walkthrough of connecting, querying, starting, and stopping lives in [`sdk/csharp/examples/`](../../sdk/csharp/examples/). Its [readme](../../sdk/csharp/examples/README.md) shows the exact commands and the output you should see.

## Where to go from here

- The [SDK readme](../../sdk/csharp/README.md) for the full client surface: requests, events, queries, options, errors, and recovery.
- [Concepts](../concepts.md) and the protocol pages for everything the SDK does under the hood.
