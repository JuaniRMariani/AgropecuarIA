import { readFile } from "node:fs/promises";
import path from "node:path";
import Ajv2020 from "ajv/dist/2020.js";
import addFormats from "ajv-formats";

const root = path.resolve(process.cwd(), "../..");
const contracts = [
  {
    schemaPath: "contracts/capacity-scenarios.schema.json",
    fixturePath: "fixtures/capacity-scenarios.json",
    negativeCases: [
      (value) => {
        value.classification = "observed";
      },
      (value) => {
        value.scenarios[1].id = "pilot";
      },
      (value) => {
        value.scenarios[0].volumes.concurrentUsers = 101;
      },
    ],
  },
  {
    schemaPath: "contracts/unit-cost-catalog.schema.json",
    fixturePath: "fixtures/unit-cost-catalog.incomplete.json",
    negativeCases: [
      (value) => {
        value.currency = "US-DOLLAR";
      },
      (value) => {
        value.status = "approved";
      },
      (value) => {
        value.status = "approved";
        for (const driver of value.drivers) {
          driver.low = 1;
          driver.base = 2;
          driver.high = 3;
        }
      },
      (value) => {
        value.status = "synthetic-test-only";
        value.region = "synthetic-test";
        value.source = "synthetic test fixture";
        for (const driver of value.drivers) {
          driver.low = 3;
          driver.base = 2;
          driver.high = 1;
        }
      },
    ],
  },
  {
    schemaPath: "contracts/capacity-report.schema.json",
    fixturePath: "fixtures/capacity-report.pilot.json",
    negativeCases: [
      (value) => {
        value.cost.missingDrivers = [];
      },
      (value) => {
        value.projection.peakRequestsPerSecond += 1;
      },
      (value) => {
        value.cost = {
          status: "estimated",
          currency: "USD",
          low: 300,
          base: 200,
          high: 100,
          missingDrivers: [],
        };
      },
    ],
  },
];
const ajv = new Ajv2020({ allErrors: true, strict: true });
addFormats(ajv);

const fixtureCache = new Map();
for (const contract of contracts) {
  const fixture = JSON.parse(
    await readFile(path.join(root, contract.fixturePath), "utf8"),
  );
  fixtureCache.set(contract.fixturePath, fixture);
}

function assertSemanticContract(fixturePath, fixture) {
  if (fixturePath === "fixtures/capacity-scenarios.json") {
    for (const scenario of fixture.scenarios) {
      if (scenario.volumes.concurrentUsers > scenario.volumes.registeredUsers) {
        throw new Error(
          `${scenario.id} has more concurrent than registered users`,
        );
      }
    }
  }

  if (fixturePath === "fixtures/unit-cost-catalog.incomplete.json") {
    for (const driver of fixture.drivers) {
      const band = [driver.low, driver.base, driver.high];
      if (
        band.every((value) => typeof value === "number") &&
        !(driver.low <= driver.base && driver.base <= driver.high)
      ) {
        throw new Error(`${driver.id} must satisfy low <= base <= high`);
      }
    }

    if (
      fixture.status === "approved" &&
      (fixture.region.startsWith("PENDING") ||
        fixture.source.startsWith("No provider selected"))
    ) {
      throw new Error("approved pricing requires a real region and source");
    }
  }

  if (fixturePath === "fixtures/capacity-report.pilot.json") {
    const scenarios = fixtureCache.get("fixtures/capacity-scenarios.json");
    const pilot = scenarios.scenarios.find(
      (scenario) => scenario.id === "pilot",
    );
    const expected = {
      averageRequestsPerSecond:
        (pilot.demand.dailyReadRequests + pilot.demand.dailyWriteRequests) /
        86_400,
      peakRequestsPerSecond:
        ((pilot.demand.dailyReadRequests + pilot.demand.dailyWriteRequests) /
          86_400) *
        pilot.demand.peakFactor,
      retainedObjectGiB:
        (pilot.volumes.documentsPerMonth *
          pilot.volumes.averageDocumentMiB *
          pilot.demand.retentionMonths *
          pilot.demand.objectVersionFactor) /
        1_024,
      dailyImportDrainSeconds:
        pilot.volumes.importRowsPerDay / pilot.demand.workerRowsPerSecond,
    };

    for (const [key, value] of Object.entries(expected)) {
      if (Math.abs(fixture.projection[key] - value) > Number.EPSILON * 16) {
        throw new Error(
          `golden pilot projection ${key} diverges from scenario fixture`,
        );
      }
    }

    if (
      fixture.cost.status === "estimated" &&
      !(
        fixture.cost.low <= fixture.cost.base &&
        fixture.cost.base <= fixture.cost.high
      )
    ) {
      throw new Error("estimated report cost must satisfy low <= base <= high");
    }
  }
}

let negativeCount = 0;
for (const contract of contracts) {
  const schema = JSON.parse(
    await readFile(path.join(root, contract.schemaPath), "utf8"),
  );
  const fixture = fixtureCache.get(contract.fixturePath);
  const validate = ajv.compile(schema);
  if (!validate(fixture)) {
    throw new Error(
      `${contract.fixturePath} does not match ${contract.schemaPath}: ${ajv.errorsText(validate.errors)}`,
    );
  }
  assertSemanticContract(contract.fixturePath, fixture);

  for (const makeInvalid of contract.negativeCases) {
    const invalidFixture = structuredClone(fixture);
    makeInvalid(invalidFixture);
    let rejected = !validate(invalidFixture);
    if (!rejected) {
      try {
        assertSemanticContract(contract.fixturePath, invalidFixture);
      } catch {
        rejected = true;
      }
    }
    if (!rejected) {
      throw new Error(
        `${contract.schemaPath} accepted a negative contract case`,
      );
    }
    negativeCount += 1;
  }
}

console.log(
  `PASS: ${contracts.length} positive and ${negativeCount} negative AGRO-DIS-007 contract cases validated`,
);
