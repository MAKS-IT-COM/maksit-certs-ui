import { isValidContact, isValidHostname } from '@/hooks/useValidation'

export interface PostAccountRequest {
  description?: string
  contacts: string[]
  hostnames: string[]
}

const validatePostAccountRequest = (
  request: PostAccountRequest | null
): string | null => {
  if (request === null) return 'Request is null'

  // Validate contacts
  for (const contact of request.contacts) {
    if (!isValidContact(contact)) {
      return `Invalid contact: ${contact}`
    }
  }

  // Validate hostnames
  for (const hostname of request.hostnames) {
    if (!isValidHostname(hostname)) {
      return `Invalid hostname: ${hostname}`
    }
  }

  // If all validations pass, return null
  return null
}

export { validatePostAccountRequest }
