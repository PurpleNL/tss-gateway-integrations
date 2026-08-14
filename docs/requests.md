# Requests

A request asks the player to do something. It is fire-and-forget: publishing it tells you nothing about the outcome. The player honors a request only when its prerequisites hold, and reports what it did through [events](events.md).

## Publishing a request

- Publish on the `tss-requests-publish` exchange with a routing key from [Routing](routing.md), at screen, setup, or location scope.
- The body is the request's payload message, encoded as Protocol Buffers. Set `content_type` to `application/x-protobuf`.
- Requests without a payload message take an empty body.
- Set a message `expiration` (the C# SDK uses 5000 ms), so requests do not pile up and fire late when a player was temporarily unreachable.

The payload messages are defined in [`requests.proto`](../protos/requests.proto).

## Story

| Topic and action | Payload message | Description | Prerequisites |
|---|---|---|---|
| `story.start` | StoryStartRequest | Starts the story with the given uuid, optionally a specific version | No story running |
| `story.stop` | none | Stops the running story | Story running |
| `story.volume` | VolumeChangeRequest | Changes the master volume | Story running |

## Scene

All scene requests require a running story.

| Topic and action | Payload message | Description | Prerequisites |
|---|---|---|---|
| `scene.change` | SceneChangeRequest | Changes to the requested scene | Scene exists, scene is not the active scene |
| `scene.next` | none | Goes to the next non-screensaver scene | |
| `scene.previous` | none | Goes to the previous non-screensaver scene | |
| `scene.skip-transition` | none | Skips the running transition | A transition is running |

## Asset

All asset requests require a running story, an existing asset, and except for `asset.content` the asset must be in the active scene. Assets are addressed by their integration id.

| Topic and action | Payload message | Description | Prerequisites |
|---|---|---|---|
| `asset.show` | AssetRequest | Shows the asset | Asset is invisible |
| `asset.hide` | AssetRequest | Hides the asset | Asset is visible |
| `asset.play` | AssetRequest | Plays the asset | Video or sound asset, paused, visible |
| `asset.pause` | AssetRequest | Pauses the asset | Video or sound asset, playing |
| `asset.mute` | AssetRequest | Mutes the asset | Video, sound, url, or NDI asset, unmuted |
| `asset.unmute` | AssetRequest | Unmutes the asset | Video, sound, url, or NDI asset, muted, visible |
| `asset.volume` | AssetVolumeChangeRequest | Changes the asset volume | Video, sound, url, or NDI asset |
| `asset.seek` | AssetSeekRequest | Seeks to a normalized progress | Video or sound asset, visible |
| `asset.page` | AssetPageChangeRequest | Goes to the requested page | Pdf asset, visible |
| `asset.move` | AssetMoveRequest | Moves the asset | Draggable asset, visible |
| `asset.resize` | AssetResizeRequest | Resizes and/or repositions the asset | Draggable asset, visible |
| `asset.animate-rect` | AssetAnimateRectRequest | Animates the asset's size and position | Asset is visible |
| `asset.enlarge` | AssetRequest | Enlarges the asset and shrinks other enlarged assets | Asset is visible |
| `asset.shrink` | AssetRequest | Shrinks the asset if enlarged | Asset is visible |
| `asset.crop` | AssetCropRequest | Crops the asset by margin values | Asset is visible |
| `asset.content` | AssetContentChangeRequest | Changes the asset's content | Text or image asset |

## Url asset

Url assets show a web page. These requests also require a running story and the asset in the active scene.

| Topic and action | Payload message | Description |
|---|---|---|
| `asset.back` | AssetRequest | Goes one page back |
| `asset.forward` | AssetRequest | Goes one page forward |
| `asset.refresh` | AssetRequest | Refreshes the page |
| `asset.home` | AssetRequest | Loads the original url again |
| `asset.touch` | UrlAssetTouchRequest | Simulates a touch on the page |
| `asset.input` | UrlAssetInputRequest | Simulates keyboard input on the page |
| `asset.keyboard-on` / `asset.keyboard-off` | AssetRequest | Enables or disables the on-screen keyboard |
| `asset.navigation-on` / `asset.navigation-off` | AssetRequest | Enables or disables the navigation panel |
| `asset.stream-on` / `asset.stream-off` | AssetRequest | Enables or disables streaming for the page |

## Trigger

| Topic and action | Payload message | Description | Prerequisites |
|---|---|---|---|
| `trigger.fire` | TriggerFireRequest | Fires the trigger, the story defines what it does | Story running, trigger exists |
