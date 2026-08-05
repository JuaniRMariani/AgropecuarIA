import { readFile } from "node:fs/promises";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import Ajv2020 from "ajv/dist/2020.js";
import addFormats from "ajv-formats";

const here = dirname(fileURLToPath(import.meta.url));
const evidenceRoot = join(here, "..", "..", "..");
const pairs = [
  ["file-object.schema.json", "file-object.available.json"],
  ["upload-intent.schema.json", "upload-intent.json"],
  ["scan-result.schema.json", "scan-result.clean.json"],
  ["download-grant.schema.json", "download-grant.json"],
  ["backup-manifest.schema.json", "backup-manifest.json"],
];

const ajv = new Ajv2020({ allErrors: true, strict: true });
addFormats(ajv);

for (const [schemaName, fixtureName] of pairs) {
  const schema = JSON.parse(
    await readFile(join(evidenceRoot, "contracts", schemaName), "utf8"),
  );
  const fixture = JSON.parse(
    await readFile(
      join(evidenceRoot, "fixtures", "contracts", fixtureName),
      "utf8",
    ),
  );
  const validate = ajv.compile(schema);
  if (!validate(fixture)) {
    throw new Error(`${fixtureName}: ${ajv.errorsText(validate.errors)}`);
  }
}

process.stdout.write(`PASS: ${pairs.length} contract fixtures validated.\n`);
