# Customer account security

## Ownership model

The customer-facing brand and the reusable operator system are separate surfaces. A customer sees their wash account, vehicles, plans, tag status, and billing actions. An operator sees operational state and controls. Neither surface treats a URL parameter as ownership.

## Identity sequence

1. A customer starts with an email-first account flow.
2. The backend issues a single-purpose verification path.
3. Successful verification establishes a server-side session.
4. The browser receives an `HttpOnly` session cookie with a strict same-site boundary.
5. Account and vehicle queries resolve the customer from the verified session.
6. Billing Portal creation resolves the provider customer from the owned account server-side.

## Controls

| Threat | Control |
| --- | --- |
| Account enumeration | Uniform account-start response |
| Link replay | Single-purpose, expiring verification token |
| Session theft through client script | `HttpOnly` cookie |
| Cross-site session use | Strict same-site cookie boundary |
| Insecure production session | Secure cookie under TLS |
| Vehicle record substitution | Server-side account and vehicle ownership check |
| Billing customer substitution | Provider customer resolved from the owned account, not browser input |
| Direct object reference | Resource lookup is scoped by the verified account |

## State shown to the customer

Plan, payment, vehicle, and tag state remain distinct:

- a plan can be selected before checkout completes;
- a subscription can be active before a physical tag is assigned;
- a tag can await activation while billing remains current; and
- a payment issue can block access without deleting the vehicle record.

This separation gives the customer a useful next action and gives the operator a precise reason code.

## Public boundary

The screenshots and fixtures omit customer names, email addresses, phone numbers, vehicle registrations, provider customer IDs, session values, and verification tokens. The operational authentication implementation remains private.
