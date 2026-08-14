# Best practices

Things that make an integration reliable, and mistakes we see in practice.

## A request is a request, not a command

The player decides whether it can honor a request. Do not assume publishing means it happened: wait for the confirming event, and treat a timeout as "the player did not do it". The [requests page](requests.md) tells you which event confirms which request and why a request may be ignored. Not every request has a confirming event (url asset requests, `trigger.fire`), for those a [query](queries.md) is the only way to verify.

## Subscribe to events instead of polling

State changes are pushed to you on the events exchange. Do not poll queries in a loop to detect changes, and never at high frequency. Queries are for reading state at moments you actually need it, for example when your application starts. If you need liveness, the player already sends an `instance.heartbeat` every second.

## Register your event wait before publishing the request

If you publish a request first and start listening afterwards, the event can arrive in between and you miss it. Set up the wait, then publish.

## One connection, reused

Connect once and keep the connection open. Do not open a connection (or channel) per message. Reconnect with a backoff when the connection drops, and remember the gateway can be temporarily unreachable, for example when a location loses internet on a remote gateway.

## Pick one client id and keep it

Your client id is woven into routing keys and queue names. Changing it between deployments makes traffic hard to trace and breaks anything that filters on your id. One application, one client id, stable across versions.

## Handle a timeout as an answer

No event within your timeout usually means the player did not honor the request, most often because a prerequisite does not hold (no story running, asset not in the active scene). Do not retry blindly, a retry storm makes debugging harder for everyone. Around story starts, stops, and scene changes a confirming event can also go missing even though the request was honored, see the delivery notes in [Events](events.md), so verify with a query before drawing conclusions there.

## Pin a stable tag

Build against the `stable` branch, pinned to the release tag that matches your player version. Building against `latest` means the protocol can change under you before your player does.
