/**
 * Reads the first `Prop.Contains|StartsWith|EndsWith("…")` value from the combined
 * LINQ-style string produced by DataTable filters (Certs sends the extracted substring as `PagedRequest.filters`).
 */
export function extractPropFilter(combined: string | undefined, propName: string): string | undefined {
  if (!combined?.trim()) return undefined
  const re = new RegExp(`${propName}\\.(?:Contains|StartsWith|EndsWith)\\("([^"]*)"`, 'i')
  const m = combined.match(re)
  return m?.[1]
}
