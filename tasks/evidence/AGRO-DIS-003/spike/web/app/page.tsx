import { IdentityJourney } from "../features/identity/identity-journey";
import { prototypeOrganizations, prototypeUser } from "../lib/identity-fixture";

export default function Home() {
  return <IdentityJourney user={prototypeUser} organizations={prototypeOrganizations} />;
}
