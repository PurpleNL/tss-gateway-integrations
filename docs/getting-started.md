# Getting started

This page covers what you need before any integration can talk to a TSS player, regardless of language.

## What you need

- Access to a TSS gateway: a host, a username and password, and the location and setup uuids of the players you want to drive. These are provided by TSC.
- A running player with at least one story synced to it.

## Reaching the gateway

The gateway runs on-site, next to the players. To talk to it, your integration must run on the same network as the players, and connect to the gateway host on the standard AMQP port 5672, virtual host `/`.

There can also be a remote gateway. It is per customer, shared by all of that customer's locations, and mirrors all traffic with the on-site gateways, so your integration can run anywhere instead of on-site. Not every customer has one, contact TSC support if you need it. A remote gateway depends on each location having internet: while a location is offline, messages to and from its players do not arrive.

## Choose your path

- Building in C#? Use the SDK, see the [C# getting started](csharp/getting-started.md).
- Building in another language? Talk to the gateway directly with any RabbitMQ client. [Concepts](concepts.md) explains the model, then [Routing](routing.md), [Requests](requests.md), [Events](events.md), and [Queries](queries.md) describe the protocol. The [proto files](../protos/) define every message.

Either way, read the [best practices](best-practices.md) before you ship.
