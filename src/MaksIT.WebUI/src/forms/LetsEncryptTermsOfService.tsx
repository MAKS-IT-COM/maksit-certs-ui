import { FC, useEffect, useRef, useState } from 'react'

import { FormContainer, FormContent, FormFooter, FormHeader } from '../components/FormLayout'
import { ApiRoutes, GetApiRoute } from '../AppMap'
import { getData, postData } from '../axiosConfig'

import { pdfjs, Document, Page } from 'react-pdf'
import 'react-pdf/dist/Page/AnnotationLayer.css'
import 'react-pdf/dist/Page/TextLayer.css'

import type { PDFDocumentProxy } from 'pdfjs-dist'
import pdfWorkerUrl from 'pdfjs-dist/build/pdf.worker.min.mjs?url'

// pdfjs-dist worker (bundled asset URL for prod)
pdfjs.GlobalWorkerOptions.workerSrc = pdfWorkerUrl

const LetsEncryptTermsOfService: FC = () => {

  const [pdfUrl, setPdfUrl] = useState<string | null>(null)
  const [objectUrl, setObjectUrl] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [numPages, setNumPages] = useState<number>()
  const containerRef = useRef<HTMLDivElement>(null)
  const [containerWidth, setContainerWidth] = useState<number>()

  useEffect(() => {
    const handleResize = () => {
      if (containerRef.current) {
        const { x } = containerRef.current.getBoundingClientRect()
        const width = window.innerWidth - x
        setContainerWidth(width)
      }
    }
    handleResize()
    window.addEventListener('resize', handleResize)
    return () => {
      window.removeEventListener('resize', handleResize)
    }
  }, [])

  useEffect(() => {
    setLoading(true)
    postData<{ [key: string]: boolean }, string>(GetApiRoute(ApiRoutes.CERTS_FLOW_CONFIGURE_CLIENT).route, {
      isStaging: true
    })
      .then(response => {
        if (!response) return
        return getData<string>(GetApiRoute(ApiRoutes.CERTS_FLOW_TERMS_OF_SERVICE).route.replace('{sessionId}', response))
      })
      .then(base64Pdf => {
        if (base64Pdf) {
          setPdfUrl(base64Pdf)
        } else {
          setError('Failed to retrieve PDF.')
        }
      })
      .catch(() => setError('Failed to load Terms of Service.'))
      .finally(() => setLoading(false))
  }, [])

  // Convert base64 to Blob and create object URL
  useEffect(() => {
    if (!pdfUrl) return
    // Remove data URL prefix if present
    const base64 = pdfUrl.replace(/^data:application\/pdf;base64,/, '')
    const byteCharacters = atob(base64)
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

  const handleDocumentLoadSuccess = ({ numPages: nextNumPages }: PDFDocumentProxy): void => {
    setNumPages(nextNumPages)
  }

  return (
    <FormContainer>
      <FormHeader>Let's Encrypt Terms of Service</FormHeader>
      <FormContent>
        {loading && <div>Loading Terms of Service...</div>}
        {error && <div style={{ color: 'red' }}>{error}</div>}
        {objectUrl && (
          <div ref={containerRef} className={'w-full overflow-auto'} style={{ minHeight: 600 }}>
            <Document file={objectUrl} onLoadSuccess={handleDocumentLoadSuccess}>
              {numPages ? (
                Array.from(new Array(numPages), (_, index) => (
                  <div key={`page_container_${index + 1}`} className={'page-container'}>
                    <Page
                      key={`page_${index + 1}`}
                      pageNumber={index + 1}
                      width={containerWidth && containerWidth > 0 ? containerWidth : 600}
                    />
                    <div className={'page-number w-full text-center text-sm text-gray-500'}>
                      Page {index + 1} / {numPages}
                    </div>
                  </div>
                ))
              ) : (
                <div>Loading PDF pages...</div>
              )}
            </Document>
          </div>
        )}
      </FormContent>
      <FormFooter />
    </FormContainer>
  )
}

export { LetsEncryptTermsOfService }