import { FC, useCallback, useEffect, useState } from 'react'
import { object, string } from 'zod'
import { ApiKeyResponse } from '../../models/letsEncryptServer/apiKeys/ApiKeyResponse'
import { useFormState } from '../../hooks/useFormState'
import { getData, patchData } from '../../axiosConfig'
import { ApiRoutes, GetApiRoute } from '../../AppMap'
import { PatchApiKeyRequest, PatchApiKeyRequestSchema } from '../../models/letsEncryptServer/apiKeys/PatchApiKeyRequest'
import { deepCopy, deepDelta, deltaHasOperations } from '../../functions'
import type { Delta } from '../../functions/deep/deepDelta'
import { addToast } from '../../components/Toast/addToast'
import { FormContainer, FormContent, FormFooter, FormHeader } from '../../components/FormLayout'
import { ButtonComponent, DateTimePickerComponent, SecretComponent, TextBoxComponent } from '../../components/editors'
import { PatchOperation } from '../../models/PatchOperation'


// Form state interface and validation

interface EditApiKeyFormProps {
  [key: string]: string | undefined

  id: string
  value: string
  description?: string
  expiresAt?: string
}

const editApiKeyFormPropsProto = (): EditApiKeyFormProps => ({
  id: '',
  value: '',
  description: '',
  expiresAt: undefined,
})

const EditApiKeyFormSchema = object({
  id: string().min(1),
  value: string(),
  description: string(),
  expiresAt: string().optional(),
})

type PatchableApiKeyFields = {
  description?: string
  expiresAt?: string
}

const mapFormStateToPatchRequest = (formState: EditApiKeyFormProps): PatchableApiKeyFields => ({
  description: formState.description,
  expiresAt: formState.expiresAt,
})

const buildPatchApiKeyRequestFromDelta = (delta: Delta<PatchableApiKeyFields>): PatchApiKeyRequest => {
  const ops = delta.operations ?? {}
  const request: PatchApiKeyRequest = {
    operations: {},
  }

  if (ops.description === PatchOperation.SetField && 'description' in delta) {
    request.description = delta.description as string
    request.operations!.description = PatchOperation.SetField
  }

  if (ops.expiresAt === PatchOperation.SetField && 'expiresAt' in delta) {
    request.expiresAt = delta.expiresAt as string
    request.operations!.expiresAt = PatchOperation.SetField
  }

  if (ops.expiresAt === PatchOperation.RemoveField) {
    request.expiresAt = undefined
    request.operations!.expiresAt = PatchOperation.RemoveField
  }

  return request
}


// Component properties

interface EditApiKeyProps {
  apiKeyId: string
  onSubmitted?: (entity: ApiKeyResponse) => void
  cancelEnabled?: boolean
  onCancel?: () => void
}

const EditApiKey: FC<EditApiKeyProps> = (props) => {
  const {
    apiKeyId,
    onSubmitted,
    cancelEnabled = false,
    onCancel,
  } = props

  const initialFormState = editApiKeyFormPropsProto()

  const {
    formState,
    errors,
    formIsValid,
    handleInputChange,
    setInitialState,
  } = useFormState<EditApiKeyFormProps>({
    initialState: initialFormState,
    validationSchema: EditApiKeyFormSchema,
  })

  const [backupState, setBackupState] = useState<EditApiKeyFormProps>(initialFormState)
  const [hasLoaded, setHasLoaded] = useState(false)

  const handleInitialization = useCallback((response: ApiKeyResponse) => {
    const newState: EditApiKeyFormProps = {
      id: response.id,
      value: response.apiKey,
      description: response.description ?? '',
      expiresAt: response.expiresAt,
    }

    setInitialState(newState)
    // Backup = last server state
    setBackupState(deepCopy(newState))
  }, [setInitialState])

  useEffect(() => {
    setHasLoaded(false)

    getData<ApiKeyResponse>(GetApiRoute(ApiRoutes.apikeyGet).route
      .replace('{apiKeyId}', apiKeyId))
      .then(response => {
        if (!response.ok || !response.payload) {
          // Leave form state as initial defaults; id will remain empty and
          // the "not found" UI will be shown below.
          setInitialState(deepCopy(initialFormState))
          setBackupState(deepCopy(initialFormState))
          return
        }

        handleInitialization(response.payload)
      })
      .finally(() => {
        setHasLoaded(true)
      })
  }, [apiKeyId, handleInitialization, setInitialState])

  const handleSubmit = () => {
    if (!formIsValid) return

    const fromFormState = mapFormStateToPatchRequest(formState)
    const fromBackupState = mapFormStateToPatchRequest(backupState)

    const delta = deepDelta(
      fromFormState as Record<string, unknown>,
      fromBackupState as Record<string, unknown>
    ) as Delta<PatchableApiKeyFields>

    if (!deltaHasOperations(delta as Record<string, unknown>)) {
      addToast('No changes detected', 'info')
      return
    }

    const requestData = buildPatchApiKeyRequestFromDelta(delta)
    const request = PatchApiKeyRequestSchema.safeParse(requestData)
    if (!request.success) {
      request.error.issues.forEach(error => {
        addToast(error.message, 'error')
      })

      return
    }

    patchData<PatchApiKeyRequest, ApiKeyResponse>(GetApiRoute(ApiRoutes.apikeyPatch).route
      .replace('{apiKeyId}', apiKeyId), request.data)
      .then(response => {
        if (!response.ok || !response.payload) return

        handleInitialization(response.payload)
        onSubmitted?.(response.payload)
      })
  }

  const handleCancel = () => {
    onCancel?.()
  }

  if (!hasLoaded) {
    return (
      <FormContainer>
        <FormHeader>Edit API Key</FormHeader>
        <FormContent><p>Loading API key...</p></FormContent>
      </FormContainer>
    )
  }

  if (!formState.id) {
    return (
      <FormContainer>
        <FormHeader>Edit API Key</FormHeader>
        <FormContent><p>API key not found or not accessible.</p></FormContent>
        <FormFooter leftChildren={cancelEnabled && <ButtonComponent label={'Back'} buttonHierarchy={'secondary'} onClick={handleCancel} />} />
      </FormContainer>
    )
  }

  return <FormContainer>
    <FormHeader>Edit API Key {apiKeyId}</FormHeader>
    <FormContent>
      <div className={'grid grid-cols-12 gap-4 w-full h-full content-start'}>
        <p className={'col-span-12'}>Prop from URL: {apiKeyId}</p>
        <SecretComponent
          colspan={12}
          label={'API Key'}
          value={formState.value}
          errorText={errors.value}
          onChange={(e) => handleInputChange('value', e.target.value)}
          readOnly={true}
          enableCopy={true}
        />
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
}

export {
  EditApiKey,
}
