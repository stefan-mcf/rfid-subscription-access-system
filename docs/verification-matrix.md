# Verification matrix

## Status language

- **Validated:** exercised by an automated test, schema check, or direct controlled readback.
- **Demonstrated:** visible in a controlled application or sandbox surface.
- **Pending onsite:** requires the actual reader, controller, or equipment at the installation.
- **Excluded:** intentionally outside the public or commercial claim boundary.

## Matrix

| Capability | Method | Status | Boundary |
| --- | --- | --- | --- |
| JSON interfaces | Draft 2020-12 compilation plus synthetic fixture validation | Validated | Provider-neutral public schemas |
| Fail-closed access decisions | .NET behavioural tests | Validated | Synthetic reference state |
| Repeat-read lockout | .NET behavioural tests | Validated | Deterministic time fixture |
| Subscription idempotency | Duplicate event test | Validated | Reference projection |
| Subscription ordering | Older-sequence test | Validated | Reference projection |
| Full tag replacement | Remove-missing and stale-version tests | Validated | In-memory reference store |
| Outbox acknowledgement | Duplicate enqueue and repeated acknowledgement tests | Validated | In-memory reference outbox |
| Dry-run output intent | Suppression and commissioned-live tests | Validated | No equipment command |
| Customer account surfaces | Controlled application screens | Demonstrated | Personal data removed |
| Sandbox billing lifecycle | Provider sandbox screens and lifecycle records | Demonstrated | Temporary test pricing only |
| Operator access and tag controls | Controlled application screens | Demonstrated | Does not establish physical movement |
| Aurora migration | Runtime, data, backup, idle, resume, and retirement checks | Validated | Observation at acceptance time |
| Reader and scanner input | Onsite read test | Pending onsite | Physical hardware required |
| Beam and busy signals | Controller input test | Pending onsite | Physical I/O required |
| PLC/CODESYS acknowledgement | End-to-end command test | Pending onsite | Field network required |
| Wash actuation | Observed onsite cycle | Pending onsite | Physical acceptance required |
| Live customer charging | Production payment record | Excluded | Sandbox only |
| Approved production pricing | Client commercial approval | Excluded | Not claimed |
| Client revenue or savings | Client-owned business records | Excluded | Not claimed |

## Repository validation

`npm run validate` checks:

- required repository structure;
- JSON parsing and schema compilation;
- every synthetic example against its declared schema;
- cross-record uniqueness and ordering invariants;
- PNG file type, minimum dimensions, and duplicate content;
- local Markdown links and asset references; and
- common secret, private-key, local-path, private-network, and provider-identifier patterns.

`npm run validate:dotnet` builds and tests the .NET 10 reference model with warnings treated as errors.
