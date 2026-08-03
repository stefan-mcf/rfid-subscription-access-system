# Architecture and failure boundaries

## Components

| Component | Responsibility |
| --- | --- |
| Customer web | Account verification, plans, per-vehicle subscriptions, checkout, and Billing Portal access |
| Stripe sandbox | Temporary checkout, invoice, subscription, and signed lifecycle events |
| AWS event broker | Ordered, duplicate-resistant subscription-event processing and exception handling |
| Subscription API | Account, subscription, claim-code, billing-event, and webhook state |
| Aurora PostgreSQL | Durable customer, subscription, tag, event, decision, and output-intent records |
| Access decision API | Tag lookup, entitlement checks, repeat protection, and wash-program decisions |
| Edge service | Local reader polling, decision requests, and buffered event delivery |
| PLC bridge | Bounded command and acknowledgement interface |
| Operator dashboard | Runtime status, tag management, decisions, events, and wash controls |

## Subscription sequence

1. A verified account selects a plan for a specific vehicle.
2. The backend creates a Stripe sandbox Checkout session using temporary test pricing.
3. Signed lifecycle events enter an ordered AWS processing path.
4. Invalid signatures are rejected and repeated events do not advance state twice.
5. Current subscription state is reconciled into PostgreSQL.
6. Customer and operator surfaces read the same durable commercial state.

## Access decision sequence

1. The reader reports a normalised tag identifier to the local edge service.
2. The edge service requests an access decision with a correlation ID.
3. The API checks tag registration, subscription state, repeat protection, and system availability.
4. An allowed response carries the wash program and a short validity window.
5. The local command path checks operator mode, busy state, and request gates.
6. Decision, command, and acknowledgement events remain separately observable.

## Fail-closed outcomes

- unknown or unassigned tag;
- inactive or unresolved subscription;
- repeated read inside the lockout interval;
- controller busy or command already active;
- stale decision;
- local dependency unavailable;
- field path not commissioned for the selected mode.

## Database migration boundary

The hosted PostgreSQL workload moved from fixed RDS compute to private Aurora Serverless v2. Migration acceptance covered record-count parity, application read and write checks, backup creation, observed zero-ACU idle state, post-retirement resume behaviour, and retirement of the former RDS instance.

The migration records support the observed operating state only. They do not establish final long-term savings.

## Hosted and onsite separation

Customer and operator application state can run in AWS, but the reader-to-controller path remains local to the site. The local API and edge process cannot be replaced with a public cloud round trip for physical command timing.

## Publication boundary

Device IP addresses, VPN routes, access tokens, live customer data, payment identifiers, tag values, client configuration, and field runbooks are excluded. Stripe material is sandbox-only. Live payments, approved production pricing, client revenue, and physical equipment activation are not claimed.
