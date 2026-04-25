import { FC } from 'react'
import { ButtonComponent, TextBoxComponent } from '../../../components/editors'
import { Offcanvas } from '../../../components/Offcanvas'
import { PatchUserChangePasswordRequest, PatchUserChangePasswordRequestSchema } from '../../../models/identity/user/PatchUserRequest'
import { useFormState } from '../../../hooks/useFormState'
import { PatchOperation } from '../../../models/PatchOperation'
import { UserResponse } from '../../../models/identity/user/UserResponse'
import { ApiRoutes, GetApiRoute } from '../../../AppMap'
import { patchData } from '../../../axiosConfig'
import { FormContainer, FormContent, FormFooter, FormHeader } from '../../../components/FormLayout'
import { addToast } from '../../../components/Toast/addToast'

interface ChangePasswordProps {
  userId: string
  isOpen?: boolean
  onClose?: () => void
  onSubmitted?: (user: UserResponse) => void
}

const ChangePassword: FC<ChangePasswordProps> = (props) => {
  const {
    userId,
    isOpen = false,
    onClose,
    onSubmitted,
  } = props

  const {
    formState,
    errors,
    formIsValid,
    handleInputChange,
    setInitialState,
  } = useFormState<PatchUserChangePasswordRequest>({
    initialState: {
      password: '',
      confirmPassword: '',
    },
    validationSchema: PatchUserChangePasswordRequestSchema,
  })

  const handleOnClose = () => {
    setInitialState({
      password: '',
      confirmPassword: '',
    })
    onClose?.()
  }

  const handleSave = async () => {
    if (!formIsValid) return
    const data: PatchUserChangePasswordRequest = {
      password: formState.password,
      operations: {
        password: PatchOperation.SetField,
      },
    }

    const response = await patchData<PatchUserChangePasswordRequest, UserResponse>(
      GetApiRoute(ApiRoutes.identityPatch).route.replace('{userId}', userId),
      data
    )

    if (!response) return

    addToast('Password updated.', 'success')
    onSubmitted?.(response)
    handleOnClose()
  }

  return (
    <Offcanvas isOpen={isOpen}>
      <FormContainer>
        <FormHeader>Change password</FormHeader>
        <FormContent>
          <div className={'grid grid-cols-12 gap-4 w-full h-full content-start'}>
            <TextBoxComponent
              colspan={12}
              type={'password'}
              label={'Password'}
              value={formState.password}
              errorText={errors.password}
              onChange={(e) => handleInputChange('password', e.target.value)}
            />
            <TextBoxComponent
              colspan={12}
              type={'password'}
              label={'Confirm password'}
              value={formState.confirmPassword}
              errorText={errors.confirmPassword}
              onChange={(e) => handleInputChange('confirmPassword', e.target.value)}
            />
          </div>
        </FormContent>
        <FormFooter
          rightChildren={
            <ButtonComponent label={'Save'} buttonHierarchy={'primary'} onClick={() => void handleSave()} />
          }
          leftChildren={
            <ButtonComponent label={'Cancel'} buttonHierarchy={'secondary'} onClick={handleOnClose} />
          }
        />
      </FormContainer>
    </Offcanvas>
  )
}

export { ChangePassword }
