# System case study

## Context

A subscription car-wash access system crosses commercial, identity, cloud, local-network, and physical-control boundaries. A single “active” flag cannot safely describe all of them.

The implementation needed to connect:

- a verified customer account;
- one or more vehicles;
- a plan and current subscription state per vehicle;
- an RFID tag and assignment state;
- a reason-coded access decision;
- a local reader and controller path; and
- an operator surface that exposes state without bypassing controls.

## Operating objective

Create one traceable path from customer subscription to RFID eligibility while retaining four independent gates:

1. identity and record ownership;
2. reconciled commercial state;
3. short-lived access eligibility; and
4. local field safety and acknowledgement.

## What was implemented

### Customer and billing

- email-first account entry and verified identity;
- customer-owned vehicle and plan selection;
- sandbox Checkout session creation;
- server-side Billing Portal ownership resolution;
- explicit plan, payment, tag, and vehicle states; and
- signed, ordered, duplicate-resistant lifecycle processing.

### Access and local runtime

- normalised RFID reads with correlation and sequence IDs;
- a full, versioned local tag-entitlement snapshot;
- stable allow-or-deny reasons;
- repeat-read and busy-state gates;
- durable local outbox delivery;
- dry-run output intents; and
- distinct command requested, acknowledged, and rejected events.

### Operator and cloud

- customer, subscription, tag, and decision context in one operator surface;
- controlled tag assignment and activation;
- off, manual, and automatic mode visibility;
- hosted PostgreSQL state on private Aurora Serverless v2; and
- explicit hosted-versus-onsite health boundaries.

## Key design decisions

### Current state is a projection, not the provider event

The provider event is stored and deduplicated first. Current subscription state changes only through an ordered projection. This prevents duplicate delivery from applying twice and blocks older events from reversing a newer state.

### Full snapshots replace local entitlement state

Incremental local changes can leave removed tags behind when a message is missed. A versioned full snapshot replaces the prior set atomically, so absence in the accepted snapshot means absence locally.

### Output intent is not actuation

An allowed access decision can prepare a bounded output intent. The intent still carries mode, commissioning, expiry, and local gate state. Only a separate acknowledgement can establish that the controller accepted a request; neither record alone establishes physical movement.

### The outbox owns delivery state

Local events are persisted before upstream delivery. Stable message IDs make enqueue and acknowledgement repeat-safe, while pending records remain visible during a network interruption.

## Failure behaviour

| Condition | Result |
| --- | --- |
| Missing local snapshot | Deny: field path unavailable |
| Unknown tag | Deny: unknown tag |
| Inactive or past-due subscription | Deny: inactive subscription |
| Repeat read inside lockout | Deny: repeat lockout |
| Busy controller | Deny: system busy |
| Expired allow decision | Suppress output intent |
| Dry-run mode | Record intent; do not permit actuation |
| Uncommissioned field path | Record suppressed intent |
| Duplicate provider event | Acknowledge receipt; do not apply twice |
| Older provider sequence | Preserve current state |
| Repeated outbox acknowledgement | No state regression |

## Verification approach

The public reference model makes the highest-risk state controls executable. JSON fixtures are synthetic and schema-validated. Repository checks also cover assets, local links, and accidental inclusion of common secret or private-data forms.

Platform screens and runtime readbacks establish the hosted and sandbox surfaces. They do not substitute for onsite physical acceptance.

[Verification matrix](verification-matrix.md)

## Current boundary

The hosted application, durable data, sandbox billing lifecycle, operator controls, edge design, and reference state controls are complete enough to demonstrate the integrated system. Final reader placement, scanner reads, beam state, controller acknowledgement, and wash actuation require onsite acceptance.

[Publication boundary](publication-boundary.md)
