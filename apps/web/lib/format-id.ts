const DASHES = /-/g;

export function formatShortId(id: string): string {
  return id.replace(DASHES, "").slice(0, 6).toUpperCase();
}
