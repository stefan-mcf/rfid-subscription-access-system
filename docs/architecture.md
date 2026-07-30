# Architecture and failure boundaries

## Components

| Component | Responsibility |
| --- | --- |
| Customer web | Plans, account verification, checkout and billing access |
| Subscription API | Account, subscription, claim-code and webhook state |
| Access decision API | Tag lookup, entitlement checks and wash-program decision |
| PostgreSQL | Durable customer, tag, event and output-intent records |
| Edge service | Local reader polling, decision request and buffered event delivery |
| PLC bridge | Bounded command and acknowledgement interface |
| Operator dashboard | Runtime status, tag management, events and wash tests |

## Decision sequence

1. The reader reports a normalised tag identifier to the local edge service.
2. The edge service requests an access decision with a correlation ID.
3. The API checks tag registration, subscription state and repeat protection.
4. An allowed response carries the wash program and a short validity window.
5. The local command path checks operator mode, busy state and request gates.
6. Decision, command and acknowledgement events remain separately observable.

## Fail-closed outcomes

- unknown or unassigned tag;
- inactive or unresolved subscription;
- repeated read inside the lockout interval;
- controller busy or command already active;
- stale decision;
- local dependency unavailable;
- field path not commissioned for the selected mode.

## Hosted and onsite separation

Customer and operator application state can run in AWS, but the reader-to-controller path is local to the site. The local API and edge process cannot be replaced with a public cloud round trip for physical command timing.

## Publication boundary

Device IP addresses, VPN routes, access tokens, live customer data, payment identifiers, tag values, client configuration and field runbooks are excluded.
