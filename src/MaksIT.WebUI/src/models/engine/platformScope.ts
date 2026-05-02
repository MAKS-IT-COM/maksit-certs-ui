/**
 * Identity / ApiKey scope rows require a GUID `entityId`. Certs UI has no org dimension, so the UI uses this
 * stable sentinel for product-wide user- and API-key-management scopes. Do not change without a data migration.
 */
export const CERTS_UI_PLATFORM_SCOPE_ENTITY_ID =
  '00000000-0000-0000-0000-000000000001'
