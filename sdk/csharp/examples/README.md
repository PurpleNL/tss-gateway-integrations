# Example integration

A minimal console application that shows the full request > player > event round trip through a TSS gateway:

1. Connect to the gateway.
2. Query the stories that are available on the player and list them.
3. Stop the story the player is currently running, if there is one, because the player ignores a start request while a story runs.
4. Request the player to start a story and wait for the `story.started` event.
5. Query the story status while it plays.
6. Request the player to stop the story and wait for the `story.stopped` event.

Every step prints what happened, so running it against a live player shows each request being answered by the player. The player is in charge: starting and stopping a story are requests, and the events are the player reporting what it actually did.

## Prerequisites

- The [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
- Access to a TSS gateway: a host, a username and password, and the location and setup uuids of the player you want to drive. These are provided by TSC.
- A running player with at least one story synced to it.

## Building

This example is developed alongside the SDK source and references it as a project, so it does not compile standalone in this repository. To run it, create your own console project, copy `Program.cs` into it, and reference the `TSS.Gateway.Sdk` NuGet package instead of the project. That package is also what a real integration references.

## Running

From the project folder:

```
dotnet run -- --host <gateway host> --user <username> --password <password> --location <location uuid> --setup <setup uuid>
```

Optional arguments:

- `--port` gateway port, defaults to 5672.
- `--screen` screen id of the player, defaults to 0.
- `--story` uuid of the story to start, defaults to the first available story.

## What you should see

```
Connected to the gateway at 10.0.0.5.
The player has 2 stories:
  Demo Story (Ready)
  Another Story (Ready)
Requested the player to start story 5103, waiting for it to report back...
The player started the story.
Story status: "Demo Story" version 3, state Running.
Letting it play for five seconds...
Requested the player to stop the story, waiting for it to report back...
The player stopped the story. Done.
```

A player that was already running a story shows an extra line before the start, saying that story was requested to stop first.

A `TimeoutException` on one of the waits usually means the player is not running, the story is still initializing, or the location, setup or screen values do not match the player.

## Where to go from here

The [SDK README](../README.md) documents the full client surface: the available requests, events and queries, the connection options, and how errors and recovery behave. The message payloads are documented in the proto files.
