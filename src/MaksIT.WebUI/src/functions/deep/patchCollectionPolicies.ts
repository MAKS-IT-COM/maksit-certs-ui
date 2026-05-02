import type { ArrayPolicy } from './deepDelta'

function preferIdElse(
  item: Record<string, unknown>,
  fallback: (item: Record<string, unknown>) => string
): string {
  const id = item.id

  if (id !== null && id !== undefined && String(id).length > 0)
    return String(id)

  return fallback(item)
}

function entityScopeFallback(item: Record<string, unknown>): string {
  const entityId = item.entityId ?? ''
  const entityType = item.entityType ?? ''
  const scope = item.scope ?? ''

  return `${entityId}-${entityType}-${scope}`
}

/**
 * Array policy for API key / user entityScopes payloads from {@link deepDelta}.
 * Pass as: {@code deepDelta(form, backup, { arrays: { entityScopes: ENTITY_SCOPES_ARRAY_POLICY } })}
 */
export const ENTITY_SCOPES_ARRAY_POLICY: ArrayPolicy = {
  identityKey: (item) => preferIdElse(item, entityScopeFallback),
  idFieldKey: '_deltaId',
}
