# Subscription reconciliation

## Why reconciliation is required

Checkout completion, invoice state, subscription state, and local access eligibility can change at different times. Redirecting a browser after checkout is not a durable commercial record, and webhook delivery can be repeated or reordered.

The lifecycle path therefore treats provider events as inputs to an ordered projection rather than direct overwrites of customer access state.

## Processing sequence

1. Receive the provider event body and signature metadata.
2. Verify the signature before parsing it into the trusted transition path.
3. Record the provider event reference with a uniqueness constraint.
4. Place valid events on the ordered processing path.
5. Compare sequence and current projection state.
6. Apply a forward transition once.
7. Store the event receipt and reconciled current state in one durable boundary.
8. Refresh customer, operator, and tag-entitlement views from that state.

## State model

The public schema uses these provider-neutral states:

| State | Meaning for the application |
| --- | --- |
| `pending_checkout` | Plan selected; no current paid entitlement |
| `active` | Commercial state can participate in access eligibility |
| `past_due` | Access is fail-closed pending commercial resolution |
| `paused` | Access is fail-closed by an explicit lifecycle state |
| `cancelled` | No current commercial entitlement |
| `incomplete` | Setup did not reach an eligible current state |

The access API does not infer eligibility from a checkout redirect or an invoice screenshot. It reads the reconciled current state.

## Idempotency and ordering

- provider event reference prevents the same event from applying twice;
- sequence protects current state from older delivery;
- current state and event receipt are stored separately;
- repeated delivery can be acknowledged safely; and
- unresolved or invalid input fails closed.

The executable [`SubscriptionProjection`](../src/Rfid.ReferenceModel/SubscriptionProjection.cs) demonstrates duplicate and out-of-order handling with synthetic data.

## Sandbox boundary

The amount shown in the published billing screen is a temporary test price used only in provider sandbox mode. It is not commercial pricing. The screen establishes checkout, subscription, invoice, lifecycle-event, and Billing Portal behaviour only. It does not establish live payments, production pricing approval, revenue, or client savings.
