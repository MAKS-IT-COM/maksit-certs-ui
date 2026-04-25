import { FC, useMemo } from 'react'
import { object, RefinementCtx, string, ZodType } from 'zod'
import { UserResponse } from '../../models/identity/user/UserResponse'
import { useFormState } from '../../hooks/useFormState'
import { Offcanvas } from '../../components/Offcanvas'
import { FormContainer, FormContent, FormFooter, FormHeader } from '../../components/FormLayout'
import { ButtonComponent, TextBoxComponent } from '../../components/editors'
import { addToast } from '../../components/Toast/addToast'
import { CreateUserRequest, CreateUserRequestSchema } from '../../models/identity/user/CreateUserRequest'
import { postData } from '../../axiosConfig'
import { ApiRoutes, GetApiRoute } from '../../AppMap'

interface CreateUserFormProps {
  username: string
  password: string
  rePassword: string
}

const createUserFormPropsProto = (): CreateUserFormProps => ({
  username: '',
  password: '',
  rePassword: '',
})

const CreateUserFormSchema: ZodType<CreateUserFormProps> = object({
  username: string().min(1),
  password: string().min(8),
  rePassword: string().min(1),
}).superRefine((val: CreateUserFormProps, ctx: RefinementCtx) => {
  if (val.password !== val.rePassword) {
    ctx.addIssue({
      code: 'custom',
      message: 'Passwords do not match',
      path: ['rePassword'],
    })
  }
})

interface CreateUserProps {
  isOpen?: boolean
  onSubmitted?: (entity: UserResponse) => void
  cancelEnabled?: boolean
  onCancel?: () => void
}

const CreateUser: FC<CreateUserProps> = (props) => {
  const { isOpen = false, onSubmitted, cancelEnabled = false, onCancel } = props

  const initialFormState = useMemo(createUserFormPropsProto, [])
  const validationSchema = useMemo(() => CreateUserFormSchema, [])

  const {
    formState,
    errors,
    formIsValid,
    handleInputChange,
    setInitialState,
  } = useFormState<CreateUserFormProps>({
    initialState: initialFormState,
    validationSchema,
  })

  const handleSubmit = () => {
    if (!formIsValid) return

    const requestData: CreateUserRequest = {
      username: formState.username.trim(),
      password: formState.password,
    }

    const request = CreateUserRequestSchema.safeParse(requestData)
    if (!request.success) {
      request.error.issues.forEach((err) => addToast(err.message, 'error'))
      return
    }

    postData<CreateUserRequest, UserResponse>(GetApiRoute(ApiRoutes.identityPost).route, request.data)
      .then((response) => {
        if (!response) return
        setInitialState(createUserFormPropsProto())
        onSubmitted?.(response)
      })
  }

  const handleCancel = () => {
    setInitialState(createUserFormPropsProto())
    onCancel?.()
  }

  return (
    <Offcanvas isOpen={isOpen}>
      <FormContainer>
        <FormHeader>Create user</FormHeader>
        <FormContent>
          <div className={'grid grid-cols-12 gap-4 w-full h-full content-start'}>
            <TextBoxComponent
              colspan={12}
              label={'Username'}
              value={formState.username}
              errorText={errors.username}
              onChange={(e) => handleInputChange('username', e.target.value)}
            />
            <TextBoxComponent
              type={'password'}
              colspan={12}
              label={'Password'}
              value={formState.password}
              errorText={errors.password}
              onChange={(e) => handleInputChange('password', e.target.value)}
            />
            <TextBoxComponent
              type={'password'}
              colspan={12}
              label={'Re-enter password'}
              value={formState.rePassword}
              errorText={errors.rePassword}
              onChange={(e) => handleInputChange('rePassword', e.target.value)}
            />
          </div>
        </FormContent>
        <FormFooter
          rightChildren={
            <ButtonComponent label={'Save'} buttonHierarchy={'primary'} onClick={handleSubmit} />
          }
          leftChildren={
            cancelEnabled ? (
              <ButtonComponent label={'Cancel'} buttonHierarchy={'secondary'} onClick={handleCancel} />
            ) : undefined
          }
        />
      </FormContainer>
    </Offcanvas>
  )
}

export { CreateUser }
