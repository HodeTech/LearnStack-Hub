/**
 * Server-side Hub SDK entry. Reads operator identity (Keycloak learnstack-hub
 * realm JWT) from request headers set by the BFF middleware (P02c-4 wires the
 * actual auth integration).
 */
export type ServerSdkOptions = {
  readonly operatorId: string;
  readonly accessToken: string;
};

export function createServerHubSdk(_options: ServerSdkOptions) {
  // Placeholder — wired up alongside the first generated route in P02c-2.
  return {} as const;
}
