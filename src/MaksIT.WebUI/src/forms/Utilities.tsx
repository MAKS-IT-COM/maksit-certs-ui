import { FC, useState } from 'react'
import { FormContainer, FormContent, FormFooter, FormHeader } from '../components/FormLayout'
import { ButtonComponent, FileUploadComponent } from '../components/editors'
import { ApiRoutes, GetApiRoute } from '../AppMap'
import { deleteData, getBinary, getData, postFile } from '../axiosConfig'
import { addToast } from '../components/Toast/addToast'
import { extractFilenameFromHeaders, saveBinaryToDisk } from '../functions'
import { downloadZip } from 'client-zip'


const Utilities: FC = () => {

  const [files, setFiles] = useState<File[]>([])

  const hadnleTestAgent = () => {
    getData(GetApiRoute(ApiRoutes.AGENT_TEST).route)
      .then((response) => {
        if (!response) return

        addToast(response?.message, 'info')
      })
  }

  const handleUploadFiles = async () => {
    if (files.length === 0) {
      addToast('No files selected for upload', 'error')
      return
    }

    const zipBlob = await downloadZip(files).blob()
    // Option A: direct file helper
    postFile(GetApiRoute(ApiRoutes.FULL_CACHE_UPLOAD_POST).route, zipBlob, 'file', 'cache.zip')
      .then((_) => {
        setFiles([])
        addToast('Files uploaded successfully', 'success')
      })
  }

  const handleDownloadFiles = () => {
    getBinary(GetApiRoute(ApiRoutes.FULL_CACHE_DOWNLOAD_GET).route
    ).then((response) => {
      if (!response) return

      const { data, headers } = response
      const filename = extractFilenameFromHeaders(headers, 'cache.zip')
      saveBinaryToDisk(data, filename)
    })
  }

  const handleDestroyFiles = () => {
    deleteData(GetApiRoute(ApiRoutes.FULL_CACHE_DELETE).route)
      .then((_) => {
        addToast('Cache files destroyed successfully', 'success')
      })
  }


  return <FormContainer>
    <FormHeader>Utilities</FormHeader>
    <FormContent>
      <div className={'grid grid-cols-12 gap-4 w-full'}>
        <ButtonComponent
          colspan={3}
          label={'Test agent'}
          buttonHierarchy={'warning'}
          onClick={hadnleTestAgent}
        />

        <span className={'col-span-9'}></span>

        <FileUploadComponent
          colspan={6}
          label={'Select cache files'}
          multiple={true}
          onChange={setFiles}
        />

        <ButtonComponent
          colspan={3}
          children={'Upload cache files'}
          buttonHierarchy={'primary'}
          onClick={handleUploadFiles}
        />

        <span className={'col-span-3'}></span>


        <ButtonComponent
          colspan={3}
          children={'Download cache files'}
          buttonHierarchy={'secondary'}
          onClick={handleDownloadFiles}
        />

        <ButtonComponent
          colspan={3}
          children={'Destroy cache files'}
          buttonHierarchy={'error'}
          onClick={handleDestroyFiles}
        />
      </div>
    </FormContent>
    <FormFooter />
  </FormContainer>
}

export { Utilities }