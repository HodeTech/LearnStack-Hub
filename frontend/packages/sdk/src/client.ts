/**
 * Client-side Hub SDK entry. Reads operator identity from request-scoped
 * context established by the BFF middleware. The real implementation lands
 * alongside the first generated route in P02c-2.
 */
export type ClientSdkOptions = {
  readonly operatorId: string;
};

export function createClientHubSdk(_options: ClientSdkOptions) {
  // Placeholder — wired up alongside the first generated route in P02c-2.
  return {} as const;
}
