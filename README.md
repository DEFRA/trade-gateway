# trade-gateway

Core delivery C# ASP.NET backend template.

* [Authentication](#authentication)
* [Authorisation](#authorisation)
* [Install MongoDB](#install-mongodb)
* [Inspect MongoDB](#inspect-mongodb)
* [Testing](#testing)
* [Running](#running)
* [Dependabot](#dependabot)


### Docker Compose

A Docker Compose template is in [compose.yml](compose.yml).

A local environment with:

- Localstack for AWS services (S3, SQS)
- Redis
- MongoDB
- This service.
- A commented out frontend example.

```bash
docker compose up --build -d
```

Note: running docker locally requires the following environment variables in a `.env` file, which
compose passes into the container. Names are the configuration binding path with `__` between
segments — note `TRACESNT`, not `TRACES_NT`, since the underscore would make it a different section.

```env
TRACESNT__BASEURL=traces_base_url

# The default TracesNT account, used by the CHED, EU-INTRA and reference-data ports
TRACESNT__CREDENTIALS__DEFAULT__USERNAME=traces_username
TRACESNT__CREDENTIALS__DEFAULT__AUTHENTICATIONKEY=traces_authentication_key
TRACESNT__CREDENTIALS__DEFAULT__WEBSERVICECLIENTID=traces_client_id

# The customs account, used by the quantity-management port
TRACESNT__CREDENTIALS__CUSTOMS__USERNAME=traces_customs_username
TRACESNT__CREDENTIALS__CUSTOMS__AUTHENTICATIONKEY=traces_customs_authentication_key
TRACESNT__CREDENTIALS__CUSTOMS__WEBSERVICECLIENTID=traces_customs_client_id

# Cognito — for clients deployed outside CDP
AUTHENTICATION__COGNITO__AUTHORITY=https://cognito-idp.<region>.amazonaws.com/<user-pool-id>
AUTHENTICATION__COGNITO__SCOPE=<cognito-scope>

# STS — for CDP-internal services authenticating via IAM role
AUTHENTICATION__STS__AUTHORITY=https://<sts-oidc-issuer>
AUTHENTICATION__STS__AUDIENCE=trade-gateway
```

Both authentication authorities must be reachable at startup. See [ADR-0004](docs/adr/0004-dual-issuer-authentication.md) for the full authentication design, including the IAM authorization requirement for STS clients.


A more extensive setup is available in [github.com/DEFRA/cdp-local-environment](https://github.com/DEFRA/cdp-local-environment)

### Authentication

The API authenticates machine-to-machine (M2M) clients via JWT bearer tokens from two
issuers. See [ADR-0004](docs/adr/0004-dual-issuer-authentication.md) for the full design.

- **Cognito** — clients deployed outside CDP. Tokens must carry the configured
  `scope` claim.
- **STS** — CDP-internal services authenticating via their IAM role. Validated on
  issuer and audience; STS tokens carry no scope.

A `MultiIssuer` policy scheme inspects the token's `iss` claim and forwards to the
matching scheme. Both schemes feed the `ApiAccess` policy, which requires an
authenticated principal with the correct scheme + scope/audience. Requests with no
valid token receive **401 Unauthorized**.

Configuration (see env vars in [Docker Compose](#docker-compose) above):

```json
{
  "Authentication": {
    "Cognito": { "Authority": "...", "Scope": "trade-gateway-resource-srv/access" },
    "Sts":     { "Authority": "...", "Audience": "trade-gateway" }
  }
}
```

Both authorities must be reachable at startup. In local development
[LocalTokenServer](src/Api/Utils/LocalTokenServer.cs) stands in for both issuers, signing with a
single in-memory key it publishes over OIDC discovery so each scheme can verify what it mints:

| Prefix | Endpoints |
|--------|-----------|
| `/local/cognito` | OIDC discovery, JWKS, and `POST /token` — form fields `scope`, `audience`, `sub` |
| `/local/sts` | OIDC discovery, JWKS, and `sts:GetWebIdentityToken` mounted on the prefix itself |

The STS half deliberately has no `/token` endpoint: real STS has no such operation. Localstack does
not implement `GetWebIdentityToken` either, so a service that authenticates through it — such as
`trade-gateway-publisher` — points the AWS SDK at this app instead, with no code change on its side:

```env
AWS_ENDPOINT_URL_STS=http://localhost:5000/local/sts
```

Tokens it issues carry `sub: trade-gateway-publisher`, which
[appsettings.Development.json](src/Api/appsettings.Development.json) grants READ on the certificate
collections. [LocalStsEndpointTests](tests/Api.Tests/Authorization/LocalStsEndpointTests.cs) drives
the endpoint through the real AWS SDK client so the response envelope cannot drift out of shape
with the SDK's unmarshaller.

### Authorisation

Downstream of `ApiAccess`, a fine-grained, **per-principal, per-resource, per-action**
layer enforces least privilege — every principal is granted only the resources it needs.
See [ADR-0005](docs/adr/0005-fine-grained-authorisation.md) for the full design.

```
Request → Authentication → ApiAccess (scheme + scope) → Fine-grained authz → Handler
```

Every principal (Cognito or STS) must have an explicit entry; there is no implicit
access. Denials return **403 Forbidden**. `/health`, the OpenAPI docs
(`/.well-known/openapi/**`) and `/redoc` bypass authorisation.

**Actions** are derived from the HTTP method — `GET` → `READ`, `POST`/`PUT` → `WRITE`.
Any other method is denied by default.

**Resources** are URL path patterns, matched case-insensitively, with two wildcards:

| Operator | Meaning | Example | Matches |
|----------|---------|---------|---------|
| `*`  | exactly one path segment | `/certificates/intras/*`  | `/certificates/intras/ABC123` but **not** the collection `/certificates/intras` |
| `**` | any suffix (zero or more segments) | `/certificates/intras/**` | the collection **and** any item beneath it |

A pattern with no wildcards is an exact match. This makes it possible to grant access to
an instance (`/certificates/intras/*` or an exact id) without exposing the collection
listing.

The config has two sections with different lifetimes:

- **`Permissions`** — alias → resource/action grants. Environment-agnostic; the same
  grants apply in every environment.
- **`Principals`** — alias → JWT `sub`. Environment-specific; the same alias resolves to
  a different `sub` in dev, test, prod, …

Locally and in tests they are configured in the app's settings files — `Permissions` in
the common `appsettings.json` and `Principals` in the environment-specific
`appsettings.Development.json`. ASP.NET Core merges them into one `Authorization` config:

```json
// appsettings.json (Permissions) + appsettings.Development.json (Principals)
{
  "Authorization": {
    "Permissions": {
      "ched-importer": [
        { "Actions": ["READ", "WRITE"], "Resource": "/certificates/cheds/**" },
        { "Actions": ["READ"],          "Resource": "/certificates/intras/**" }
      ],
      "reference-data-reader": [
        { "Actions": ["READ"], "Resource": "/reference-data/**" }
      ]
    },
    "Principals": {
      "ched-importer":         "arn:aws:sts::123456789012:assumed-role/ched-importer-role/session",
      "reference-data-reader": "b2c3d4e5-f6a7-8901-bcde-f12345678901"
    }
  }
}
```

The `sub` style differs by issuer: an **STS** `sub` is the assumed IAM role ARN
(`ched-importer` above), while a **Cognito** M2M `sub` is the client's UUID
(`reference-data-reader` above). Both are modelled identically — only the alias and the
`sub` value differ.

Startup fails fast if a `Permissions` alias has no matching `Principals` entry, or a
principal has an empty permissions list.

> **CDP deployment.** In deployed environments config is injected as environment
> variables, which merge over `appsettings.json`. The same split maps onto CDP's config
> files: `Permissions` go in the **shared/default env settings file** (applied to all
> environments), and `Principals` go in the **per-deployed-environment file** (one per
> dev/test/prod). Array entries require zero-based contiguous indices:
>
> ```env
> # default env settings (shared) — Permissions
> AUTHORIZATION__PERMISSIONS__CHED-IMPORTER__0__ACTIONS__0=READ
> AUTHORIZATION__PERMISSIONS__CHED-IMPORTER__0__ACTIONS__1=WRITE
> AUTHORIZATION__PERMISSIONS__CHED-IMPORTER__0__RESOURCE=/certificates/cheds/**
>
> # per-environment settings (e.g. dev) — Principals
> AUTHORIZATION__PRINCIPALS__CHED-IMPORTER=arn:aws:sts::123456789012:assumed-role/ched-importer-role/session
> AUTHORIZATION__PRINCIPALS__REFERENCE-DATA-READER=b2c3d4e5-f6a7-8901-bcde-f12345678901
> ```

### MongoDB

#### MongoDB via Docker

See above.

```
docker compose up -d mongodb
```

#### MongoDB locally

Alternatively install MongoDB locally:

- Install [MongoDB](https://www.mongodb.com/docs/manual/tutorial/#installation) on your local machine
- Start MongoDB:
```bash
sudo mongod --dbpath ~/mongodb-cdp
```

#### MongoDB in CDP environments

In CDP environments a MongoDB instance is already set up
and the credentials exposed as enviromment variables.


### Inspect MongoDB

To inspect the Database and Collections locally:
```bash
mongosh
```

You can use the CDP Terminal to access the environments' MongoDB.

### Testing

Run the tests with:

Tests run by running a full `WebApplication` backed by [Ephemeral MongoDB](https://github.com/asimmon/ephemeral-mongo).
Tests do not use mocking of any sort and read and write from the in-memory database.

```bash
dotnet test
````

### Running

Run CDP-Deployments application:
```bash
dotnet run --project Api --launch-profile Development
```

### SonarCloud

Example SonarCloud configuration are available in the GitHub Action workflows.

### Dependabot

We have added an example dependabot configuration file to the repository. You can enable it by renaming
the [.github/example.dependabot.yml](.github/example.dependabot.yml) to `.github/dependabot.yml`


### About the licence

The Open Government Licence (OGL) was developed by the Controller of Her Majesty's Stationery Office (HMSO) to enable
information providers in the public sector to license the use and re-use of their information under a common open
licence.

It is designed to encourage use and re-use of information freely and flexibly, with only a few conditions.
