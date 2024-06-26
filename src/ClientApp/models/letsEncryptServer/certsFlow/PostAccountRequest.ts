import { isValidContact, isValidHostname } from '@/hooks/useValidation'

export interface PostAccountRequest {
  description: string
  contacts: string[]
  challengeType: string
  hostnames: string[]
  isStaging: boolean
}

const validatePostAccountRequest = (
  request: PostAccountRequest | null
): string[] => {
  const errors: string[] = []

  if (request === null) {
    errors.push('Request is null')
    return errors
  }

  // Validate description
  if (request.description === '') {
    errors.push('Description cannot be empty')
  }

  // Validate contacts
  if (request.contacts.length === 0) {
    errors.push('Contacts cannot be empty')
  }

  request.contacts.forEach((contact) => {
    if (!isValidContact(contact)) {
      errors.push(`Invalid contact: ${contact}`)
    }
  })

  // Validate challenge type
  if (request.challengeType === '') {
    errors.push('Challenge type cannot be empty')
  }

  // Validate hostnames
  if (request.hostnames.length === 0) {
    errors.push('Hostnames cannot be empty')
  }

  request.hostnames.forEach((hostname) => {
    if (!isValidHostname(hostname)) {
      errors.push(`Invalid hostname: ${hostname}`)
    }
  })

  // Return the array of errors
  return errors
}

export { validatePostAccountRequest }
