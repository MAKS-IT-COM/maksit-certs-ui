import { FC } from 'react'
import { ButtonComponent, TextBoxComponent } from '../../../components/editors'
import { Offcanvas } from '../../../components/Offcanvas'
import { PatchUserChangePasswordRequest, PatchUserChangePasswordRequestSchema } from '../../../models/identity/user/PatchUserRequest'
import { useFormState } from '../../../hooks/useFormState'
import { PatchOperation } from '../../../models/PatchOperation'
import { UserResponse } from '../../../models/identity/user/UserResponse'
import { ApiRoutes, GetApiRoute } from '../../../AppMap'
import { patchData } from '../../../axiosConfig'
import { FormContainer, FormContent, FormHeader } from '../../../components/FormLayout'

interface ChangePasswordProps {
    userId: string;
    isOpen?: boolean;
    onClose?: () => void;
}

const ChangePassword: FC<ChangePasswordProps> = (props) => {
  const {
    userId,
    isOpen = false,
    onClose
  } = props

  const {
    formState,
    errors,
    formIsValid,
    handleInputChange,
    setInitialState
  } = useFormState<PatchUserChangePasswordRequest>({
    initialState: {
      password: '',
      confirmPassword: ''
    },
    validationSchema: PatchUserChangePasswordRequestSchema,
  })

  const handleOnClose = () => {
    setInitialState({
      password: '',
      confirmPassword: ''
    })

    onClose?.()
  }

  const handleSave = () => {
    if (formIsValid) {
      const data: PatchUserChangePasswordRequest = {
        password: formState.password,
        operations: {
          password: PatchOperation.SetField
        }
      }

      patchData<PatchUserChangePasswordRequest, UserResponse>(GetApiRoute(ApiRoutes.identityPatch).route.replace('{userId}', userId), data)
        .then(response => {
          if (!response.ok || !response.payload) return

          handleOnClose()
        })
    }
  }

  return <Offcanvas isOpen={isOpen}>
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
          <ButtonComponent
            colspan={12}
            label={'Save'}
            buttonHierarchy={'primary'}
            onClick={handleSave}
          />
          <ButtonComponent
            colspan={12}
            label={'Cancel'}
            buttonHierarchy={'secondary'}
            onClick={handleOnClose}
          />
        </div>
      </FormContent>
    </FormContainer>
  </Offcanvas>
}

export {
  ChangePassword
}
