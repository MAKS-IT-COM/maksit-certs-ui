import { FC, useMemo } from 'react'
import { array, boolean, object, string, ZodType } from 'zod'
import { ApiKeyResponse } from '../../models/certsUI/apiKeys/ApiKeyResponse'
import { useFormState } from '../../hooks/useFormState'
import { addToast } from '../../components/Toast/addToast'
import { postData } from '../../axiosConfig'
import { ApiRoutes, GetApiRoute } from '../../AppMap'
import { Offcanvas } from '../../components/Offcanvas'
import { FormContainer, FormContent, FormFooter, FormHeader } from '../../components/FormLayout'
import { ButtonComponent, CheckBoxComponent, DateTimePickerComponent, TextBoxComponent } from '../../components/editors'
import { CreateApiKeyRequest, CreateApiKeyRequestSchema } from '../../models/certsUI/apiKeys/CreateApiKeyRequest'
import { EditUserScopes, EntityScopeFormProps, EntityScopeFormPropsSchema } from '../shared/EditScopes'
import { deepCopy, hasFlag } from '../../functions'
import { useAppSelector } from '../../redux/hooks'
import { ScopeEntityType, ScopePermission } from '../../models/engine/scopeEnums'


// Form state interface and validation
interface CreateApiKeyFormProps {
  [key: string]: string | boolean | EntityScopeFormProps[] | undefined

  description: string
  expiresAt?: string
  isGlobalAdmin?: boolean
  entityScopes: EntityScopeFormProps []
}

const createApiKeyFormPropsProto = (): CreateApiKeyFormProps => ({
  description: '',
  expiresAt: undefined,
  isGlobalAdmin: false,
  entityScopes: []
})

const CreateApiKeyFormPropsSchema: ZodType<CreateApiKeyFormProps> = object({
  description: string().min(1),
  expiresAt: string().optional(),
  isGlobalAdmin: boolean().optional(),
  entityScopes: array(EntityScopeFormPropsSchema)
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
    setInitialState
  } = useFormState<CreateApiKeyFormProps>({
    initialState: initialFormState,
    validationSchema,
  })

  const handleSubmit = () => {
    if (!formIsValid) return

    const requestData: CreateApiKeyRequest = {
      description: formState.description,
      expiresAt: formState.expiresAt,
      isGlobalAdmin: formState.isGlobalAdmin,
      entityScopes: formState.entityScopes?.map(entityScope => ({
        entityId: entityScope.entityId,
        entityType: entityScope.entityType,
        scope: entityScope.scope
      }))
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
        if (!response.ok || !response.payload) return

        onSubmitted?.(response.payload)
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
          {identity.isGlobalAdmin && (
            <CheckBoxComponent
              colspan={12}
              label={'Is Global Admin'}
              value={formState.isGlobalAdmin ?? false}
              onChange={(e) => handleInputChange('isGlobalAdmin', e.target.checked)}
            />
          )}
          {!formState.isGlobalAdmin && <EditUserScopes
            allowIdentityAndApiKeyScopes={false}
            colspan={12}
            entityScopes={formState.entityScopes}
            emptyStateMessage={'At least one scope is required for non-global admin API keys.'}
            onChange={(entityScopes) => {

              if (!identity.isGlobalAdmin) {
                const allowedOrganizationIds = identity.acls
                  ?.filter(acl => acl.entityType === ScopeEntityType.ApiKey
                  && hasFlag(acl.scope, ScopePermission.Create))
                  .map(acl => acl.entityId) ?? []

                const selectedOrganizationIds = entityScopes
                  ?.map(scope => scope.entityId) ?? []

                if (selectedOrganizationIds.length > 0) {
                  const hasPermissionForAnySelected = selectedOrganizationIds
                    .some(id => allowedOrganizationIds.includes(id))

                  if (!hasPermissionForAnySelected) {
                    addToast(
                      'You do not have permission to create API keys with the selected scope.',
                      'error'
                    )

                    return
                  }
                }
              }

              const newState = deepCopy(formState)
              newState.entityScopes = entityScopes

              setInitialState(newState)
            }}
          />}
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
  CreateApiKey
}
