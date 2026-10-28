# Changelog

Protocol and documentation changes, per release. Maintained by hand, newest first.

## Unreleased

- New query `asset.stream-offer`, which starts a WebRTC stream of a url asset to one viewer. The video goes directly from the player to the viewer, the gateway only carries the handshake. See [Streaming a url asset](requests.md#streaming-a-url-asset).
- New requests `asset.stream-resize` and `asset.stream-stop`, which change the size of a running stream and stop it.
- New error code 716: the player already streams to its maximum number of viewers (`maxUrlStreams`, default 4).
- The C# SDK exposes OfferUrlAssetStreamAsync, ResizeUrlAssetStreamAsync and StopUrlAssetStreamAsync.

## 2026-09-25

- `scene.change` accepts `sceneIndex` as a third way to address the scene, next to `uuid` and `integrationId`. The screensaver scene is index 0, the other scenes count from 1 in story order, skipping the screensaver. The C# SDK exposes ChangeSceneByIndexAsync.
- The Scene model carries `sceneIndex`, visible in `story.data` and `scene.status`, and `scene.changing` and `scene.changed` carry the `sceneIndex` of the scene they identify.

## 2026-09-15

- New request `asset.change-url`, which loads another page in a url asset for the rest of the scene. The authored url stays the asset's home, so `asset.back` and `asset.home` still return to it, and leaving the scene loads it again.
- New event `asset.url-changed`, published whenever the page a url asset shows changes, carrying the page that loaded. Besides `asset.change-url` it also covers `asset.back`, `asset.forward` and `asset.home`, the navigation panel, and a visitor following a link.

## 2026-08-26

- New event `instance.state-changed`, published on every playback state change and carrying the new state. The C# SDK exposes WaitForInstanceStateChangedAsync to wait for a specific state.
- New events `screensaver.entered` and `screensaver.exited`, published when a scene change moves the player into or out of the screensaver scene.
- New events `interaction.down`, `interaction.drag`, `interaction.longpress`, and `interaction.up`, reporting visitor touches with the pressed asset, position, and touch id.
- New event `scene.changing`, published when a scene change starts, before the transition runs.
- New events `asset.looped` (a looping video or sound wrapped around) and `asset.completed` (a non-looping video or sound reached its end and stopped).
- `scene.changed` and `scene.changing` carry the cause of the change and the pressed asset or fired trigger that caused it.
- `asset.shown` and `asset.hidden` carry the cause of the change the same way.
- `story.started` and `story.stopped` carry the version of the story that is playing.

## 2026-08-24

- The C# SDK message types moved from the TSS.Play.Shared.* namespaces to TSS.Gateway.Sdk.Models, TSS.Gateway.Sdk.Events, and TSS.Gateway.Sdk.Requests. Breaking for SDK package consumers: update your using directives when upgrading.
- InstanceHeartbeatEvent no longer carries the ip and apiPort fields; the player's removed local HTTP API was their only use. The field numbers are reserved.

## 2026-08-21

- `asset.volume-changed` now carries AssetVolumeChangedEvent instead of AssetVolumeChangeRequest. The event message was already defined, nothing published it.
- The wire format is unchanged for existing consumers, both messages use the same field numbers and types.
- The event now also carries `uuid`. It was previously left empty because the request message wraps `uuid` and `integrationId` in a `oneof`.
- The C# SDK method `WaitForAssetVolumeChangedAsync` returns AssetVolumeChangedEvent instead of AssetEvent.

## 2026-08-14

- Initial documentation set: getting started, concepts, routing, authentication, requests, events, queries, best practices, and feature requests.
- Documents the protocol as currently on `latest`: 36 requests, 18 events, 8 queries.
