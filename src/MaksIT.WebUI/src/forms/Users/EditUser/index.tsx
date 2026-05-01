import { FC, useCallback, useEffect, useState } from 'react'
import { getData, patchData } from '../../../axiosConfig'
import { ApiRoutes, GetApiRoute } from '../../../AppMap'
import { UserResponse } from '../../../models/identity/user/UserResponse'
import { FormContainer, FormContent, FormFooter, FormHeader } from '../../../components/FormLayout'
import { ButtonComponent, CheckBoxComponent } from '../../../components/editors'
import { ChangePassword } from './ChangePassword'
import { EnableTwoFactor } from './EnableTwoFactor'
import { formatISODateString } from '../../../functions'
import { PatchUserEnabeleTwoFactorRequest, PatchUserRequest } from '../../../models/identity/user/PatchUserRequest'
import { PatchOperation } from '../../../models/PatchOperation'
import { useAppSelector } from '../../../redux/hooks'

interface EditUserProps {
  userId: string
  onSubmitted?: (entity: UserResponse) => void
  cancelEnabled?: boolean
  onCancel?: () => void
}

const EditUser: FC<EditUserProps> = (props) => {
  const {
    userId,
    onSubmitted,
    cancelEnabled = false,
    onCancel,
  } = props

  const { identity } = useAppSelector((state) => state.identity)

  const [hasLoaded, setHasLoaded] = useState(false)
  const [user, setUser] = useState<UserResponse | null>(null)
  const [showChangePassword, setShowChangePassword] = useState(false)
  const [showEnableTwoFactor, setShowEnableTwoFactor] = useState(false)

  const [qrCodeUrl, setQrCodeUrl] = useState<string | undefined>(undefined)
  const [recoveryCodes, setRecoveryCodes] = useState<string[] | undefined>(undefined)
  const [recoveryCodesLeft, setRecoveryCodesLeft] = useState<number | undefined>(undefined)

  const [twoFactorEnabled, setTwoFactorEnabled] = useState(false)
  const [isActive, setIsActive] = useState(true)
  const [dirtyIsActive, setDirtyIsActive] = useState(false)

  const isEditingSelf =
    !!identity &&
    (identity.userId === userId || (!!identity.username && identity.username === user?.username))

  const handleLoad = useCallback(() => {
    getData<UserResponse>(GetApiRoute(ApiRoutes.identityGet).route.replace('{userId}', userId))
      .then((response) => {
        setUser(response.payload ?? null)
        if (response.ok && response.payload) {
          setTwoFactorEnabled(!!response.payload.twoFactorEnabled)
          setRecoveryCodesLeft(response.payload.recoveryCodesLeft)
          setIsActive(response.payload.isActive !== false)
          setDirtyIsActive(false)
        }
      })
      .finally(() => {
        setHasLoaded(true)
      })
  }, [userId])

  useEffect(() => {
    setHasLoaded(false)
    setUser(null)
    handleLoad()
  }, [handleLoad])

  const handleCancel = () => {
    onCancel?.()
  }

  const handlePasswordSubmitted = (updated: UserResponse) => {
    setUser(updated)
    onSubmitted?.(updated)
  }

  const handleSaveIsActive = () => {
    if (!user?.id) return
    const body: PatchUserRequest = {
      isActive,
      operations: {
        isActive: PatchOperation.SetField,
      },
    }
    patchData<PatchUserRequest, UserResponse>(
      GetApiRoute(ApiRoutes.identityPatch).route.replace('{userId}', userId),
      body
    ).then((response) => {
      if (!response.ok || !response.payload) return
      setUser(response.payload)
      setIsActive(response.payload.isActive !== false)
      setDirtyIsActive(false)
      onSubmitted?.(response.payload)
    })
  }

  if (!hasLoaded) {
    return (
      <FormContainer>
        <FormHeader>Edit user</FormHeader>
        <FormContent><p>Loading user…</p></FormContent>
      </FormContainer>
    )
  }

  if (!user?.id) {
    return (
      <FormContainer>
        <FormHeader>Edit user</FormHeader>
        <FormContent><p>User not found or not accessible.</p></FormContent>
        <FormFooter
          leftChildren={
            cancelEnabled ? (
              <ButtonComponent label={'Back'} buttonHierarchy={'secondary'} onClick={handleCancel} />
            ) : undefined
          }
        />
      </FormContainer>
    )
  }

  return (
    <>
      <FormContainer>
        <FormHeader>Edit user {userId}</FormHeader>
        <FormContent>
          <div className={'grid grid-cols-12 gap-4 w-full content-start'}>
            <p className={'col-span-12 text-sm text-neutral-600'}>
              Username: {user.username ?? '—'}
            </p>
            <p className={'col-span-12 text-sm text-neutral-600'}>
              Last login:{' '}
              {user.lastLogin ? formatISODateString(String(user.lastLogin)) : '—'}
            </p>
            <CheckBoxComponent
              colspan={12}
              label={'Is active'}
              value={isActive}
              onChange={(e) => {
                setIsActive(e.target.checked)
                setDirtyIsActive(true)
              }}
              disabled={isEditingSelf}
            />
            {dirtyIsActive && !isEditingSelf && (
              <ButtonComponent
                colspan={4}
                label={'Save active flag'}
                buttonHierarchy={'secondary'}
                onClick={() => handleSaveIsActive()}
              />
            )}
            <CheckBoxComponent
              colspan={12}
              label={`Two factor enabled${twoFactorEnabled ? ` (Recovery codes left: ${recoveryCodesLeft ?? '—'})` : ''}`}
              value={twoFactorEnabled}
              onChange={(e) => {
                if (e.target.checked) {
                  patchData<PatchUserEnabeleTwoFactorRequest, UserResponse>(
                    GetApiRoute(ApiRoutes.identityPatch).route.replace('{userId}', userId),
                    { twoFactorEnabled: true }
                  ).then((response) => {
                    if (!response.ok || !response.payload) return
                    setShowEnableTwoFactor(true)
                    setTwoFactorEnabled(!!response.payload.twoFactorEnabled)
                    setQrCodeUrl(response.payload.qrCodeUrl)
                    setRecoveryCodes(response.payload.twoFactorRecoveryCodes)
                    setRecoveryCodesLeft(response.payload.recoveryCodesLeft)
                    setUser(response.payload)
                    onSubmitted?.(response.payload)
                  })
                } else {
                  patchData<PatchUserEnabeleTwoFactorRequest, UserResponse>(
                    GetApiRoute(ApiRoutes.identityPatch).route.replace('{userId}', userId),
                    { twoFactorEnabled: false }
                  ).then((response) => {
                    if (!response.ok || !response.payload) return
                    setTwoFactorEnabled(!!response.payload.twoFactorEnabled)
                    setQrCodeUrl(undefined)
                    setRecoveryCodes(undefined)
                    setRecoveryCodesLeft(response.payload.recoveryCodesLeft)
                    setUser(response.payload)
                    onSubmitted?.(response.payload)
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
          </div>
        </FormContent>
        <FormFooter
          leftChildren={
            cancelEnabled ? (
              <ButtonComponent label={'Cancel'} buttonHierarchy={'secondary'} onClick={handleCancel} />
            ) : undefined
          }
        />
      </FormContainer>

      <ChangePassword
        userId={userId}
        isOpen={showChangePassword}
        onClose={() => setShowChangePassword(false)}
        onSubmitted={handlePasswordSubmitted}
      />
      <EnableTwoFactor
        isOpen={showEnableTwoFactor}
        qrCodeUrl={qrCodeUrl}
        twoFactorRecoveryCodes={recoveryCodes}
        onClose={() => setShowEnableTwoFactor(false)}
      />
    </>
  )
}

export { EditUser }
