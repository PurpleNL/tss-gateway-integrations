# Routing

The gateway uses RabbitMQ topic exchanges. Every message carries a routing key that says who sent it and which player it concerns, and the exchange delivers it to everyone whose binding matches.

## Routing key

A fully scoped routing key has seven segments:

```
tss.<client>.<location>.<setup>.<screen>.<topic>.<action>
```

| Segment | Description | Example(s) |
|---|---|---|
| `tss` | Fixed prefix for TSS player traffic | `tss` |
| client | Who sent the message: your client id, or `play` for messages from the player. Must be a single segment: no dots, stick to letters, digits, and dashes. A dotted client id makes the key match nothing, and the message is silently discarded | `my-app`, `control`, `play` |
| location | Uuid of the location the message concerns | `16c3b565-c107-4fe8-982c-95c93f6c67cf` |
| setup | Uuid of the setup | `9e060128-9cb2-474f-b45a-ca54496d81a8` |
| screen | Screen id of the setup, starting at 0 | `0`, `1` |
| topic | Subject of the message | `story`, `scene`, `asset`, `trigger`, `instance` |
| action | What is requested to happen, or what happened | `start`, `stop`, `changed`, `fire` |

## Scopes

A request does not have to target a single screen. Leaving segments off the middle widens the scope, and the player accepts all three shapes:

| Scope | Shape | Reaches |
|---|---|---|
| Screen | `tss.<client>.<location>.<setup>.<screen>.<topic>.<action>` | One screen |
| Setup | `tss.<client>.<location>.<setup>.<topic>.<action>` | Every screen of the setup |
| Location | `tss.<client>.<location>.<topic>.<action>` | Every screen at the location |

Example: `tss.my-app.16c3b565-c107-4fe8-982c-95c93f6c67cf.9e060128-9cb2-474f-b45a-ca54496d81a8.story.stop` stops the story on every screen of that setup.

Events from the player are always fully scoped: they name the exact screen they happened on, so a setup or location scoped request is answered by one event per screen. [Queries](queries.md) must always be screen scoped.

## Exchanges

You always publish to a `-publish` exchange and consume from a `-listen` exchange. The pairs exist so a local and a remote gateway can mirror each other's traffic: whatever is published on one side arrives on the `-listen` exchanges of both.

| Exchange | Direction | Carries |
|---|---|---|
| `tss-requests-publish` | You publish | [Requests](requests.md) |
| `tss-events-listen` | You consume | [Events](events.md) |
| `tss-api-publish` | You publish | [Queries](queries.md) |
| `tss-response-listen` | You consume | Query responses |

All exchanges are topic exchanges and durable.
