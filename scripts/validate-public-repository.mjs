import assert from "node:assert/strict";
import { createHash } from "node:crypto";
import { readdir, readFile, stat } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import Ajv2020 from "ajv/dist/2020.js";
import addFormats from "ajv-formats";

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const repositoryRoot = path.resolve(scriptDirectory, "..");

const requiredFiles = [
  "README.md",
  "LICENSE",
  "package.json",
  "package-lock.json",
  "global.json",
  "Rfid.SubscriptionAccess.Reference.slnx",
  "assets/manifest.json",
  "verification/claim-matrix.json",
  ".github/workflows/validate.yml",
  "docs/case-study.md",
  "docs/architecture.md",
  "docs/customer-account-security.md",
  "docs/subscription-reconciliation.md",
  "docs/local-edge-and-outbox.md",
  "docs/field-safety-and-commissioning.md",
  "docs/aurora-migration.md",
  "docs/verification-matrix.md",
  "docs/publication-boundary.md",
  "src/Rfid.ReferenceModel/Rfid.ReferenceModel.csproj",
  "src/Rfid.ReferenceModel/AccessDecisionEngine.cs",
  "src/Rfid.ReferenceModel/SubscriptionProjection.cs",
  "src/Rfid.ReferenceModel/TagSnapshotStore.cs",
  "src/Rfid.ReferenceModel/LocalOutbox.cs",
  "src/Rfid.ReferenceModel/OutputIntentPlanner.cs",
  "src/Rfid.ReferenceModel/Program.cs",
  "tests/Rfid.ReferenceModel.Tests/Rfid.ReferenceModel.Tests.csproj",
  "tests/Rfid.ReferenceModel.Tests/AccessDecisionEngineTests.cs",
  "tests/Rfid.ReferenceModel.Tests/StateControlTests.cs",
  "tests/Rfid.ReferenceModel.Tests/OutputIntentPlannerTests.cs"
];

const requiredSchemas = [
  "contracts/access-decision.json",
  "contracts/wash-event.json",
  "contracts/rfid-read.schema.json",
  "contracts/subscription-state-change.schema.json",
  "contracts/tag-snapshot.schema.json",
  "contracts/local-outbox.schema.json",
  "contracts/output-intent.schema.json"
];

const requiredAssets = [
  "assets/01-subscription-to-access-decision.png",
  "assets/02-vehicle-plan-selection.png",
  "assets/03-vehicle-details-before-checkout.png",
  "assets/04-customer-account-and-access-state.png",
  "assets/05-stripe-sandbox-billing-state.png",
  "assets/06-operator-and-edge-access-decisions.png",
  "assets/07-tag-assignment-and-claim-lookup.png",
  "assets/08-wash-controls-outputs-disabled.png",
  "assets/09-aurora-serverless-database-migration.png"
];

const readUtf8 = (relativePath) =>
  readFile(path.join(repositoryRoot, relativePath), "utf8");

const readJson = async (relativePath) => JSON.parse(await readUtf8(relativePath));

const ignoredDirectoryNames = new Set([".git", "node_modules", "bin", "obj", "TestResults"]);

const listFilesRecursively = async (relativeDirectory) => {
  const absoluteDirectory = path.join(repositoryRoot, relativeDirectory);
  const entries = await readdir(absoluteDirectory, { withFileTypes: true });
  const nested = await Promise.all(entries.map(async (entry) => {
    const relativePath = path.join(relativeDirectory, entry.name);
    if (entry.isDirectory() && ignoredDirectoryNames.has(entry.name)) {
      return [];
    }
    return entry.isDirectory() ? listFilesRecursively(relativePath) : [relativePath];
  }));
  return nested.flat().sort();
};

const exists = async (absolutePath) => {
  try {
    await stat(absolutePath);
    return true;
  } catch (error) {
    if (error.code === "ENOENT") {
      return false;
    }
    throw error;
  }
};

for (const relativePath of [...requiredFiles, ...requiredSchemas, ...requiredAssets]) {
  assert(await exists(path.join(repositoryRoot, relativePath)), `Missing required file: ${relativePath}`);
}

const ajv = new Ajv2020({ allErrors: true, strict: true });
addFormats(ajv);

const contractFiles = (await listFilesRecursively("contracts")).filter((file) => file.endsWith(".json"));
for (const requiredSchema of requiredSchemas) {
  assert(contractFiles.includes(requiredSchema), `Missing required schema: ${requiredSchema}`);
}

const schemaValidators = new Map();
for (const relativePath of contractFiles) {
  const schema = await readJson(relativePath);
  assert.equal(schema.$schema, "https://json-schema.org/draft/2020-12/schema", `${relativePath} must use JSON Schema 2020-12`);
  assert(schema.$id?.startsWith("https://schemas.smsystems.au/rfid/"), `${relativePath} must have a public schema ID`);
  schemaValidators.set(relativePath, ajv.compile(schema));
}

const exampleFiles = (await listFilesRecursively("examples")).filter((file) => file.endsWith(".json"));
assert(exampleFiles.length >= 7, "Expected at least seven synthetic example files");

const identifierFields = ["event_id", "read_id", "snapshot_id", "message_id", "intent_id", "correlation_id"];

for (const relativePath of exampleFiles) {
  const fixture = await readJson(relativePath);
  assert.equal(fixture.fixture, "synthetic", `${relativePath} must declare fixture=synthetic`);
  assert.equal(fixture.schema_version, "1.0.0", `${relativePath} must declare schema_version=1.0.0`);
  assert(schemaValidators.has(fixture.schema), `${relativePath} declares an unknown schema: ${fixture.schema}`);
  assert(Array.isArray(fixture.cases) && fixture.cases.length > 0, `${relativePath} must contain at least one case`);

  const validator = schemaValidators.get(fixture.schema);
  for (const [index, item] of fixture.cases.entries()) {
    const valid = validator(item);
    assert(valid, `${relativePath} case ${index + 1} failed schema validation:\n${ajv.errorsText(validator.errors, { separator: "\n" })}`);
  }

  const identityField = identifierFields.find((field) => fixture.cases.every((item) => Object.hasOwn(item, field)));
  assert(identityField, `${relativePath} cases need a stable identifier`);
  const identifiers = fixture.cases.map((item) => item[identityField]);
  assert.equal(new Set(identifiers).size, identifiers.length, `${relativePath} contains duplicate ${identityField} values`);
}

const subscriptionChanges = await readJson("examples/subscription-state-changes.json");
for (const item of subscriptionChanges.cases) {
  assert(new Date(item.recorded_at) >= new Date(item.effective_at), `${item.event_id} was recorded before it became effective`);
}
const subscriptionSequences = subscriptionChanges.cases.map((item) => item.event_sequence);
assert.deepEqual(subscriptionSequences, [...subscriptionSequences].sort((left, right) => left - right), "Subscription events must be ordered in the fixture");

const rfidReads = await readJson("examples/rfid-reads.json");
const readSequences = rfidReads.cases.map((item) => item.monotonic_sequence);
assert.equal(new Set(readSequences).size, readSequences.length, "RFID read sequences must be unique");
assert.deepEqual(readSequences, [...readSequences].sort((left, right) => left - right), "RFID reads must be ordered in the fixture");

const snapshots = await readJson("examples/tag-snapshots.json");
for (const snapshot of snapshots.cases) {
  const tagIds = snapshot.tags.map((tag) => tag.tag_id);
  assert.equal(new Set(tagIds).size, tagIds.length, `${snapshot.snapshot_id} contains duplicate tag IDs`);
}

const washEvents = await readJson("examples/wash-events.json");
const eventsByCorrelation = Map.groupBy(washEvents.cases, (item) => item.correlation_id);
for (const [correlationId, events] of eventsByCorrelation) {
  const sequences = events.map((item) => item.sequence);
  assert.deepEqual(sequences, [...sequences].sort((left, right) => left - right), `${correlationId} has out-of-order event sequences`);
}

const outputIntents = await readJson("examples/output-intents.json");
for (const intent of outputIntents.cases) {
  assert(new Date(intent.expires_at) >= new Date(intent.requested_at), `${intent.intent_id} expires before it was requested`);
}

const claimMatrix = await readJson("verification/claim-matrix.json");
assert.equal(claimMatrix.schema_version, "1.0.0", "Claim matrix must declare schema_version=1.0.0");
assert.deepEqual(
  claimMatrix.statuses,
  ["validated", "demonstrated", "pending_onsite", "excluded"],
  "Claim matrix status vocabulary changed"
);
assert(Array.isArray(claimMatrix.claims) && claimMatrix.claims.length >= 10, "Claim matrix needs at least ten bounded claims");
assert.equal(new Set(claimMatrix.claims.map((claim) => claim.id)).size, claimMatrix.claims.length, "Claim IDs must be unique");
for (const claim of claimMatrix.claims) {
  assert(/^RFID-\d{3}$/.test(claim.id), `Invalid claim ID: ${claim.id}`);
  assert(claimMatrix.statuses.includes(claim.status), `${claim.id} has an invalid status`);
  assert(typeof claim.statement === "string" && claim.statement.endsWith("."), `${claim.id} needs a complete statement`);
  assert(Array.isArray(claim.basis) && claim.basis.length > 0, `${claim.id} needs at least one basis path`);
  for (const relativePath of claim.basis) {
    assert(await exists(path.join(repositoryRoot, relativePath)), `${claim.id} references a missing basis path: ${relativePath}`);
  }
}

const markdownFiles = ["README.md", ...(await listFilesRecursively("docs")).filter((file) => file.endsWith(".md"))];
const markdownContent = new Map(await Promise.all(markdownFiles.map(async (relativePath) => [relativePath, await readUtf8(relativePath)])));

const markdownLinkPattern = /!?(?:\[[^\]]*\])\(([^)]+)\)/g;
for (const [relativePath, content] of markdownContent) {
  for (const match of content.matchAll(markdownLinkPattern)) {
    const rawTarget = match[1].trim().replace(/^<|>$/g, "");
    const target = rawTarget.split("#", 1)[0];
    if (!target || target.startsWith("#") || target.startsWith("mailto:")) {
      continue;
    }
    if (/^https?:\/\//.test(target)) {
      assert(target.startsWith("https://"), `${relativePath} contains a non-HTTPS external link: ${target}`);
      continue;
    }

    const decodedTarget = decodeURIComponent(target);
    const absoluteTarget = path.resolve(path.dirname(path.join(repositoryRoot, relativePath)), decodedTarget);
    assert(absoluteTarget.startsWith(`${repositoryRoot}${path.sep}`), `${relativePath} links outside the repository: ${target}`);
    assert(await exists(absoluteTarget), `${relativePath} contains a broken local link: ${target}`);
  }
}

const assetFiles = (await listFilesRecursively("assets")).filter((file) => file.endsWith(".png"));
assert(assetFiles.length >= requiredAssets.length, `Expected at least ${requiredAssets.length} public system frames`);
const assetHashes = new Set();
const allMarkdown = [...markdownContent.values()].join("\n");
const assetManifest = await readJson("assets/manifest.json");
assert.equal(assetManifest.schema_version, "1.0.0", "Asset manifest must declare schema_version=1.0.0");
assert.equal(assetManifest.generated_from, "approved-public-frames", "Asset manifest source boundary changed");
assert.equal(assetManifest.assets.length, assetFiles.length, "Asset manifest count does not match PNG count");
assert.deepEqual(
  assetManifest.assets.map((item) => item.sequence),
  Array.from({ length: assetFiles.length }, (_, index) => index + 1),
  "Asset manifest sequence is not contiguous"
);
const manifestByPath = new Map(assetManifest.assets.map((item) => [`assets/${item.file}`, item]));

for (const relativePath of assetFiles) {
  const image = await readFile(path.join(repositoryRoot, relativePath));
  assert(image.subarray(0, 8).equals(Buffer.from([137, 80, 78, 71, 13, 10, 26, 10])), `${relativePath} is not a PNG`);
  assert.equal(image.subarray(12, 16).toString("ascii"), "IHDR", `${relativePath} has no PNG IHDR chunk`);
  const width = image.readUInt32BE(16);
  const height = image.readUInt32BE(20);
  assert(width >= 1200 && height >= 900, `${relativePath} is too small (${width}x${height})`);

  const hash = createHash("sha256").update(image).digest("hex");
  const manifestEntry = manifestByPath.get(relativePath.replaceAll(path.sep, "/"));
  assert(manifestEntry, `${relativePath} is missing from the asset manifest`);
  assert.equal(manifestEntry.width, width, `${relativePath} width differs from the asset manifest`);
  assert.equal(manifestEntry.height, height, `${relativePath} height differs from the asset manifest`);
  assert.equal(manifestEntry.sha256, hash, `${relativePath} SHA-256 differs from the asset manifest`);
  assert(!assetHashes.has(hash), `${relativePath} duplicates another asset`);
  assetHashes.add(hash);
  assert(allMarkdown.includes(relativePath.replaceAll(path.sep, "/")), `${relativePath} is not referenced by README or docs`);
}

const textExtensions = new Set([".md", ".json", ".mjs", ".cs", ".csproj", ".yml", ".yaml"]);
const textFiles = (await listFilesRecursively("."))
  .filter((relativePath) => textExtensions.has(path.extname(relativePath)) || ["README.md", "package.json"].includes(relativePath));

const rejectedPatterns = [
  { label: "AWS access key", pattern: /\b(?:AKIA|ASIA)[A-Z0-9]{16}\b/ },
  { label: "private key", pattern: /-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----/ },
  { label: "provider secret", pattern: /\b(?:sk_(?:live|test)|whsec|ghp)_[A-Za-z0-9_-]{8,}\b/ },
  { label: "AWS ARN", pattern: /\barn:aws(?:-[a-z]+)?:/ },
  { label: "Stripe object ID", pattern: /\b(?:cus|sub|in|evt|cs_(?:test|live))_[A-Za-z0-9]{8,}\b/ },
  { label: "local absolute path", pattern: /\/(?:Users|home)\/[^\s)"']+/ },
  { label: "private IPv4 address", pattern: /\b(?:10\.(?:\d{1,3}\.){2}\d{1,3}|192\.168\.(?:\d{1,3}\.)\d{1,3}|172\.(?:1[6-9]|2\d|3[01])\.(?:\d{1,3}\.)\d{1,3})\b/ },
  { label: "email address", pattern: /\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b/i }
];

for (const relativePath of textFiles) {
  const content = await readUtf8(relativePath);
  for (const { label, pattern } of rejectedPatterns) {
    assert(!pattern.test(content), `${relativePath} contains a rejected ${label}`);
  }
}

const disallowedPortfolioWord = new RegExp(`\\b${"pr" + "oof"}\\b`, "i");

for (const relativePath of markdownFiles) {
  const content = markdownContent.get(relativePath);
  assert(!/\bTODO\b|\bTBD\b/.test(content), `${relativePath} contains unfinished copy`);
  assert(!disallowedPortfolioWord.test(content), `${relativePath} contains disallowed portfolio wording`);
}

const sourceProject = await readUtf8("src/Rfid.ReferenceModel/Rfid.ReferenceModel.csproj");
const testProject = await readUtf8("tests/Rfid.ReferenceModel.Tests/Rfid.ReferenceModel.Tests.csproj");
assert(sourceProject.includes("<TargetFramework>net10.0</TargetFramework>"), "Reference model must target .NET 10");
assert(testProject.includes("<TargetFramework>net10.0</TargetFramework>"), "Reference tests must target .NET 10");
assert(testProject.includes("../../src/Rfid.ReferenceModel/Rfid.ReferenceModel.csproj"), "Tests must reference the executable model");

console.log(`Validated ${contractFiles.length} schemas, ${exampleFiles.length} fixture files, ${assetFiles.length} assets, and ${markdownFiles.length} Markdown files.`);
