import { FC, useMemo } from 'react'
import { object, string, ZodType } from 'zod'
import { ApiKeyResponse } from '../../models/letsEncryptServer/apiKeys/ApiKeyResponse'
import { useFormState } from '../../hooks/useFormState'
import { addToast } from '../../components/Toast/addToast'
import { postData } from '../../axiosConfig'
import { ApiRoutes, GetApiRoute } from '../../AppMap'
import { Offcanvas } from '../../components/Offcanvas'
import { FormContainer, FormContent, FormFooter, FormHeader } from '../../components/FormLayout'
import { ButtonComponent, DateTimePickerComponent, TextBoxComponent } from '../../components/editors'
import { CreateApiKeyRequest, CreateApiKeyRequestSchema } from '../../models/letsEncryptServer/apiKeys/CreateApiKeyRequest'
import { useAppSelector } from '../../redux/hooks'


// Form state interface and validation

interface CreateApiKeyFormProps {
  [key: string]: string | undefined

  description: string
  expiresAt?: string
}

const createApiKeyFormPropsProto = (): CreateApiKeyFormProps => ({
  description: '',
  expiresAt: undefined,
})

const CreateApiKeyFormPropsSchema: ZodType<CreateApiKeyFormProps> = object({
  description: string().min(1),
  expiresAt: string().optional(),
})

interface CreateApiKeyProps {
  isOpen?: boolean
  onSubmitted?: (entity: ApiKeyResponse) => void
  cancelEnabled?: boolean
  onCancel?: () => void
}

const CreateApiKey: FC<CreateApiKeyProps> = (props) => {

  const { isOpen = false, onSubmitted, cancelEnabled = false, onCancel } = props

  const { identity } = useAppSelector(state => state.identity)

  const initialFormState = useMemo(createApiKeyFormPropsProto, [])
  const validationSchema = useMemo(() => CreateApiKeyFormPropsSchema, [])

  const {
    formState,
    errors,
    formIsValid,
    handleInputChange,
  } = useFormState<CreateApiKeyFormProps>({
    initialState: initialFormState,
    validationSchema,
  })

  const handleSubmit = () => {
    if (!formIsValid) return

    const requestData: CreateApiKeyRequest = {
      description: formState.description,
      expiresAt: formState.expiresAt,
    }

    const request = CreateApiKeyRequestSchema.safeParse(requestData)

    if (!request.success) {
      request.error.issues.forEach(error => {
        addToast(error.message, 'error')
      })

      return
    }

    postData<CreateApiKeyRequest, ApiKeyResponse>(GetApiRoute(ApiRoutes.apikeyPost).route, request.data)
      .then(response => {
        if (!response) return

        onSubmitted?.(response)
      })
  }

  const handleCancel = () => {
    onCancel?.()
  }

  if (!identity) return <></>

  return <Offcanvas isOpen={isOpen}>
    <FormContainer>
      <FormHeader>Create API Key</FormHeader>
      <FormContent>
        <div className={'grid grid-cols-12 gap-4 w-full h-full content-start'}>
          <TextBoxComponent
            colspan={12}
            label={'Description'}
            value={formState.description}
            errorText={errors.description}
            onChange={(e) => handleInputChange('description', e.target.value)}
          />
          <DateTimePickerComponent
            colspan={12}
            label={'Expires at'}
            value={formState.expiresAt}
            errorText={errors.expiresAt}
            onChange={(dateTime) => handleInputChange('expiresAt', dateTime)}
          />
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
  </Offcanvas>
}

export {
  CreateApiKey,
}
