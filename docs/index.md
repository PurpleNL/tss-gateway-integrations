# TSS Gateway

The entry point for integrating with the TSS gateway: the single integration point for anything that wants to talk to TSS players. Send requests, receive events, and run queries against the players at your locations.

- New here? Start with [Getting started](getting-started.md) and [Concepts](concepts.md).
- Building in C#? The [C# SDK](csharp/getting-started.md) wraps the whole protocol.
- Building in another language? [Routing](routing.md), [Requests](requests.md), [Events](events.md), and [Queries](queries.md) describe everything on the wire.
- Read the [best practices](best-practices.md) before you ship, and [how to submit a feature request](feature-requests.md) when you miss something.

## Versioning

Every version of the TSS player has a matching tag in the [repository](https://github.com/PurpleNL/tss-gateway-integrations), so the protocol files always line up with the player you integrate against. The `stable` branch follows releases, pin the tag that matches your player. This site follows `latest`, the ongoing development state. See the [changelog](changelog.md) for what changed.

See [Compatibility](concepts.md#compatibility) for what a pinned tag guarantees when the player updates.
