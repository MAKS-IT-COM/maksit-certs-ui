'use client'

import { ApiRoutes, GetApiRoute } from '@/ApiRoutes'
import { PageContainer } from '@/components/pageContainer'
import { CustomButton, CustomFileUploader } from '@/controls'
import { showToast } from '@/redux/slices/toastSlice'
import { useAppDispatch } from '@/redux/store'
import { httpService } from '@/services/HttpService'
import { useEffect, useState } from 'react'

const TestPage = () => {
  const dispatch = useAppDispatch()

  const [files, setFiles] = useState<File[] | null>(null)

  const handleTestAgent = async () => {
    httpService
      .get<string>(GetApiRoute(ApiRoutes.AGENT_TEST))
      .then((response) => {
        dispatch(
          showToast({
            message: JSON.stringify(response),
            type: 'info'
          })
        )
      })
  }

  const handleDownloadCache = async () => {}

  const handleUploadCache = async () => {}

  const handleRestoreFromCache = async () => {}

  useEffect(() => {
    if (files && files.length > 0) {
      handleUploadCache()
    }
  }, [files])

  return (
    <PageContainer title="CertsUI Utils">
      <CustomButton
        type="button"
        onClick={handleTestAgent}
        className="bg-green-500 text-white p-2 rounded ml-2"
      >
        Test Agent
      </CustomButton>

      <CustomButton
        type="button"
        onClick={handleRestoreFromCache}
        className="bg-yellow-500 text-white p-2 rounded ml-2"
      >
        Restore from cache
      </CustomButton>

      <CustomButton
        type="button"
        onClick={handleDownloadCache}
        className="bg-blue-500 text-white p-2 rounded ml-2"
      >
        Download cache
      </CustomButton>

      <CustomFileUploader
        value={files}
        onChange={setFiles}
        accept=".zip"
        buttonClassName="bg-blue-500 text-white p-2 rounded ml-2"
        title="Upload cache"
      />
    </PageContainer>
  )
}

export default TestPage
