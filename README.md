# TSS Gateway Integrations

The entry point for integrating with the TSS gateway: the protocol files, the C# SDK source, and integrator documentation.

The gateway is the single integration point for anything that wants to talk to TSS players: sending requests, receiving responses and events, and running queries.

## Layout

| Path | Contents |
|---|---|
| `protos/` | The protocol files (Protocol Buffers). The language-neutral contract, including reference documentation in the comments. |
| `sdk/csharp/` | Source of the C# SDK, published as the `TSS.Gateway.Sdk` NuGet package. Includes an example integration under `examples/`. |
| `docs/` | Integrator documentation, authored in this repository. |

## Documentation

- [Getting started](docs/getting-started.md): from nothing to a first request answered by a player.
- [Concepts](docs/concepts.md): the request > player > event model, addressing, and versioning.
- [Example integration](sdk/csharp/examples/README.md): a complete commented walkthrough.

## Versioning

Every version of the TSS player has a matching tag in this repository, so the protocol files, the SDK source, and the player version always line up.

- The `stable` branch follows TSS Play releases. Release tags look like `4.1.0`. Pin one of these.
- The `latest` branch follows ongoing development. Development tags look like `4.0.8-ef8b351e`.

## Read-only mirror

The synced content in this repository is maintained in the TSS Play repository and pushed here automatically. Pull requests can therefore not be accepted; the next sync would overwrite them. For questions, problems, or change requests, contact Storytelling Company support.

## License

The contents of this repository are licensed under the Apache License 2.0, see [LICENSE](LICENSE). Access to a TSS gateway itself is arranged contractually.
