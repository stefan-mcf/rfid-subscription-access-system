# RFID Subscription and Access Platform

Connect customer accounts and per-vehicle subscriptions to RFID access decisions, local edge state and operator controls.

**Status:** operational system confirmed by the project owner as of 24 October 2026; the public package contains reference code and historical/synthetic evidence.

**My contribution:** application architecture, .NET backend, customer and operator interfaces, AWS migration, edge integration and PLC/CODESYS control boundaries.

[Architecture](docs/architecture.md) · [Case study](docs/case-study.md) · [Current delivery status](docs/current-status.md)

![Subscription, access-decision and local-control system diagram](assets/01-subscription-to-access-decision.png)

*Public system diagram. The [case study](docs/case-study.md) retains the interface and migration views.*

## How it works

1. Customer identity, vehicle, plan, billing and tag state remain separate.
2. Signed subscription lifecycle events are deduplicated and projected in sequence.
3. A tag read produces a short-lived, reason-coded allow/deny decision.
4. The local edge service uses a versioned tag snapshot, repeat-read controls and a SQLite-backed outbox.
5. Output intent passes mode, busy-state, expiry and acknowledgement gates before the PLC/CODESYS path.

An active subscription, an eligible tag, an output request and a controller acknowledgement represent different states. The system preserves those distinctions during connectivity loss and duplicate delivery.

The AWS data migration moved hosted PostgreSQL to private Aurora Serverless v2. Recorded acceptance covered data parity, application reads/writes, backup, idle/resume behavior and old-instance retirement; it did not establish long-term savings.

## Implementation

The public package includes seven JSON Schemas, synthetic examples, architecture and an executable .NET 10 reference model:

```bash
dotnet run --project src/Rfid.ReferenceModel/Rfid.ReferenceModel.csproj
dotnet test Rfid.SubscriptionAccess.Reference.slnx
```

Tests cover fail-closed decisions, expiry, repeat lockout, ordered subscription projection, snapshot replacement, outbox acknowledgement and output-mode controls.

## Validation

Requires Node.js 24+ and .NET 10.

```bash
npm ci
npm run validate
npm run validate:dotnet
```

The validator checks schemas, fixtures, local links, asset integrity and the [claim matrix](verification/claim-matrix.json). Reference-model tests do not substitute for a new physical acceptance run.

## Scope and current state

Stefan has confirmed the delivered system works as intended. [Current status](docs/current-status.md) separates that owner confirmation from the dated public verification package; the earlier commissioning-pending wording is historical.

Client runtime source, network/device configuration, provider setup and field runbooks remain private. Published billing examples use Stripe sandbox and do not establish live charging, production pricing, revenue or savings.

## Documentation

- [Historical interface and migration evidence](docs/evidence.md)
- [System case study](docs/case-study.md) and [architecture](docs/architecture.md)
- [Customer account security](docs/customer-account-security.md)
- [Subscription reconciliation](docs/subscription-reconciliation.md)
- [Local edge and outbox](docs/local-edge-and-outbox.md)
- [Field control boundaries](docs/field-safety-and-commissioning.md)
- [Aurora migration](docs/aurora-migration.md)
- [Verification matrix](docs/verification-matrix.md) and [publication boundary](docs/publication-boundary.md)
