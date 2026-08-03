# Publication boundary

## Included

- system architecture and responsibility boundaries;
- selected screens with personal and client data removed;
- provider-neutral JSON Schemas;
- synthetic examples with deterministic identifiers;
- a small .NET 10 reference model;
- behavioural tests for state and safety controls;
- migration and verification notes; and
- explicit hosted-versus-onsite acceptance status.

## Excluded

- customer names, email addresses, phone numbers, and vehicle registrations;
- live RFID tag values and claim codes;
- payment customer, checkout, subscription, invoice, and event identifiers;
- account numbers, cloud resource names, endpoints, and network topology identifiers;
- device addresses, VPN routes, controller symbols, and program mappings;
- credentials, tokens, cookies, signatures, connection strings, and environment files;
- client configuration and field runbooks;
- operational source code; and
- screenshots that expose browser chrome, private account navigation, or unrelated provider controls.

## Commercial boundary

The billing material is sandbox-only and uses temporary test pricing. The repository does not claim live payments, approved production pricing, client revenue, final long-term savings, or production transaction volume.

## Physical boundary

Hosted availability, a current subscription, an allowed access decision, and a prepared output intent do not establish that equipment moved. Physical scanner reads, input signals, PLC/CODESYS acknowledgement, and wash actuation require onsite acceptance.

## Synthetic data rule

All public JSON example files declare `"fixture": "synthetic"`. Identifiers use deterministic `demo-` references or reserved hexadecimal tag examples. They are not copied from customer, payment, cloud, device, or field systems.
