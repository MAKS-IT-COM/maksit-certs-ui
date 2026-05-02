import { FC, useMemo } from 'react'
import { UserResponse } from '../../models/identity/user/UserResponse'
import { array, boolean, object, RefinementCtx, string, ZodType } from 'zod'
import { useFormState } from '../../hooks/useFormState'
import { Offcanvas } from '../../components/Offcanvas'
import { FormContainer, FormContent, FormFooter, FormHeader } from '../../components/FormLayout'
import { ButtonComponent, CheckBoxComponent, TextBoxComponent } from '../../components/editors'
import { EditUserScopes, EntityScopeFormProps, EntityScopeFormPropsSchema } from '../shared/EditScopes'
import { addToast } from '../../components/Toast/addToast'
import { CreateUserRequest, CreateUserRequestSchema } from '../../models/identity/user/CreateUserRequest'
import { postData } from '../../axiosConfig'
import { ApiRoutes, GetApiRoute } from '../../AppMap'
import { deepCopy, hasFlag } from '../../functions'
import { useAppSelector } from '../../redux/hooks'
import { ScopeEntityType, ScopePermission } from '../../models/engine/scopeEnums'


// Form state interface and validation
interface CreateUserFormProps {
    [key: string]: string | boolean | EntityScopeFormProps[] | undefined
    username: string
    email: string
    mobileNumber: string
    password: string
    rePassword: string
    isGlobalAdmin?: boolean
    entityScopes: EntityScopeFormProps[]
}

const createUserFormPropsProto = (): CreateUserFormProps => ({
  username: '',
  email: '',
  mobileNumber: '',
  password: '',
  rePassword: '',
  entityScopes: []
})

const CreateUserFormSchema: ZodType<CreateUserFormProps> = object({
  username: string().min(1),
  email: string().min(1),
  mobileNumber: string().min(1),
  password: string().min(1),
  rePassword: string().min(1),
  isGlobalAdmin: boolean().optional(),
  entityScopes: array(EntityScopeFormPropsSchema)
}).superRefine((val: CreateUserFormProps, ctx: RefinementCtx) => {
  if (val.password !== val.rePassword) {
    const mismatch = {
      code: 'custom' as const,
      message: 'Passwords do not match'
    }
    
    ctx.addIssue({ ...mismatch, path: ['password'] })
    ctx.addIssue({ ...mismatch, path: ['rePassword'] })
  }

  if (!val.isGlobalAdmin && (val.entityScopes?.length ?? 0) === 0) {
    ctx.addIssue({
      code: 'custom',
      message: 'At least one entity scope is required for non-global admin users',
      path: ['entityScopes']
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

  const { identity } = useAppSelector(state => state.identity)

  const initialFormState = useMemo(createUserFormPropsProto, [])
  const validationSchema = useMemo(() => CreateUserFormSchema, [])

  const {
    formState,
    errors,
    formIsValid,
    handleInputChange,
    setInitialState
  } = useFormState<CreateUserFormProps>({
    initialState: initialFormState,
    validationSchema,
  })

  const handleSubmit = () => {
    if (!formIsValid) return

    const requestData: CreateUserRequest = {
      username: formState.username,
      email: formState.email,
      mobileNumber: formState.mobileNumber,
      password: formState.password,
      isGlobalAdmin: formState.isGlobalAdmin,
      entityScopes: formState.entityScopes?.map(entityScope => ({
        entityId: entityScope.entityId,
        entityType: entityScope.entityType,
        scope: entityScope.scope
      }))
    }

    const request = CreateUserRequestSchema.safeParse(requestData)

    if (!request.success) {
      request.error.issues.forEach(error => {
        addToast(error.message, 'error')
      })

      return
    }

    postData<CreateUserRequest, UserResponse>(GetApiRoute(ApiRoutes.identityPost).route, request.data)
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
            colspan={12}
            label={'Email'}
            value={formState.email}
            errorText={errors.email}
            onChange={(e) => handleInputChange('email', e.target.value)}
          />
          <TextBoxComponent
            colspan={12}
            label={'Mobile number'}
            value={formState.mobileNumber}
            errorText={errors.mobileNumber}
            onChange={(e) => handleInputChange('mobileNumber', e.target.value)}
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
          {identity.isGlobalAdmin && <><CheckBoxComponent

            colspan={12}
            label={'Is Global Admin'}
            value={formState.isGlobalAdmin || false}
            onChange={(e) => handleInputChange('isGlobalAdmin', e.target.checked)}
          />
          {!formState.isGlobalAdmin && <EditUserScopes
            allowIdentityAndApiKeyScopes={true}
            colspan={12}
            entityScopes={formState.entityScopes}
            emptyStateMessage={'At least one scope is required for non-global admin users.'}
            onChange={(entityScopes) => {

              if (!identity.isGlobalAdmin) {
                const allowedOrganizationIds = identity.acls
                  ?.filter(acl => acl.entityType === ScopeEntityType.Identity
                  && hasFlag(acl.scope, ScopePermission.Create))
                  .map(acl => acl.entityId) ?? []

                const selectedOrganizationIds = entityScopes
                  ?.map(scope => scope.entityId) ?? []

                if (selectedOrganizationIds.length > 0) {
                  const hasPermissionForAnySelected = selectedOrganizationIds
                    .some(id => allowedOrganizationIds.includes(id))

                  if (!hasPermissionForAnySelected) {
                    addToast(
                      'You do not have permission to create identities for one of the selected organizations.',
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
          </>}
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
  CreateUser
}
