import { z } from 'zod'

const guidHex =
  /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/

/**
 * Accepts any standard 8-4-4-4-12 hex GUID string that .NET / PostgreSQL accept.
 * Zod's {@link z.string().uuid} only allows RFC 4122 variants and rejects sentinel IDs such as the Certs UI platform scope entity id.
 */
export const guidStringSchema = z.string().refine((s) => guidHex.test(s.trim()), {
  message: 'Invalid UUID',
})

export const guidStringSchemaOptionalNullable = guidStringSchema.optional().nullable()
