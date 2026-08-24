# Changelog

Protocol and documentation changes, per release. Maintained by hand, newest first.

## 2026-08-24

- The C# SDK message types moved from the TSS.Play.Shared.* namespaces to TSS.Gateway.Sdk.Models, TSS.Gateway.Sdk.Events, and TSS.Gateway.Sdk.Requests. Breaking for SDK package consumers: update your using directives when upgrading.
- InstanceHeartbeatEvent no longer carries the ip and apiPort fields; the player's removed local HTTP API was their only use. The field numbers are reserved.

## 2026-08-14

- Initial documentation set: getting started, concepts, routing, authentication, requests, events, queries, best practices, and feature requests.
- Documents the protocol as currently on `latest`: 36 requests, 18 events, 8 queries.
