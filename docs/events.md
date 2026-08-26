# Events

An event is the player reporting a state change: a story started, a scene changed, an asset was shown. Events are the confirmation signal for [requests](requests.md).

## Consuming events

- Declare a queue of your own and bind it on the `tss-events-listen` exchange. The C# SDK declares its queue non-durable, exclusive, and auto-delete, which is the right shape for a live event stream.
- All exchanges already exist on the gateway, you do not need to declare them. If your client library declares them anyway, declare durable topic exchanges, other parameters are refused with a channel error.
- Bind with a pattern that matches the player you care about, for example `tss.*.<location>.<setup>.<screen>.#` for one screen's events.
- Events from the player carry `play` as the client segment and are always fully scoped: the routing key names the exact screen. A setup or location scoped request therefore produces one event per screen it reached.
- The body is a Protocol Buffers message, see [`events.proto`](../protos/events.proto).

## Delivery notes

- Events expire after 5000 ms (deployment default), so a consumer that was offline does not receive stale ones.
- Asset events are sent a short moment after the change. Rapid successive changes of the same kind on the same asset can be reported as one event carrying the final value.
- Events that are still pending when a story starts, a story stops, or the scene changes are not sent. A request honored right before such a transition may therefore never report its event, treat a timeout around transitions with care.

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
| `asset.volume-changed` | AssetVolumeChangedEvent | The asset's volume changed |
| `asset.seek-ended` | AssetProgressChangedEvent | A seek completed and actually changed the position, carries the new normalized progress |
| `asset.page-changed` | AssetPageChangedEvent | A pdf asset's page changed |
| `asset.content-changed` | AssetEvent | A text or image asset's content changed |
| `asset.moved` | AssetMovedEvent | The asset's position changed |
| `asset.resized` | AssetResizedEvent | The asset was resized or repositioned |
| `asset.cropped` | AssetCroppedEvent | The asset's crop meaningfully changed, carries the applied margins (a minimal change reports `asset.resized` instead) |

## Instance

| Topic and action | Payload message | Fired when |
|---|---|---|
| `instance.heartbeat` | InstanceHeartbeatEvent | Every second (deployment default) while the player runs, identifies the instance (setup, screen) |
