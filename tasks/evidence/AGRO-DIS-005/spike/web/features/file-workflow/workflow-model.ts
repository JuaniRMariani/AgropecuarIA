export const MAX_FILE_BYTES = 10 * 1024 * 1024;

export const allowedMediaTypes = [
  "application/pdf",
  "image/jpeg",
  "image/png",
] as const;

export type AllowedMediaType = (typeof allowedMediaTypes)[number];

export type FileDescriptor = Readonly<{
  name: string;
  mediaType: string;
  sizeBytes: number;
}>;

type AcceptedFile = Readonly<{
  name: string;
  mediaType: AllowedMediaType;
  sizeBytes: number;
}>;

export type Scenario =
  | "clean"
  | "quarantine"
  | "scan_unavailable"
  | "provider_unavailable"
  | "conflict"
  | "upload_error"
  | "expired";

export type WorkflowState =
  | Readonly<{ kind: "idle" }>
  | Readonly<{ kind: "validation_error"; message: string }>
  | Readonly<{ kind: "ready"; file: AcceptedFile }>
  | Readonly<{ kind: "uploading"; file: AcceptedFile; progress: number }>
  | Readonly<{ kind: "scanning"; file: AcceptedFile; fileId: string }>
  | Readonly<{
      kind: "available";
      file: AcceptedFile;
      fileId: string;
      grant: "active" | "expired";
      downloaded: boolean;
    }>
  | Readonly<{ kind: "quarantined"; file: AcceptedFile; fileId: string }>
  | Readonly<{ kind: "scan_unavailable"; file: AcceptedFile; fileId: string }>
  | Readonly<{ kind: "provider_unavailable"; file: AcceptedFile }>
  | Readonly<{ kind: "conflict"; file: AcceptedFile; currentVersion: number }>
  | Readonly<{ kind: "upload_error"; file: AcceptedFile }>;

export type WorkflowAction =
  | Readonly<{ type: "select"; file: FileDescriptor }>
  | Readonly<{ type: "start_upload" }>
  | Readonly<{ type: "advance_upload" }>
  | Readonly<{ type: "finish_scan"; scenario: Scenario }>
  | Readonly<{ type: "expire_grant" }>
  | Readonly<{ type: "renew_grant" }>
  | Readonly<{ type: "request_download" }>
  | Readonly<{ type: "retry" }>
  | Readonly<{ type: "reset" }>;

const syntheticFileId = "19f64ec7-21ca-4762-bac8-02d233cf57e2";

export function shortId(value: string): string {
  return value.replaceAll("-", "").slice(0, 6).toUpperCase();
}

export function isScenario(value: string): value is Scenario {
  return [
    "clean",
    "quarantine",
    "scan_unavailable",
    "provider_unavailable",
    "conflict",
    "upload_error",
    "expired",
  ].includes(value);
}

function isAllowedMediaType(value: string): value is AllowedMediaType {
  return allowedMediaTypes.some((mediaType) => mediaType === value);
}

function validate(file: FileDescriptor): WorkflowState {
  if (file.sizeBytes < 1) {
    return { kind: "validation_error", message: "El archivo está vacío." };
  }

  if (file.sizeBytes > MAX_FILE_BYTES) {
    return {
      kind: "validation_error",
      message: "El archivo supera el límite del prototipo de 10 MB.",
    };
  }

  if (!isAllowedMediaType(file.mediaType)) {
    return {
      kind: "validation_error",
      message: "El tipo declarado no está permitido. Usá PDF, JPG o PNG.",
    };
  }

  return {
    kind: "ready",
    file: {
      name: file.name,
      mediaType: file.mediaType,
      sizeBytes: file.sizeBytes,
    },
  };
}

function finishScan(
  state: Extract<WorkflowState, { kind: "scanning" }>,
  scenario: Scenario,
): WorkflowState {
  switch (scenario) {
    case "clean":
      return {
        ...state,
        kind: "available",
        grant: "active",
        downloaded: false,
      };
    case "expired":
      return {
        ...state,
        kind: "available",
        grant: "expired",
        downloaded: false,
      };
    case "quarantine":
      return { ...state, kind: "quarantined" };
    case "scan_unavailable":
      return { ...state, kind: "scan_unavailable" };
    case "provider_unavailable":
      return { kind: "provider_unavailable", file: state.file };
    case "conflict":
      return { kind: "conflict", file: state.file, currentVersion: 2 };
    case "upload_error":
      return { kind: "upload_error", file: state.file };
    default: {
      const exhaustive: never = scenario;
      return exhaustive;
    }
  }
}

export function workflowReducer(
  state: WorkflowState,
  action: WorkflowAction,
): WorkflowState {
  switch (action.type) {
    case "select":
      return validate(action.file);
    case "start_upload":
      return state.kind === "ready"
        ? { kind: "uploading", file: state.file, progress: 0 }
        : state;
    case "advance_upload":
      if (state.kind !== "uploading") return state;
      if (state.progress < 50) return { ...state, progress: 50 };
      return { kind: "scanning", file: state.file, fileId: syntheticFileId };
    case "finish_scan":
      return state.kind === "scanning"
        ? finishScan(state, action.scenario)
        : state;
    case "expire_grant":
      return state.kind === "available"
        ? { ...state, grant: "expired", downloaded: false }
        : state;
    case "renew_grant":
      return state.kind === "available"
        ? { ...state, grant: "active", downloaded: false }
        : state;
    case "request_download":
      return state.kind === "available" && state.grant === "active"
        ? { ...state, downloaded: true }
        : state;
    case "retry":
      if (
        state.kind === "provider_unavailable" ||
        state.kind === "conflict" ||
        state.kind === "upload_error"
      ) {
        return { kind: "ready", file: state.file };
      }
      if (state.kind === "scan_unavailable") {
        return {
          kind: "scanning",
          file: state.file,
          fileId: state.fileId,
        };
      }
      return state;
    case "reset":
      return { kind: "idle" };
    default: {
      const exhaustive: never = action;
      return exhaustive;
    }
  }
}

export function formatBytes(value: number): string {
  if (value < 1024) return `${value} B`;
  if (value < 1024 * 1024) return `${(value / 1024).toFixed(1)} KB`;
  return `${(value / (1024 * 1024)).toFixed(1)} MB`;
}
