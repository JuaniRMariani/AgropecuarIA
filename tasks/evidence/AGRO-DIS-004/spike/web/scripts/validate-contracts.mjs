import { readFileSync } from "node:fs";
import { resolve } from "node:path";

import Ajv2020 from "ajv/dist/2020.js";
import addFormats from "ajv-formats";

const evidenceRoot = resolve(process.cwd(), "../..");
const ajv = new Ajv2020({ allErrors: true, strict: true });
addFormats(ajv);

function readJson(path) {
  return JSON.parse(readFileSync(path, "utf8").replace(/^\uFEFF/, ""));
}

const validations = [
  [
    "spatial-reference.schema.json",
    "fixtures/contracts/spatial-reference.valid.json",
  ],
  [
    "weather-snapshot.schema.json",
    "fixtures/contracts/weather-snapshot.valid.json",
  ],
  ["cap-lifecycle.schema.json", "fixtures/contracts/cap-lifecycle.valid.json"],
];

for (const [schemaName, instancePath] of validations) {
  const schema = readJson(resolve(evidenceRoot, "contracts", schemaName));
  const instance = readJson(resolve(evidenceRoot, instancePath));
  const validate = ajv.compile(schema);
  if (!validate(instance)) {
    throw new Error(
      `${instancePath} does not satisfy ${schemaName}: ${ajv.errorsText(validate.errors)}`,
    );
  }
}

const providerSchema = readJson(
  resolve(evidenceRoot, "contracts/provider-run.schema.json"),
);
const providerEvidence = readJson(
  resolve(evidenceRoot, "results/provider-probes.json"),
);
const validateProvider = ajv.compile(providerSchema);
for (const run of providerEvidence.providerRuns) {
  if (!validateProvider(run)) {
    throw new Error(
      `Provider ${run.provider} does not satisfy provider-run.schema.json: ${ajv.errorsText(validateProvider.errors)}`,
    );
  }
}

console.log(
  "Contract validation passed: 3 canonical examples and 5 provider runs.",
);
