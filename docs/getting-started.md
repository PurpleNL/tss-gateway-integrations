# Getting started

This page covers what you need before any integration can talk to a TSS player, regardless of language.

## What you need

- Access to a TSS gateway: a host, a username and password, and the location and setup uuids of the players you want to drive. These are provided by TSC.
- A running player with at least one story synced to it.

## Reaching the gateway

The gateway runs on-site, next to the players. To talk to it, your integration must run on the same network as the players, and connect to the gateway host on the standard AMQP port 5672.

Some locations also have a remote gateway. It mirrors all traffic with the on-site gateway, so your integration can run anywhere instead of on-site. Not every location has one, contact TSC support if you need it. A remote gateway depends on the location having internet: while the location is offline, messages do not arrive.

## Choose your path

- Building in C#? Use the SDK, see the [C# getting started](csharp/getting-started.md).
- Building in another language? Talk to the gateway directly with any RabbitMQ client. [Concepts](concepts.md) explains the model, then [Routing](routing.md), [Requests](requests.md), [Events](events.md), and [Queries](queries.md) describe the protocol. The [proto files](../protos/) define every message.

Either way, read the [best practices](best-practices.md) before you ship.
