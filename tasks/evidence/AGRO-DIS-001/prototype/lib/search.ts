import type { CatalogDomain, CatalogEntry, SupportLevel } from "./catalog-types";

const COMBINING_MARKS = /[\u0300-\u036f]/g;
const NON_ALPHANUMERIC = /[^a-z0-9]+/g;

export type DomainFilter = CatalogDomain | "ALL";
export type SupportFilter = SupportLevel | "ALL";

export type SearchFilters = Readonly<{
  query: string;
  domain: DomainFilter;
  support: SupportFilter;
}>;

export function normalizeSearchValue(value: string): string {
  return value
    .normalize("NFD")
    .replace(COMBINING_MARKS, "")
    .toLocaleLowerCase("es-AR")
    .replace(NON_ALPHANUMERIC, " ")
    .trim()
    .replace(/\s+/g, " ");
}

function matchesQuery(entry: CatalogEntry, query: string): boolean {
  if (query.length === 0) {
    return true;
  }

  const searchableValues = [entry.code, entry.canonicalName, ...entry.aliases];
  return searchableValues.some((value) => normalizeSearchValue(value).includes(query));
}

export function searchCatalog(
  entries: readonly CatalogEntry[],
  filters: SearchFilters,
): readonly CatalogEntry[] {
  const query = normalizeSearchValue(filters.query);

  return entries
    .filter((entry) => filters.domain === "ALL" || entry.domain === filters.domain)
    .filter((entry) => filters.support === "ALL" || entry.supportLevel === filters.support)
    .filter((entry) => matchesQuery(entry, query))
    .toSorted((left, right) =>
      left.canonicalName.localeCompare(right.canonicalName, "es-AR", { sensitivity: "base" }),
    );
}
