# Best practices

Things that make an integration reliable, and mistakes we see in practice.

## A request is a request, not a command

The player decides whether it can honor a request. Do not assume publishing means it happened: wait for the matching event, and treat a timeout as "the player did not do it". The [prerequisites per request](requests.md) tell you why a request may be ignored.

## Subscribe to events instead of polling

State changes are pushed to you on the events exchange. Do not poll queries in a loop to detect changes, and never at high frequency. Queries are for reading state at moments you actually need it, for example when your application starts. If you need liveness, the player already sends a periodic `instance.heartbeat`.

## Register your event wait before publishing the request

If you publish a request first and start listening afterwards, the event can arrive in between and you miss it. Set up the wait, then publish.

## One connection, reused

Connect once and keep the connection open. Do not open a connection (or channel) per message. Reconnect with a backoff when the connection drops, and remember the gateway can be temporarily unreachable, for example when a location loses internet on a remote gateway.

## Pick one client id and keep it

Your client id is woven into routing keys and queue names. Changing it between deployments leaves orphaned queues behind and makes traffic hard to trace. One application, one client id, stable across versions.

## Handle a timeout as an answer

No event within your timeout means the player did not honor the request. Do not retry blindly: the cause is usually a prerequisite that does not hold (no story running, asset not in the active scene), and a retry storm makes debugging harder for everyone.

## Pin a stable tag

Build against the `stable` branch, pinned to the release tag that matches your player version. Building against `latest` means the protocol can change under you before your player does.
