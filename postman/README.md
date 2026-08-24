# Postman collection

`tss-play-gateway.postman_collection.json` is **generated** by `gen-collection.js`. Never edit the JSON by hand: edit the script and rerun it.

```
node postman/gen-collection.js
```

## How it stays in sync

- The request and query keys are read from the SDK source in `../sdk/csharp` (`GatewayRequests.cs`, `GatewayQueries.cs`), which mirrors the player repository. The script only maintains an example payload per key and fails when the payload tables and the SDK diverge, in either direction.
- The regeneration workflow reruns the script when the script or the mirrored SDK source changes, so a new request appears in the collection automatically once it has a payload entry.
- The protobuf decoder tables in the script (used to decode query replies) mirror `requests.proto` and `models.proto` by hand. Update them when the protos change; unknown fields decode as `field_<n>`, so a stale table degrades gracefully instead of failing.

See the documentation for how to import and use the collection.
