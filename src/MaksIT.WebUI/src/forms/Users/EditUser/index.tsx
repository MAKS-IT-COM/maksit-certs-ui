import { FC, useState } from 'react'
import { FormContainer, FormContent, FormHeader } from '../../../components/FormLayout'
import { ButtonComponent } from '../../../components/editors'
import { ChangePassword } from './ChangePassword'


interface EditUserProps {
  userId: string;
}

const EditUser : FC<EditUserProps> = (props) => {
  const {
    userId,
  } = props

  const [showChangePassword, setShowChangePassword] = useState(false)
  
  return <>
    <FormContainer>
      <FormHeader>Edit user</FormHeader>
      <FormContent>
        <div className={'grid grid-cols-12 gap-4 w-full'}>
          <ButtonComponent
            colspan={3}
            label={'Change password'}
            buttonHierarchy={'primary'}
            onClick={() => setShowChangePassword(true)}
          />
          <span className={'col-span-9'}></span>
        </div>
      </FormContent>
    </FormContainer>


    <ChangePassword
      userId={userId}
      isOpen={showChangePassword}
      onClose={() => setShowChangePassword(false)}
    />
  </>
}

export {
  EditUser
}

