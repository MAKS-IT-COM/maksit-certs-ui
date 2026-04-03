import { FC, useEffect, useState } from 'react'

import { FormContainer, FormContent, FormFooter, FormHeader } from '../components/FormLayout'
import { ApiRoutes, GetApiRoute } from '../AppMap'
import { getData, postData } from '../axiosConfig'

/**
 * Uses the browser's native PDF viewer (iframe + blob URL). No PDF.js / workers / extra MIME rules.
 * UX varies slightly by browser; mobile Safari may open PDF in a new context instead of inline.
 */
const LetsEncryptTermsOfService: FC = () => {
  const [pdfUrl, setPdfUrl] = useState<string | null>(null)
  const [objectUrl, setObjectUrl] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    setLoading(true)
    postData<{ [key: string]: boolean }, string>(GetApiRoute(ApiRoutes.CERTS_FLOW_CONFIGURE_CLIENT).route, {
      isStaging: true
    })
      .then(response => {
        if (!response) return
        return getData<string>(
          GetApiRoute(ApiRoutes.CERTS_FLOW_TERMS_OF_SERVICE).route.replace('{sessionId}', response),
          120_000
        )
      })
      .then(base64Pdf => {
        if (typeof base64Pdf === 'string' && base64Pdf.length > 0) {
          setPdfUrl(base64Pdf)
        } else {
          setError('Failed to retrieve PDF.')
        }
      })
      .catch(() => setError('Failed to load Terms of Service.'))
      .finally(() => setLoading(false))
  }, [])

  useEffect(() => {
    if (!pdfUrl) return
    const base64 = pdfUrl.replace(/^data:application\/pdf;base64,/, '').replace(/\s/g, '')
    let byteCharacters: string
    try {
      byteCharacters = atob(base64)
    } catch {
      setError('Invalid PDF data from server.')
      return
    }
    const byteNumbers = new Array(byteCharacters.length)
    for (let i = 0; i < byteCharacters.length; i++) {
      byteNumbers[i] = byteCharacters.charCodeAt(i)
    }
    const byteArray = new Uint8Array(byteNumbers)
    const blob = new Blob([byteArray], { type: 'application/pdf' })
    const url = URL.createObjectURL(blob)
    setObjectUrl(url)
    return () => {
      URL.revokeObjectURL(url)
    }
  }, [pdfUrl])

  return (
    <FormContainer>
      <FormHeader>Let's Encrypt Terms of Service</FormHeader>
      <FormContent className={'flex flex-col overflow-hidden'}>
        <div className={'flex min-h-0 flex-1 flex-col gap-2'}>
          {loading && <div className={'shrink-0'}>Loading Terms of Service...</div>}
          {error && <div className={'shrink-0 text-red-600'}>{error}</div>}
          {objectUrl && !error && (
            <iframe
              title={"Let's Encrypt Terms of Service PDF"}
              src={objectUrl}
              className={
                'min-h-0 w-full flex-1 rounded border border-gray-200 bg-gray-50'
              }
              style={{ borderWidth: 1 }}
            />
          )}
        </div>
      </FormContent>
      <FormFooter />
    </FormContainer>
  )
}

export { LetsEncryptTermsOfService }
