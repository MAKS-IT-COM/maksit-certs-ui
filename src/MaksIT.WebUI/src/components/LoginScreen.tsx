import React, { FC, useEffect, useState } from 'react'
import { LoginRequest, LoginRequestSchema } from '../models/identity/login/LoginRequest'
import { useAppDispatch, useAppSelector } from '../redux/hooks'
import { login } from '../redux/slices/identitySlice'
import { useFormState } from '../hooks/useFormState'
import { useNavigate } from 'react-router-dom'
import { ButtonComponent, CheckBoxComponent, TextBoxComponent } from './editors'

const LoginScreen: FC = () => {
  const [use2FA, setUse2FA] = useState(false)
  const [use2FARecovery, setUse2FARecovery] = useState(false)

  const navigate = useNavigate()
  const dispatch = useAppDispatch()
  const { identity } = useAppSelector((state) => state.identity)

  const {
    formState,
    errors,
    formIsValid,
    handleInputChange
  } = useFormState<LoginRequest>({
    initialState: { username: '', password: '' },
    validationSchema: LoginRequestSchema,
  })

  useEffect(() => {
    if (!identity || new Date(identity.refreshTokenExpiresAt) < new Date()) {
      navigate('/login', { replace: true })
    } else {
      navigate('/', { replace: true })
    }
  }, [identity, navigate])

  const handleUse2FA = (e: React.ChangeEvent<HTMLInputElement>) => {
    setUse2FA(e.target.checked)
    if (!e.target.checked) {
      setUse2FARecovery(false)
    }
  }

  const handleLogin = () => {
    if (!formIsValid) return

    if (formState.twoFactorCode === '') delete formState.twoFactorCode
    if (formState.twoFactorRecoveryCode === '') delete formState.twoFactorRecoveryCode

    dispatch(login(formState))
  }

  return (
    <div className={'flex items-center justify-center min-h-screen bg-gray-100'}>
      <div className={'w-full max-w-md bg-white rounded-lg shadow-md p-8 space-y-6'}>
        {/* Logo */}
        <div className={'flex justify-center'}>
          <img src={'/logo.png'} alt={'Logo'} className={'h-12 w-auto'} />
        </div>

        {/* Form */}
        <div className={'space-y-4'}>
          <div className={'space-y-4'}>
            <TextBoxComponent
              label={'Username'}
              placeholder={'Username...'}
              value={formState.username}
              onChange={(e) => handleInputChange('username', e.target.value)}
              errorText={errors.username}
            />

            <TextBoxComponent
              label={'Password'}
              placeholder={'Password...'}
              type={'password'}
              value={formState.password}
              onChange={(e) => handleInputChange('password', e.target.value)}
              errorText={errors.password}
            />
          </div>

          {/* 2FA Options */}
          <div className={'flex items-center gap-4'}>
            <CheckBoxComponent
              label={'Use 2FA'}
              value={use2FA}
              onChange={handleUse2FA}
            />
            {use2FA && (
              <CheckBoxComponent
                label={'Use 2FA Recovery'}
                value={use2FARecovery}
                onChange={(e) => setUse2FARecovery(e.target.checked)}
              />
            )}
          </div>

          {/* 2FA Inputs */}
          {use2FA && (
            <div className={'space-y-4'}>
              {use2FARecovery ? (
                <TextBoxComponent
                  label={'2FA Recovery Code'}
                  placeholder={'Recovery code...'}
                  value={formState.twoFactorRecoveryCode}
                  onChange={(e) => handleInputChange('twoFactorRecoveryCode', e.target.value)}
                  errorText={errors.twoFactorRecoveryCode}
                />
              ) : (
                <TextBoxComponent
                  label={'2FA Code'}
                  placeholder={'Authentication code...'}
                  value={formState.twoFactorCode}
                  onChange={(e) => handleInputChange('twoFactorCode', e.target.value)}
                  errorText={errors.twoFactorCode}
                />
              )}
            </div>
          )}

          {/* Submit */}
          <ButtonComponent
            label={'Sign in'}
            buttonHierarchy={'primary'}
            onClick={handleLogin}
          />
        </div>
      </div>
    </div>
  )
}

export { LoginScreen }
