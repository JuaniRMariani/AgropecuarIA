import { describe, expect, it } from "vitest";
import {
  MAX_FILE_BYTES,
  shortId,
  workflowReducer,
  type FileDescriptor,
  type WorkflowState,
} from "../../features/file-workflow/workflow-model";

const validFile: FileDescriptor = {
  name: "informe-lote.pdf",
  mediaType: "application/pdf",
  sizeBytes: 2048,
};

function reachScanning(): WorkflowState {
  const ready = workflowReducer(
    { kind: "idle" },
    { type: "select", file: validFile },
  );
  const uploading = workflowReducer(ready, { type: "start_upload" });
  const halfway = workflowReducer(uploading, { type: "advance_upload" });
  return workflowReducer(halfway, { type: "advance_upload" });
}

describe("workflowReducer", () => {
  it("rechaza MIME declarado no permitido y archivos que superan el límite", () => {
    expect(
      workflowReducer(
        { kind: "idle" },
        { type: "select", file: { ...validFile, mediaType: "text/html" } },
      ),
    ).toMatchObject({ kind: "validation_error" });

    expect(
      workflowReducer(
        { kind: "idle" },
        {
          type: "select",
          file: { ...validFile, sizeBytes: MAX_FILE_BYTES + 1 },
        },
      ),
    ).toMatchObject({ kind: "validation_error" });
  });

  it("nunca hace disponible un archivo antes de completar carga y análisis", () => {
    const scanning = reachScanning();
    expect(scanning.kind).toBe("scanning");

    const available = workflowReducer(scanning, {
      type: "finish_scan",
      scenario: "clean",
    });
    expect(available).toMatchObject({
      kind: "available",
      grant: "active",
      downloaded: false,
    });
  });

  it.each(["quarantine", "scan_unavailable"] as const)(
    "mantiene el contenido aislado para el escenario %s",
    (scenario) => {
      const result = workflowReducer(reachScanning(), {
        type: "finish_scan",
        scenario,
      });
      expect(result.kind).toBe(
        scenario === "quarantine" ? "quarantined" : scenario,
      );
      expect(result.kind).not.toBe("available");
    },
  );

  it("modela vencimiento y renovación del permiso de descarga", () => {
    const available = workflowReducer(reachScanning(), {
      type: "finish_scan",
      scenario: "clean",
    });
    const expired = workflowReducer(available, { type: "expire_grant" });
    expect(expired).toMatchObject({ kind: "available", grant: "expired" });
    expect(workflowReducer(expired, { type: "renew_grant" })).toMatchObject({
      kind: "available",
      grant: "active",
    });
  });

  it("solo registra la descarga sintética con un permiso activo", () => {
    const available = workflowReducer(reachScanning(), {
      type: "finish_scan",
      scenario: "clean",
    });
    expect(
      workflowReducer(available, { type: "request_download" }),
    ).toMatchObject({
      kind: "available",
      downloaded: true,
    });

    const expired = workflowReducer(available, { type: "expire_grant" });
    expect(workflowReducer(expired, { type: "request_download" })).toEqual(
      expired,
    );
  });

  it("vuelve a un estado seguro al reintentar fallos y conflictos", () => {
    for (const scenario of [
      "provider_unavailable",
      "conflict",
      "upload_error",
    ] as const) {
      const failure = workflowReducer(reachScanning(), {
        type: "finish_scan",
        scenario,
      });
      expect(workflowReducer(failure, { type: "retry" }).kind).toBe("ready");
    }
  });
});

describe("shortId", () => {
  it("muestra solamente los primeros seis caracteres sin guiones y en mayúsculas", () => {
    expect(shortId("19f64ec7-21ca-4762-bac8-02d233cf57e2")).toBe("19F64E");
  });
});
