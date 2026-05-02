import { FC, useCallback, useEffect, useState } from 'react'
import { getData, patchData } from '../../../axiosConfig'
import { ApiRoutes, GetApiRoute } from '../../../AppMap'
import { UserResponse } from '../../../models/identity/user/UserResponse'
import { useFormState } from '../../../hooks/useFormState'
import { PatchUserEnableTwoFactorRequest, PatchUserRequest } from '../../../models/identity/user/PatchUserRequest'
import type { PatchUserEntityScopeRequest } from '../../../models/identity/user/PatchUserEntityScopeRequest'
import { ButtonComponent, CheckBoxComponent, TextBoxComponent } from '../../../components/editors'
import { ChangePassword } from './ChangePassword'
import { EnableTwoFactor } from './EnableTwoFactor'
import { FormContent, FormContainer, FormFooter, FormHeader } from '../../../components/FormLayout'
import { useAppDispatch, useAppSelector } from '../../../redux/hooks'
import { refreshJwt } from '../../../redux/slices/identitySlice'
import { addToast } from '../../../components/Toast/addToast'
import { EditUserScopes, EntityScopeFormProps, EntityScopeFormPropsSchema } from '../../shared/EditScopes'
import z, { array, boolean, object, ZodType, string } from 'zod'
import { deepCopy, deepDelta, deltaHasOperations, ENTITY_SCOPES_ARRAY_POLICY } from '../../../functions'


// Form state interface and validation
interface EditUserFormProps {
  [key: string]: string | boolean | EntityScopeFormProps[] | undefined

  id: string
  username?: string
  email?: string
  mobileNumber?: string
  isActive: boolean
  isGlobalAdmin: boolean
  entityScopes: EntityScopeFormProps[]
}

const editUsertFormPropsProto = (): EditUserFormProps => ({
  id: '',
  username: '',
  email: '',
  mobileNumber: '',
  isActive: false,
  isGlobalAdmin: false,
  entityScopes: []
})

const EditUsertFormSchema: ZodType<EditUserFormProps> = object({
  id: string().min(1),
  username: string().min(1).optional(),
  email: z.email().optional(),
  mobileNumber: string().min(1).optional(),
  isActive: boolean(),
  isGlobalAdmin: boolean(),
  entityScopes: array(EntityScopeFormPropsSchema)
})

// Component props interface
interface EditUserProps {
  userId: string;
  onSubmitted?: (entity: UserResponse) => void
  cancelEnabled?: boolean
  onCancel?: () => void
}

const EditUser: FC<EditUserProps> = (props) => {
  const {
    userId,
    onSubmitted,
    cancelEnabled = false,
    onCancel
  } = props

  const dispatch = useAppDispatch()
  const { identity } = useAppSelector((state) => state.identity)

  const [showChangePassword, setShowChangePassword] = useState(false)
  const [showEnableTwoFactor, setShowEnableTwoFactor] = useState(false)

  const [qrCodeUrl, setQrCodeUrl] = useState<string | undefined>(undefined)
  const [recoveryCodes, setRecoveryCodes] = useState<string[] | undefined>(undefined)
  const [recoveryCodesLeft, setRecoveryCodesLeft] = useState<number | undefined>(undefined)

  const [twoFactorEnabled, setTwoFactorEnabled] = useState<boolean>(false)

  const [hasLoaded, setHasLoaded] = useState(false)

  const initialFormState = editUsertFormPropsProto()

  const {
    formState,
    errors,
    formIsValid,
    handleInputChange,
    setInitialState
  } = useFormState<EditUserFormProps>({
    initialState: initialFormState,
    validationSchema: EditUsertFormSchema,
  })

  const isEditingSelf =
    !!identity &&
    (
      identity.userId === userId ||
      identity.userId === formState.id ||
      (!!identity.username && identity.username === formState.username)
    )

  const [backupState, setBackupState] = useState<EditUserFormProps>(initialFormState)

  const handleInitialization = useCallback((response: UserResponse) => {

    if (response.twoFactorEnabled !== undefined)
      setTwoFactorEnabled(response.twoFactorEnabled)

    setRecoveryCodesLeft(response.recoveryCodesLeft)

    const newState: EditUserFormProps = {
      id: response.id,
      username: response.username,
      email: response.email ?? '',
      mobileNumber: response.mobileNumber ?? '',
      isActive: response.isActive,
      isGlobalAdmin: response.isGlobalAdmin ?? false,
      entityScopes: response.entityScopes?.map(scope => ({
        id: scope.id,
        entityId: scope.entityId,
        entityType: scope.entityType,
        scope: scope.scope
      })) ?? []
    }

    setInitialState(newState)
    // Backup = last server state; patchable array items (e.g. entityScopes) always have id
    setBackupState(deepCopy(newState))

  }, [setInitialState])

  useEffect(() => {
    if (!userId) {
      setHasLoaded(true)
      return
    }

    getData<UserResponse>(GetApiRoute(ApiRoutes.identityGet).route.replace('{userId}', userId))
      .then((response) => {
        if (!response.ok || !response.payload) return

        handleInitialization(response.payload)
      })
      .finally(() => {
        setHasLoaded(true)
      })
  }, [userId, handleInitialization])

  const mapFormStateToPatchRequest = (formState: EditUserFormProps): PatchUserRequest => {
    const formStateCopy = deepCopy(formState)

    const patchReqst: PatchUserRequest = {
      username: formStateCopy.username,
      email: formStateCopy.email,
      mobileNumber: formStateCopy.mobileNumber,
      entityScopes: formStateCopy.entityScopes?.map(scope => ({
        id: scope.id,
        entityId: scope.entityId,
        entityType: scope.entityType,
        scope: scope.scope,
      }))
    }

    if (!isEditingSelf) {
      patchReqst.isActive = formStateCopy.isActive
      patchReqst.isGlobalAdmin = formStateCopy.isGlobalAdmin
    }

    return patchReqst
  }

  const handleSubmit = () => {
    if (!formIsValid) return

    const fromFormState = mapFormStateToPatchRequest(formState)
    const fromBackupState = mapFormStateToPatchRequest(backupState)

    const delta = deepDelta(fromFormState, fromBackupState, { arrays: { entityScopes: ENTITY_SCOPES_ARRAY_POLICY } })

    // Don't send entityScopes when the delta only contains items with id (no Add/Remove, no scope fields).
    // That happens when only other fields (e.g. isActive) changed; sending id-only scope items would make the backend overwrite scopes.
    if (Array.isArray(delta.entityScopes) && delta.entityScopes.length > 0) {
      const collectionOpKey = 'collectionItemOperation' as const
      const onlyIdItems = delta.entityScopes.every(
        (item: PatchUserEntityScopeRequest) =>
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

    patchData<PatchUserRequest, UserResponse>(GetApiRoute(ApiRoutes.identityPatch).route.replace('{userId}', userId), delta)
      .then((response) => {
        if (!response.ok || !response.payload) return

        handleInitialization(response.payload)

        void dispatch(refreshJwt(true))

        onSubmitted?.(response.payload)
      })
  }

  const handleCancel = () => {
    onCancel?.()
  }

  if (!hasLoaded) {
    return <>
      <FormContainer>
        <FormHeader>Edit user</FormHeader>
        <FormContent>
          <p>Loading user...</p>
        </FormContent>
      </FormContainer>
    </>
  }

  if (!formState.id) {
    return <>
      <FormContainer>
        <FormHeader>Edit user</FormHeader>
        <FormContent>
          <p>User not found or not accessible.</p>
        </FormContent>
        <FormFooter
          rightChildren={cancelEnabled && <ButtonComponent label={'Back'} buttonHierarchy={'secondary'} onClick={handleCancel} />}
        />
      </FormContainer>
    </>
  }

  return <>
    <FormContainer>
      <FormHeader>Edit user</FormHeader>
      <FormContent>
        <div className={'grid grid-cols-12 gap-4 w-full'}>
          <TextBoxComponent
            colspan={12}
            label={'Id'}
            value={formState.id}
            errorText={errors.id}
            onChange={(e) => handleInputChange('id', e.target.value)}
            readOnly={true}
          />
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
          <EditUserScopes
            allowIdentityAndApiKeyScopes={true}
            colspan={12}
            id={formState.id}
            entityScopes={formState.entityScopes}
            onChange={(entityScopes) => handleInputChange('entityScopes', entityScopes)}
          />
          <CheckBoxComponent
            colspan={12}
            label={'Is active'}
            value={formState.isActive}
            onChange={(e) => handleInputChange('isActive', e.target.checked)}
            disabled={isEditingSelf}
          />
          <CheckBoxComponent
            colspan={12}
            label={'Is Global Admin'}
            value={formState.isGlobalAdmin}
            onChange={(e) => handleInputChange('isGlobalAdmin', e.target.checked)}
            disabled={isEditingSelf}
          />

          <CheckBoxComponent
            colspan={12}
            label={`Two factor enabled ${twoFactorEnabled ? `(Recovery codes left: ${recoveryCodesLeft})` : '' }`}
            value={twoFactorEnabled}
            onChange={(e) => {
              if (e.target.checked) {
                patchData<PatchUserEnableTwoFactorRequest, UserResponse>(GetApiRoute(ApiRoutes.identityPatch).route.replace('{userId}', formState.id), {
                  twoFactorEnabled: true
                }).then((response) => {
                  if (!response.ok || !response.payload) return

                  setShowEnableTwoFactor(true)

                  setTwoFactorEnabled(!!response.payload.twoFactorEnabled)
                  setQrCodeUrl(response.payload.qrCodeUrl)
                  setRecoveryCodes(response.payload.twoFactorRecoveryCodes)
                })
              }
              else {
                patchData<PatchUserEnableTwoFactorRequest, UserResponse>(GetApiRoute(ApiRoutes.identityPatch).route.replace('{userId}', formState.id), {
                  twoFactorEnabled: false
                }).then((response) => {
                  if (!response.ok || !response.payload) return

                  setTwoFactorEnabled(!!response.payload.twoFactorEnabled)
                  setQrCodeUrl(undefined)
                  setRecoveryCodes(undefined)
                })
              }
            }}
          />
          <ButtonComponent
            colspan={4}
            label={'Change password'}
            buttonHierarchy={'primary'}
            onClick={() => setShowChangePassword(true)}
          />
          <span className={'col-span-8'}></span>
        </div>
      </FormContent>
      <FormFooter
        rightChildren={<ButtonComponent label={'Save'} buttonHierarchy={'primary'} onClick={handleSubmit} />}
        leftChildren={cancelEnabled && <ButtonComponent label={'Cancel'} buttonHierarchy={'secondary'} onClick={handleCancel} />}
      />
    </FormContainer>
    <ChangePassword
      userId={userId}
      isOpen={showChangePassword}
      onClose={() => setShowChangePassword(false)}
    />
    <EnableTwoFactor
      isOpen={showEnableTwoFactor}
      qrCodeUrl={qrCodeUrl}
      twoFactorRecoveryCodes={recoveryCodes}
      onClose={() => setShowEnableTwoFactor(false)}
    />
  </>
}

export {
  EditUser
}
