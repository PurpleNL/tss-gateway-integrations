# Events

An event is the player reporting a state change: a story started, a scene changed, an asset was shown. Events are the only reliable signal that a [request](requests.md) was honored.

## Consuming events

- Declare a queue of your own and bind it on the `tss-events-listen` exchange.
- Bind with a pattern that matches the player you care about, for example `tss.*.<location>.<setup>.<screen>.#` for one screen's events.
- Events from the player carry `play` as the client segment and are always fully scoped: the routing key names the exact screen.
- The body is a Protocol Buffers message, see [`events.proto`](../protos/events.proto). Events expire after 5000 ms, so a consumer that was offline does not receive stale ones.

## Story

| Topic and action | Payload message | Fired when |
|---|---|---|
| `story.started` | StoryEvent | A story has started |
| `story.stopped` | StoryEvent | A story has stopped |
| `story.master-volume-changed` | MasterVolumeChangedEvent | The master volume changed |

## Scene

| Topic and action | Payload message | Fired when |
|---|---|---|
| `scene.changed` | SceneEvent | The scene finished changing |

## Asset

| Topic and action | Payload message | Fired when |
|---|---|---|
| `asset.shown` | AssetEvent | The asset is fully shown |
| `asset.hidden` | AssetEvent | The asset is fully hidden |
| `asset.playing` | AssetEvent | The asset started playing |
| `asset.paused` | AssetEvent | The asset was paused |
| `asset.muted` | AssetEvent | The asset was muted |
| `asset.unmuted` | AssetEvent | The asset was unmuted |
| `asset.volume-changed` | AssetVolumeChangeRequest | The asset's volume changed |
| `asset.seek-ended` | AssetProgressChangedEvent | A seek completed, carries the new normalized progress |
| `asset.page-changed` | AssetPageChangedEvent | A pdf asset's page changed |
| `asset.content-changed` | AssetEvent | A text or image asset's content changed |
| `asset.moved` | AssetMovedEvent | The asset's position changed |
| `asset.resized` | AssetResizedEvent | The asset was resized or repositioned |
| `asset.cropped` | AssetCroppedEvent | The asset was cropped, carries the applied margins |

## Instance

| Topic and action | Payload message | Fired when |
|---|---|---|
| `instance.heartbeat` | InstanceHeartbeatEvent | Periodically while the player runs, identifies the instance (setup, screen, ip) |
