import { FC, useState } from 'react'
import { FormContainer, FormContent, FormFooter, FormHeader } from '../components/FormLayout'
import { ButtonComponent, DateTimePickerComponent, FileUploadComponent } from '../components/editors'
import { ApiRoutes, GetApiRoute } from '../AppMap'
import { getData } from '../axiosConfig'
import { addToast } from '../components/Toast/addToast'

const Utilities: FC = () => {

  const [files, setFiles] = useState<File[]>([])

  const hadnleTestAgent = () => {
    getData(GetApiRoute(ApiRoutes.AGENT_TEST).route)
      .then((response) => {
        if (!response) return

        addToast(response?.message, 'info')
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

        <FileUploadComponent
          colspan={6}
          label={'Upload cache files'}
          multiple={true}
          onChange={setFiles}
        />

        <ButtonComponent
          colspan={3}
          children={'Download cache files'}
          buttonHierarchy={'secondary'}
          onClick={() => {}}
        />

        <ButtonComponent
          colspan={3}
          children={'Destroy cache files'}
          buttonHierarchy={'error'}
          onClick={() => {}}
        />
      </div>
    </FormContent>
    <FormFooter />
  </FormContainer>
}

export { Utilities }