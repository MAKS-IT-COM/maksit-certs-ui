/**
 * Vault attaches Identity / ApiKey scopes to an **organization** `entityId`.
 * Certs UI is single-app / no org dimension: scope rows still require a GUID `entityId`, so we use this stable sentinel
 * for “whole product” user-management and API-key-management scopes (must stay aligned with stored JWT ACLs / DB rows).
 */
export const CERTS_UI_PLATFORM_SCOPE_ENTITY_ID =
  '00000000-0000-0000-0000-000000000001'
