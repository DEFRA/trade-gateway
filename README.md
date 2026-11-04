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

# The customs office this gateway speaks for, sent on every customs quantity-management call.
# 1-8 alphanumeric characters; the app will not start without it.
TRACESNT__CUSTOMSOFFICEREFERENCENUMBER=traces_customs_office_reference_number

# The default TracesNT account, used by the CHED, EU-INTRA, DOCOM ports
TRACESNT__CREDENTIALS__DEFAULT__USERNAME=traces_username
TRACESNT__CREDENTIALS__DEFAULT__AUTHENTICATIONKEY=traces_authentication_key
TRACESNT__CREDENTIALS__DEFAULT__WEBSERVICECLIENTID=traces_client_id

# The customs account, used by the quantity-management port
TRACESNT__CREDENTIALS__CUSTOMS__USERNAME=traces_customs_username
TRACESNT__CREDENTIALS__CUSTOMS__AUTHENTICATIONKEY=traces_customs_authentication_key
TRACESNT__CREDENTIALS__CUSTOMS__WEBSERVICECLIENTID=traces_customs_client_id

# The reference data TracesNT account, used by the reference-data port
TRACESNT__CREDENTIALS__REFERENCEDATA__USERNAME=traces_reference-data_username
TRACESNT__CREDENTIALS__REFERENCEDATA__AUTHENTICATIONKEY=traces_reference-data_authentication_key
TRACESNT__CREDENTIALS__REFERENCEDATA__WEBSERVICECLIENTID=traces_reference-data_client_id

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

Resources sit under three top-level prefixes: `/certificates/**`, `/reference-data/**` and
`/customs/**`. A `**` grant is a standing commitment to everything ever added beneath it, so
customs quantity management lives beside the certificates rather than under them — a `ched-reader`
must not gain customs quantity data by having a resource added to a prefix it already holds.
See [ADR-0006](docs/adr/0006-customs-quantity-management.md).

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

### CHED attachments

`GET certificates/cheds/{id}/attachments/{attachmentId}` does not call TRACES inline. Each attachment
has a record in the `ched_attachments` Mongo collection:

1. **First request**: the record is created (`Requested`), a `ChedAttachmentDownloadRequested`
   message is published to the `trade_gateway_ched_attachments_internal.fifo` SNS topic
   (de-duplication and group id = the attachment id), and the endpoint returns **202** with
   `Retry-After`.
2. **Consumer** (`ChedAttachmentDownloadConsumer`, reading
   `trade_gateway_ched_attachments_internal_downloader.fifo`): fetches the CHED and attachment from
   TRACES, then hands the bytes to [cdp-uploader](https://github.com/DEFRA/cdp-uploader) (`/initiate`
   plus a binary upload), so the file is malware scanned before it reaches S3. The record moves to
   `Downloading`, then `Scanning`.
3. **Scan callback**: cdp-uploader posts to `POST /attachments/scan-results`. This endpoint is
   anonymous, and it only acts on a callback that carries the random token stored with the
   attachment's current upload. The record becomes `Available` (with its S3 key) or `Failed`
   (`FILE_VIRUS`, `FILE_EMPTY`, ...).
4. **Later requests**: `Available` is streamed from S3 with its original content type and file name.
   `Failed` returns a problem with an `errorCode`: 404 not found or draft, 403, 422 scan rejection,
   500, or 502 TRACES/uploader failure. Anything still in progress returns 202.

Publishing and consuming use `Defra.TradeImports.Messaging`, and the uploader client is
`Defra.TradeImports.CdpUploader`. Both packages come from
[trade-imports-common](https://github.com/DEFRA/trade-imports-common).

**Supported file types** are listed in `ChedAttachments:AllowedFileTypes` (appsettings.json).
Only PDF is allowed for now:

```json
"AllowedFileTypes": [ { "MimeType": "application/pdf", "Extensions": [ ".pdf" ] } ]
```

To support another type, add an entry. TRACES' content type decides whether a file is allowed. The
extensions are only used when TRACES gives no specific type (none, or `application/octet-stream`).
A file whose type isn't on the list fails with `UNSUPPORTED_FILE_TYPE` (422) before anything is
uploaded. It is downloaded again on its next request once its type has been added. The list is also
sent to cdp-uploader as `mimeTypes`. The uploader only checks the type we declare, so the scan
callback also checks the type it detected. A file whose content isn't an allowed type (e.g. a PNG
named `.pdf`) fails with `FILE_INVALID_TYPE` (422).

Errors are recorded on the record (`error.source`, `code`, `message`, internal `detail`). Transient
failures (TRACES unavailable, uploader errors) are also rethrown, so SQS redelivers the message, up
to the DLQ's `maxReceiveCount`. A request stuck in progress, or a failure other than a scan
rejection, is queued again when it is requested after `ChedAttachments:RetryAfter` (15 minutes).

Retention: Mongo records expire after **180 days** (TTL index `CreatedAtTtlIdx` on `createdAt`).
Bucket objects expire after **190 days**, so a record never points at a deleted object.

Locally, `docker compose up` adds `redis` and `cdp-uploader` (the scan is mocked, and any file name
containing `virus` is rejected). floci creates the topic, the queue and its DLQ, and the
`trade-gateway-documents` bucket with its 190-day rule.

In CDP, configure the following:

* `cdp-tenant-config`: the FIFO topic and queue (subscription with raw delivery, a DLQ, and a
  visibility timeout of at least `ChedAttachments:VisibilityTimeoutSeconds`). Also the bucket, with
  an expiration rule of `"days": "190"`, and IAM permissions for the queue, topic and bucket.
* Ask #cdp-support to allow cdp-uploader to write to the bucket.
* `cdp-app-config`: `ChedAttachments__TopicArn`, `ChedAttachments__QueueUrl`,
  `ChedAttachments__S3Bucket`, `CdpUploader__BaseUrl`
  (`https://cdp-uploader.{env}.cdp-int.defra.cloud`) and `CdpUploader__CallbackUrl`
  (`https://trade-gateway.{env}.cdp-int.defra.cloud/attachments/scan-results`).

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
