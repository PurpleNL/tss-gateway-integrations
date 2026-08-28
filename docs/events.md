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
| `story.started` | StoryEvent | A story has started, carries the version that is playing (the player starts the newest version it has) |
| `story.stopped` | StoryEvent | A story has stopped, carries the version that was playing |
| `story.master-volume-changed` | MasterVolumeChangedEvent | The master volume changed |

## Scene

| Topic and action | Payload message | Fired when |
|---|---|---|
| `scene.changing` | SceneEvent | A scene change started, before the transition runs, identifies the scene the transition leads to |
| `scene.changed` | SceneEvent | The scene finished changing |

Both carry the cause of the change: `CONTROL` (a request sent by control), `REQUEST` (the same requests from any other sender), `TRIGGER`, `ONCLICK`, `AUTOSWITCH` (the background video ended and the scene advanced automatically), `TIMEOUT` (the inactivity timeout activated the screensaver scene), or `TOUCH` (a press on the screensaver scene navigated back out of it). For `ONCLICK` and `TRIGGER`, `causeUuid` names the pressed asset or the fired trigger.

## Asset

| Topic and action | Payload message | Fired when |
|---|---|---|
| `asset.shown` | AssetEvent | The asset is fully shown, carries the cause (`CONTROL`, `REQUEST`, `TRIGGER`, or `ONCLICK`) with the pressed asset or fired trigger in `causeUuid` |
| `asset.hidden` | AssetEvent | The asset is fully hidden, carries the cause like `asset.shown` |
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
| `asset.looped` | AssetEvent | A looping video or sound wrapped around |
| `asset.completed` | AssetEvent | A non-looping video or sound reached its end and stopped |

## Screensaver

| Topic and action | Payload message | Fired when |
|---|---|---|
| `screensaver.entered` | ScreensaverEvent | A scene change moved the player into the screensaver scene |
| `screensaver.exited` | ScreensaverEvent | A scene change moved the player out of the screensaver scene |

Both are published alongside the `scene.changed` event and with the same cause, whichever path caused the change: the inactivity timeout, the exit press, or a request or trigger. The uuid identifies the screensaver scene. No `screensaver.exited` is published when the story stops.

## Interaction

| Topic and action | Payload message | Fired when |
|---|---|---|
| `interaction.down` | InteractionEvent | A visitor touch landed on the screen |
| `interaction.drag` | InteractionEvent | A touch moved, at most one event per 0.1 second per touch |
| `interaction.longpress` | InteractionEvent | A press on an asset was held past the longpress threshold |
| `interaction.up` | InteractionEvent | A touch was released |

The uuid identifies the pressed asset and is empty for a touch on the background. Positions are in pixels, origin at the bottom-left. The `touchId` identifies the finger across its events for the duration of the touch.

## Instance

| Topic and action | Payload message | Fired when |
|---|---|---|
| `instance.heartbeat` | InstanceHeartbeatEvent | Every second (deployment default) while the player runs, identifies the instance (setup, screen) |
| `instance.state-changed` | InstanceStateChangedEvent | The player's playback state changed, carries the new state |

The state is the same value the `story.status` [query](queries.md) returns: `NONE`, `STARTING`, `RUNNING`, `CLOSING`, `TRANSITIONING`, `PREPARING_TRANSITION`, or `ERROR`. `story.started` and `story.stopped` cover the transitions into `RUNNING` and out of it, `instance.state-changed` covers them all. After `story.stopped` the player is still unloading and ignores start requests until the state is back to `NONE`; wait for that state instead of guessing with a delay.
