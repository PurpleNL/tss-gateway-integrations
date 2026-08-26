# Postman collection

The [postman folder](https://github.com/PurpleNL/tss-gateway-integrations/tree/latest/postman) contains a ready-made Postman collection with every request and query, so you can drive a player and explore the protocol without writing code.

Postman speaks HTTP, not AMQP, so the collection publishes through the RabbitMQ management API that runs next to the gateway's AMQP port.

## What you need

- Network access to the gateway host on port 15672, the RabbitMQ management API.
- Gateway credentials that can use the management API.
- The location and setup uuids and the screen id of the player, see [Getting started](getting-started.md).

## Import and setup

1. Download `tss-play-gateway.postman_collection.json` from the [postman folder](https://github.com/PurpleNL/tss-gateway-integrations/tree/latest/postman) and import it in Postman (File > Import).
2. On the collection's Variables tab, fill in `base_url` (`http://<gateway-host>:15672`), `user`, `pass`, `location_uuid`, `setup_uuid`, and `screen_id`.
3. Fill the remaining variables when a request needs them: `story_uuid`, `asset_integration_id`, `asset_uuid`.

## Using it

- Commands publish to the exchanges described in [Routing](routing.md). Payloads are JSON: the player accepts JSON as well as protobuf. Send enums as integers or C# PascalCase names, not proto SCREAMING_CASE.
- Queries handle the reply plumbing for you: a pre-request script creates and binds a durable reply queue, and the test script polls it, decodes the protobuf response, and shows it as JSON in the response's Visualize tab and the console. "Get replies" drains the queue manually when a reply arrives late.

## Changing the collection

The collection JSON is generated, never edit it by hand. The request and query list follows the SDK automatically; example payloads live in `gen-collection.js`. See the [postman folder](https://github.com/PurpleNL/tss-gateway-integrations/tree/latest/postman) README.
