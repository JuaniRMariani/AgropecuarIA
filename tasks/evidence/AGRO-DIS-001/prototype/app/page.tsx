import { CatalogExplorer } from "./catalog-explorer";
import { loadCatalogPrototypeData } from "../lib/catalog-data";

export default async function CatalogPage() {
  const catalog = await loadCatalogPrototypeData();

  return <CatalogExplorer initialEntries={catalog.entries} metadata={catalog.metadata} />;
}
