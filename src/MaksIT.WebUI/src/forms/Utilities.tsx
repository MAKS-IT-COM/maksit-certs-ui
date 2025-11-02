import { FC, useState } from 'react'
import { FormContainer, FormContent, FormFooter, FormHeader } from '../components/FormLayout'
import { ButtonComponent, FileUploadComponent } from '../components/editors'

const Utilities: FC = () => {

  const [files, setFiles] = useState<File[]>([])

  return <FormContainer>
    <FormHeader>Utilities</FormHeader>
    <FormContent>
      <div className={'grid grid-cols-12 gap-4 w-full'}>
        <ButtonComponent
          colspan={3}
          label={'Test agent'}
          buttonHierarchy={'primary'}
          onClick={() => {}}
        />
      
        <ButtonComponent
          colspan={3}
          label={'Download cache files'}
          buttonHierarchy={'secondary'}
          onClick={() => {}}
        />

        <FileUploadComponent
          colspan={6}
          label={'Upload cache files'}
          multiple={true}
          onChange={setFiles}
        />

        <span className={'col-span-12'}></span>
      </div>
     
    </FormContent>
    <FormFooter />
  </FormContainer>
}

export { Utilities }