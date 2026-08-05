import type { OrganizationSummary, PrototypeUser } from "./identity-machine";

export const prototypeUser: PrototypeUser = {
  id: "9f4c2d11-88a1-4ef8-99c1-5b4ba407a532",
  name: "Marina Ferreyra",
  email: "marina.ferreyra@example.test",
};

export const prototypeOrganizations: readonly OrganizationSummary[] = [
  {
    id: "a10b2c33-164d-48d3-8f8b-f712de9db101",
    name: "La Rinconada",
    locality: "Pergamino",
    role: "OWNER",
    type: "PRODUCER",
  },
  {
    id: "b71d9e24-e529-4ed7-a709-03f1a5b456c2",
    name: "Los Álamos",
    locality: "Rafaela",
    role: "ADVISOR",
    type: "COMPANY",
  },
  {
    id: "c45f8a30-bab0-4d96-9fd3-148514a3c012",
    name: "Campo Sur",
    locality: "Tandil",
    role: "ADVISOR",
    type: "COMPANY",
  },
];
