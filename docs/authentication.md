# Authentication

Connecting to a gateway requires a username and password. Credentials are issued per integration by TSC, contact support to get yours.

- Keep the credentials out of your source code and repositories, treat them like any other secret.
- Use one set of credentials and one client id per application. The client id is woven into your routing keys and queue names, which is what keeps your messages and responses separate from other clients. See [Routing](routing.md) for the characters a client id may use.
- A local gateway speaks plain AMQP on port 5672, virtual host `/`. The connection details you receive from TSC state anything that differs, for example for a remote gateway.
- Traffic between a local and a remote gateway is encrypted with TLS by the gateways themselves.

There is no anonymous access. A connection with wrong or missing credentials is refused by the gateway.
