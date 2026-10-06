# Requests

A request asks the player to do something. It is fire-and-forget: publishing it tells you nothing about the outcome. The player honors a request only when its prerequisites hold, and reports what it did through [events](events.md). The "confirmed by" column below names the event to wait for; requests without one cannot be confirmed by an event.

Two prerequisites apply to every request and are not repeated in the tables: the player must not be in an error state, and where a table says "story running" the request is ignored without one.

## Publishing a request

- Publish on the `tss-requests-publish` exchange with a routing key from [Routing](routing.md), at screen, setup, or location scope.
- The body is the request's payload message, encoded as Protocol Buffers. Set `content_type` to `application/x-protobuf`.
- Requests without a payload message take an empty body.
- Set a message `expiration` (the C# SDK uses 5000 ms), so requests do not pile up and fire late when a player was temporarily unreachable.

The payload messages are defined in [`requests.proto`](../protos/requests.proto).

## Addressing assets and scenes

Assets and scenes are addressed by their integration id or their uuid, the payload messages accept either. One integration id can match more than one asset, in which case the request applies to each match and each match reports its own event.

## Story

| Topic and action | Payload message | Description | Prerequisites | Confirmed by |
|---|---|---|---|---|
| `story.start` | StoryStartRequest | Starts the story with the given uuid, optionally a specific version | No story running | `story.started` |
| `story.stop` | none | Stops the running story | Story running | `story.stopped` |
| `story.volume` | VolumeChangeRequest | Changes the master volume | Story running | `story.master-volume-changed` |

## Scene

All scene requests require a running story.

| Topic and action | Payload message | Description | Prerequisites | Confirmed by |
|---|---|---|---|---|
| `scene.change` | SceneChangeRequest | Changes to the requested scene | Scene exists, scene is not the active scene | `scene.changed` |
| `scene.next` | none | Goes to the next non-screensaver scene | Story has more than one scene | `scene.changed` |
| `scene.previous` | none | Goes to the previous non-screensaver scene | Story has more than one scene | `scene.changed` |
| `scene.skip-transition` | none | Skips the running transition | A transition is running | none (the interrupted change still reports `scene.changed`) |

## Asset

All asset requests require a running story, an existing asset, and except for `asset.content` the asset must be in the active scene or an active group.

| Topic and action | Payload message | Description | Prerequisites | Confirmed by |
|---|---|---|---|---|
| `asset.show` | AssetRequest | Shows the asset | Asset is invisible | `asset.shown` |
| `asset.hide` | AssetRequest | Hides the asset | Asset is visible | `asset.hidden` |
| `asset.play` | AssetRequest | Plays the asset | Video or sound asset, paused, visible | `asset.playing` |
| `asset.pause` | AssetRequest | Pauses the asset | Video or sound asset, playing | `asset.paused` |
| `asset.mute` | AssetRequest | Mutes the asset | Video, sound, url, or NDI asset, unmuted, visible | `asset.muted` |
| `asset.unmute` | AssetRequest | Unmutes the asset | Video, sound, url, or NDI asset, muted, visible | `asset.unmuted` |
| `asset.volume` | AssetVolumeChangeRequest | Changes the asset volume | Video, sound, url, or NDI asset | `asset.volume-changed` |
| `asset.seek` | AssetSeekRequest | Seeks to a normalized progress | Video or sound asset, visible | `asset.seek-ended` |
| `asset.page` | AssetPageChangeRequest | Goes to the requested page | Pdf asset, visible, page in range | `asset.page-changed` |
| `asset.move` | AssetMoveRequest | Moves the asset | Draggable asset, visible | `asset.moved` |
| `asset.resize` | AssetResizeRequest | Resizes and/or repositions the asset | Draggable asset, visible | `asset.resized` |
| `asset.animate-rect` | AssetAnimateRectRequest | Animates the asset's size and position | Asset is visible, not enlarged | `asset.resized` and/or `asset.moved` |
| `asset.enlarge` | AssetRequest | Enlarges the asset and shrinks other enlarged assets | Asset is visible, not already enlarged | `asset.resized` |
| `asset.shrink` | AssetRequest | Shrinks the asset if enlarged | Asset is visible, enlarged | `asset.resized` |
| `asset.crop` | AssetCropRequest | Crops the asset by margin values | Asset is visible, croppable asset type | `asset.cropped`, or `asset.resized` for a minimal change |
| `asset.content` | AssetContentChangeRequest | Changes the asset's content | Text or image asset, content differs from the current content | `asset.content-changed` |

## Url asset

Url assets show a web page. These requests also require a running story and the asset in the active scene. Every request that lands on a different page is confirmed by `asset.url-changed`.

| Topic and action | Payload message | Description | Prerequisites | Confirmed by |
|---|---|---|---|---|
| `asset.back` | AssetRequest | Goes one page back | Page has history to go back to | `asset.url-changed` |
| `asset.forward` | AssetRequest | Goes one page forward | Page has history to go forward to | `asset.url-changed` |
| `asset.refresh` | AssetRequest | Refreshes the page | | none, the page does not change |
| `asset.home` | AssetRequest | Loads the original url again | Page is not already the original url | `asset.url-changed` |
| `asset.change-url` | AssetUrlChangeRequest | Loads another page | Url the player can load, differs from the page currently loaded | `asset.url-changed` |
| `asset.touch` | UrlAssetTouchRequest | Simulates a touch on the page | | none |
| `asset.input` | UrlAssetInputRequest | Simulates keyboard input on the page | | none |
| `asset.keyboard-on` / `asset.keyboard-off` | AssetRequest | Enables or disables the on-screen keyboard | | none |
| `asset.navigation-on` / `asset.navigation-off` | AssetRequest | Enables or disables the navigation panel | | none |
| `asset.stream-on` / `asset.stream-off` | AssetRequest | Enables or disables streaming for the page | | none |

`asset.change-url` changes the page for the rest of the scene, not for the story. The authored url stays the asset's home, so `asset.back` and `asset.home` still return to it, and leaving the scene loads it again. A url asset in a group is never unloaded, so it keeps the requested page until the story stops. The player loads `http` and `https` urls.

A page a visitor navigates to themselves raises `asset.url-changed` as well, so an integration that only wants to see its own changes should match the url it requested.

## Trigger

| Topic and action | Payload message | Description | Prerequisites | Confirmed by |
|---|---|---|---|---|
| `trigger.fire` | TriggerFireRequest | Fires the trigger, the story defines what it does | Story running, trigger exists | none for the trigger itself, its story-defined effects report their own events |
