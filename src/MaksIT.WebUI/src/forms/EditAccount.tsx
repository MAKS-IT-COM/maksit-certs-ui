import { FC } from 'react'
import { FormContainer, FormContent, FormFooter, FormHeader } from '../components/FormLayout'
import { ButtonComponent, CheckBoxComponent, TextBoxComponent } from '../components/editors'
import { GetAccountResponse } from '../models/letsEncryptServer/account/responses/GetAccountResponse'
import { useFormState } from '../hooks/useFormState'
import { boolean, object, Schema, string } from 'zod'


interface EditAccountFormProps {
  description: string
  disabled: boolean
}

const RegisterFormProto = (): EditAccountFormProps => ({
  description: '',
  disabled: false
})

const RegisterFormSchema: Schema<EditAccountFormProps> = object({
  description: string(),
  disabled: boolean()
})

interface EditAccountProps {
    accountId: string
    onSubmitted?: (entity: GetAccountResponse) => void
    cancelEnabled?: boolean
    onCancel?: () => void
}

const EditAccount: FC<EditAccountProps> = (props) => {
  const {
    accountId,
    onSubmitted,
    cancelEnabled,
    onCancel
  } = props


  const {
    formState,
    errors,
    formIsValid,
    handleInputChange
  } = useFormState<EditAccountFormProps>({
    initialState: RegisterFormProto(),
    validationSchema: RegisterFormSchema
  })

  const handleSubmit = () => {
    // onSubmitted && onSubmitted(updatedEntity)
  }

  const handleCancel = () => {
    onCancel?.()
  }

  return <FormContainer>
    <FormHeader>Edit Account {accountId}</FormHeader>
    <FormContent>
      <div className={'grid grid-cols-12 gap-4 w-full'}>
        <TextBoxComponent
          colspan={12}
          label={'Account Description'}
          value={formState.description}
          onChange={(e) => handleInputChange('description', e.target.value)}
          placeholder={'Account Description'}
          errorText={errors.description}
        />
        <CheckBoxComponent
          colspan={12}
          label={'Disabled'}
          value={formState.disabled}
          onChange={(e) => handleInputChange('disabled', e.target.checked)}
          errorText={errors.disabled}
        />

        <h3 className={'col-span-12'}>Contacts:</h3>
        
      </div>
    </FormContent>
    <FormFooter
    
      rightChildren={
        <ButtonComponent label={'Save'} buttonHierarchy={'primary'} onClick={handleSubmit} />
      }
      leftChildren={
        cancelEnabled && <ButtonComponent label={'Cancel'} buttonHierarchy={'secondary'} onClick={handleCancel} />
      }
    />
  </FormContainer>
}

export {
  EditAccount
}