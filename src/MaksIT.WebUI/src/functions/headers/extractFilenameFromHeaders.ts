/**
 * Extracts filename from HTTP headers.
 * @param headers The HTTP headers object.
 * @param fallbackName The fallback filename if none found in headers.
 * @return The extracted filename or the fallback name.
 */
const extractFilenameFromHeaders = (
  headers: Record<string, string>,
  fallbackName: string = 'download.bin'
): string => {

  const cd = headers['content-disposition']
  if (!cd) {
    return fallbackName
  }

  // RFC 5987 — filename*=UTF-8''encoded-name
  const matchEncoded = /filename\*=\s*UTF-8''([^;]+)/i.exec(cd)
  if (matchEncoded && matchEncoded[1]) {
    try {
      return decodeURIComponent(matchEncoded[1])
    } catch {
      return matchEncoded[1]
    }
  }

  // Standard — filename="quoted"
  const matchQuoted = /filename="([^"]+)"/i.exec(cd)
  if (matchQuoted && matchQuoted[1]) {
    return matchQuoted[1]
  }

  // Standard — filename=plain
  const matchPlain = /filename=([^;]+)/i.exec(cd)
  if (matchPlain && matchPlain[1]) {
    return matchPlain[1].trim()
  }

  return fallbackName
}

export {
  extractFilenameFromHeaders
}