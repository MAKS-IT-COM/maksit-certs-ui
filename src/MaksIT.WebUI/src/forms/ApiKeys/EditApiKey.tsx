import { FC, useCallback, useEffect, useState } from 'react'
import { array, boolean, object, string } from 'zod'
import { ApiKeyResponse } from '../../models/apiKey/ApiKeyResponse'
import { useFormState } from '@maks-it.com/webui-core'
import { getData, patchData } from '../../axiosConfig'
import { ApiRoutes, GetApiRoute } from '../../AppMap'
import { PatchApiKeyRequest, createPatchApiKeyRequestSchema } from '../../models/apiKey/PatchApiKeyRequest'
import type { PatchApiKeyEntityScopeRequest } from '../../models/apiKey/PatchApiKeyEntityScopeRequest'
import { deepCopy, deepDelta, deltaHasOperations, ENTITY_SCOPES_ARRAY_POLICY } from '@maks-it.com/webui-core'
import { addToast } from '@maks-it.com/webui-components'
import { FormContainer, FormContent, FormFooter, FormHeader } from '@maks-it.com/webui-components'
import { ButtonComponent, CheckBoxComponent, DateTimePickerComponent, SecretComponent, TextBoxComponent } from '@maks-it.com/webui-components'
import { EditUserScopes, EntityScopeFormProps, EntityScopeFormPropsSchema } from '../shared/EditScopes'
import { useAppSelector } from '../../redux/hooks'


// Form state interface and validation
interface EditApiKeyFormProps {
  [key: string]: string | boolean | EntityScopeFormProps[] | undefined

  id: string
  value: string
  description?: string
  expiresAt?: string
  isGlobalAdmin?: boolean
  entityScopes: EntityScopeFormProps[]
}

const editApiKeyFormPropsProto = (): EditApiKeyFormProps => ({
  id: '',
  value: '',
  description: '',
  expiresAt: undefined,
  isGlobalAdmin: false,
  entityScopes: [],
})

const EditApiKeyFormSchema = object({
  id: string().min(1),
  value: string().min(1),
  description: string(),
  expiresAt: string().optional(),
  isGlobalAdmin: boolean().optional(),
  entityScopes: array(EntityScopeFormPropsSchema)
})

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
    onCancel
  } = props

  const { identity } = useAppSelector(state => state.identity)
  const initialFormState = editApiKeyFormPropsProto()

  const {
    formState,
    errors,
    formIsValid,
    handleInputChange,
    setInitialState
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
      description: response.description ?? undefined,
      expiresAt: response.expiresAt ?? undefined,
      isGlobalAdmin: response.isGlobalAdmin,
      entityScopes: (response.entityScopes ?? []).map(scope => ({
        id: scope.id,
        entityId: scope.entityId,
        entityType: scope.entityType,
        scope: scope.scope
      }))
    }

    setInitialState(newState)
    // Backup = last server state; patchable array items (entityScopes) always have id
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

  const mapFormStateToPatchRequest = (formState: EditApiKeyFormProps): PatchApiKeyRequest => {
    return {
      description: formState.description,
      expiresAt: formState.expiresAt,
      isGlobalAdmin: formState.isGlobalAdmin,
      entityScopes: formState.entityScopes?.map(scope => ({
        id: scope.id,
        entityId: scope.entityId,
        entityType: scope.entityType,
        scope: scope.scope
      }))
    }
  }

  const handleSubmit = () => {
    if (!formIsValid) return

    const fromFormState = mapFormStateToPatchRequest(formState)
    const fromBackupState = mapFormStateToPatchRequest(backupState)

    const delta = deepDelta(fromFormState, fromBackupState, { arrays: { entityScopes: ENTITY_SCOPES_ARRAY_POLICY } })

    // Match Edit user: don't send entityScopes when the delta only has id-only items (other fields changed).
    if (Array.isArray(delta.entityScopes) && delta.entityScopes.length > 0) {
      const collectionOpKey = 'collectionItemOperation' as const
      const onlyIdItems = delta.entityScopes.every(
        (item: PatchApiKeyEntityScopeRequest) =>
          item.entityId === undefined &&
          item.entityType === undefined &&
          item.scope === undefined &&
          (item.operations == null || item.operations[collectionOpKey] == null)
      )
      if (onlyIdItems) delete (delta as Record<string, unknown>).entityScopes
    }

    if (!deltaHasOperations(delta)) {
      addToast('No changes detected', 'info')
      return
    }

    const validated = createPatchApiKeyRequestSchema(identity?.isGlobalAdmin ?? false).safeParse(delta)
    if (!validated.success) {
      validated.error.issues.forEach(error => {
        addToast(error.message, 'error')
      })
      return
    }

    patchData<PatchApiKeyRequest, ApiKeyResponse>(GetApiRoute(ApiRoutes.apikeyPatch).route
      .replace('{apiKeyId}', apiKeyId), validated.data)
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
          errorText={errors.apiKey}
          onChange={(e) => handleInputChange('apiKey', e.target.value)}
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
        <EditUserScopes
          allowIdentityAndApiKeyScopes={true}
          colspan={12}
          id={formState.id}
          entityScopes={formState.entityScopes}
          targetIsGlobalAdmin={formState.isGlobalAdmin ?? false}
          onChange={(entityScopes) => handleInputChange('entityScopes', entityScopes)}
        />
        {identity?.isGlobalAdmin && (
          <CheckBoxComponent
            colspan={12}
            label={'Is Global Admin'}
            value={formState.isGlobalAdmin ?? false}
            onChange={(e) => handleInputChange('isGlobalAdmin', e.target.checked)}
          />
        )}
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
  EditApiKey
}
