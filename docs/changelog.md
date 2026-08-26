# Changelog

Protocol and documentation changes, per release. Maintained by hand, newest first.

## 2026-08-21

- `asset.volume-changed` now carries AssetVolumeChangedEvent instead of AssetVolumeChangeRequest. The event message was already defined, nothing published it.
- The wire format is unchanged for existing consumers, both messages use the same field numbers and types.
- The event now also carries `uuid`. It was previously left empty because the request message wraps `uuid` and `integrationId` in a `oneof`.
- The C# SDK method `WaitForAssetVolumeChangedAsync` returns AssetVolumeChangedEvent instead of AssetEvent.

## 2026-08-14

- Initial documentation set: getting started, concepts, routing, authentication, requests, events, queries, best practices, and feature requests.
- Documents the protocol as currently on `latest`: 36 requests, 18 events, 8 queries.
