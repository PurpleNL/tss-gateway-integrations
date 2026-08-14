# Concepts

## The gateway

The TSS gateway is the single integration point for anything that wants to talk to TSS players. It is a message bus (RabbitMQ): your application publishes messages on it and subscribes to what comes back. The flow is always client application > gateway > player.

The C# SDK wraps all of this. You never build routing keys or touch RabbitMQ directly.

## Requests, events, and queries

The player is in charge of playback. An integration never commands it directly, it publishes requests:

- A **request** is fire-and-forget: start a story, change a scene, fire a trigger. Publishing a request returns immediately and tells you nothing about the outcome.
- An **event** is the player reporting what it actually did: `story.started`, `scene.changed`. To know a request worked, wait for the matching event. A request that the player cannot honor produces no event, which surfaces as a timeout on your wait.
- A **query** is a read: the story status, the available stories. Queries are request/response and return the player's answer, or an error response when the player rejects the query.

This split keeps integrations honest about the physical world. A screen that is off, a story that is still syncing, or a player that is restarting cannot be hidden behind a return value.

## Addressing

Every player installation is identified by three values, which you receive from Storytelling Company:

- A **location** is the physical site.
- A **setup** is one player installation at that location.
- A **screen** is one output of that setup. Screen ids start at 0.

A request targets one screen by default. The SDK's `RequestScope` widens it: `Setup` targets every screen of the setup, `Location` targets every screen at the location.

## Versioning and pinning

Every version of the TSS player has a matching tag in this repository, so the protocol files and SDK source always line up with the player you integrate against.

- Integrate against the `stable` branch and pin the release tag that matches your player version, for example `4.1.0`.
- The `latest` branch follows ongoing development and can change ahead of your player. Use it to preview, not to build against.

## Where the reference detail lives

- Message payloads and their fields: the comments in the [proto files](../protos/).
- The client surface (requests, events, queries, options, errors, recovery): the [SDK readme](../sdk/csharp/README.md).
