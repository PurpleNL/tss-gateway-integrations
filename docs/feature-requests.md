# Feature requests

Missing a request, an event, a query, or a field? For example an event that does not exist yet, like a scene transition having started. This repository is a read-only mirror, so the way in is a support ticket to TSC.

## How to submit one

1. Check the [requests](requests.md), [events](events.md), and [queries](queries.md) pages and the [proto files](../protos/) to confirm it does not already exist. Also check the tag you are pinned to against `latest`, it may already exist in a newer version.
2. Fill in the template below.
3. Send it to TSC support.

Requests with a clear use case and context are far easier to assess and prioritize than "please add X".

## Template

```
What we are building:
  (the integration and what it does for the end user)

What we are trying to achieve:
  (the concrete behaviour you want, e.g. "dim the lights the moment a
  scene transition starts")

What we are missing:
  (e.g. "a scene.transition-started event, scene.changed only fires
  after the transition finished")

What we tried instead:
  (workarounds you attempted and why they fall short)

Player version and pinned tag:
  (e.g. player 4.1.0, tag 4.1.0)
```
