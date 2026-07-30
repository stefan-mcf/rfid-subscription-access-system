import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const readJson = async (path) => JSON.parse(await readFile(path, "utf8"));

const accessSchema = await readJson(new URL("../contracts/access-decision.json", import.meta.url));
const eventSchema = await readJson(new URL("../contracts/wash-event.json", import.meta.url));
const examples = await readJson(new URL("../examples/decisions.json", import.meta.url));

const requiredAccessFields = new Set(accessSchema.required);
for (const item of examples.cases) {
  for (const field of requiredAccessFields) {
    assert(Object.hasOwn(item, field), `Missing ${field} in ${item.correlation_id}`);
  }
  assert(accessSchema.properties.decision.enum.includes(item.decision));
  assert(accessSchema.properties.reason.enum.includes(item.reason));
  assert(/^[A-F0-9]{8,32}$/.test(item.tag_id));
  if (item.decision === "allow") {
    assert.equal(item.reason, "active_subscription");
    assert(Number.isInteger(item.wash_program));
    assert(item.valid_until);
  } else {
    assert.equal(item.wash_program, null);
    assert.equal(item.valid_until, null);
  }
}

assert.equal(new Set(examples.cases.map((item) => item.correlation_id)).size, examples.cases.length);
assert(eventSchema.properties.event_type.enum.includes("command_acknowledged"));
assert(eventSchema.properties.event_type.enum.includes("command_rejected"));

console.log("RFID access contracts are valid.");
