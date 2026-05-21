/**
 * Typed Hub API client surface. The actual client is generated from the Hub
 * backend's `/openapi/v1.json` during CI (P02c-2 wires the generation).
 *
 * Operator portal code MUST route through this SDK — direct `fetch` from
 * Client Components is forbidden (mirrors LearnStack core Standards 03
 * § Forbidden).
 */
export * from "./client";
export * from "./server";
