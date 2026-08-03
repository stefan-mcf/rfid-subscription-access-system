# Field safety and commissioning

## Separate acceptance layers

| Layer | Acceptance question |
| --- | --- |
| Commercial | Is the customer and per-vehicle subscription state current? |
| Eligibility | Did the access API return a current allow decision? |
| Local intent | Did local mode, busy state, lockout, and expiry gates permit an intent? |
| Controller | Did the PLC/CODESYS bridge acknowledge the bounded request? |
| Physical | Did onsite observation confirm the expected equipment response? |

Passing one layer does not establish the next.

## Output modes

- **Off:** output intents are not permitted.
- **Dry run:** the same decision and planning path runs, but the intent is suppressed and actuation permission remains false.
- **Manual:** an authorised operator can request a bounded test subject to the same local safety gates.
- **Automatic:** an eligible tag can prepare an intent only after field commissioning and runtime checks pass.

## Commissioning gates

- reader identity and expected tag format;
- reader placement and read range;
- beam or vehicle-presence signal where applicable;
- controller connection and expected program mapping;
- busy and active-cycle state;
- command pulse bounds and acknowledgement timeout;
- repeat-read lockout;
- loss-of-connection behaviour;
- operator emergency and off-mode controls; and
- observed physical response recorded onsite.

## Fail-closed rule

Missing snapshot, stale decision, unknown state, unavailable controller, expired intent, or uncommissioned mode suppresses the output path. A dashboard click or cloud response is never presented as confirmation that machinery moved.

## Public acceptance status

The hosted software, event records, operator modes, reference decision controls, and dry-run output-intent path are represented here. Physical scanner reads, beam state, PLC/CODESYS acknowledgement, and wash actuation remain onsite acceptance items.
