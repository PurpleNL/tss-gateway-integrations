# Queries

A query reads state from the player: the story status, the available stories. Unlike [requests](requests.md), every query gets a response.

## Running a query

Queries are request/response over two exchanges, correlated by you:

1. Declare an exclusive, auto-delete queue of your own and bind it on the `tss-response-listen` exchange with a reply routing key that is unique to your client, for example `tss.response.<client>.<location>.<setup>.<screen>.<random>`. The exchanges already exist on the gateway, see the declare note in [Events](events.md).
2. Publish the query on the `tss-api-publish` exchange with these properties:
   - The routing key must be **screen scoped** (see [Routing](routing.md)). Setup and location scopes do not work for queries: every screen would answer with the same correlation id.
   - `correlation_id`: a unique id per query, used to match the response.
   - `content_type`: `application/x-protobuf`.
   - `expiration`: how long the query may wait for the player (the C# SDK uses 15000 ms).
   - Header `tss-reply-to`: your reply routing key from step 1.
3. The player publishes a Response message to your reply routing key, with the same `correlation_id`.

Responses can arrive out of order, always match on the correlation id instead of assuming the answer order.

The body of every response is the Response envelope from [`requests.proto`](../protos/requests.proto): a oneof that carries either the result or an Error with a code and message.

## Errors

An Error response means the player rejected the query. The code tells you why, the message carries detail. Codes you can encounter include 400 for an unknown query, and story-specific codes such as 147 when the requested story does not exist. The list is not fixed, handle unknown codes gracefully.

## Available queries

The request payload is empty unless a message is named. The response column names the field of the Response envelope that carries the answer.

| Topic and action | Request message | Response field |
|---|---|---|
| `instance.info` | none | instanceInfo |
| `story.status` | none | storyStatus |
| `story.available-stories` | AvailableStoriesRequest | availableStories |
| `story.available-story` | AvailableStoryRequest | availableStory |
| `story.available-scenes` | none | availableScenes |
| `story.corrupted-assets` | none | corruptedAssets |
| `story.data` | StoryDataRequest | storyData |
| `scene.status` | none | currentState |

Note for `story.available-stories`: set the `limit` field explicitly. The player returns at most `limit` stories, and the protobuf default of 0 returns none.
