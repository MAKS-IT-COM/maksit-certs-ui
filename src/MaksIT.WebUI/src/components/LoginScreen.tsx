import React, { useEffect, KeyboardEvent } from 'react'
import { LoginRequest, LoginRequestSchema } from '../models/identity/login/LoginRequest'
import { useAppDispatch, useAppSelector } from '../redux/hooks'
import { login } from '../redux/slices/identitySlice'
import { useFormState } from '../hooks/useFormState'
import { useNavigate } from 'react-router-dom'
import { ButtonComponent, TextBoxComponent } from './editors'

const LoginScreen: React.FC = () => {
  /* Backend has no 2FA support yet — re-enable when API is ready.
  const [use2FA, setUse2FA] = useState(false)
  const [use2FARecovery, setUse2FARecovery] = useState(false)
  */

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

  /*
  const handleUse2FA = (e: React.ChangeEvent<HTMLInputElement>) => {
    setUse2FA(e.target.checked)
    if (!e.target.checked) {
      setUse2FARecovery(false)
    }
  }
  */

  const handleLogin = () => {
    if (!formIsValid) return

    const newFormState = { ...formState }

    if (newFormState.twoFactorCode === '') delete newFormState.twoFactorCode
    if (newFormState.twoFactorRecoveryCode === '') delete newFormState.twoFactorRecoveryCode

    dispatch(login(newFormState))
  }

  const handleSubmit = (e: KeyboardEvent<HTMLDivElement>) => {
    if (e.key === 'Enter') handleLogin()
  }

  return (
    <div className={'relative min-h-screen bg-gray-100 flex items-center justify-center'}>
      {/* Top left logo and company name */}
      <a
        href={import.meta.env.VITE_COMPANY_URL}
        target={'_blank'}
        rel={'noopener noreferrer'}
        className={'absolute top-6 left-8 flex items-center space-x-3 z-10'}
      >
        <img src={'/logo.png'} alt={'Logo'} className={'h-10 w-auto'} />
      </a>


      <div className={'w-full max-w-md bg-white rounded-lg shadow-md p-8 space-y-6'} onKeyDown={handleSubmit} tabIndex={0}>
        {/* App logo and name above form */}
        <div className={'flex justify-center items-center space-x-3 mb-2'}>
          <img src={'/certs-ui-logo-only.png'} alt={'CertsUI'} className={'h-12 w-auto'} />
          <span className={'text-2xl font-bold text-gray-800 select-none'}>{import.meta.env.VITE_APP_TITLE}</span>
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

          {/*
          Backend has no 2FA support yet — restore useState, handleUse2FA, CheckBox import, and this block when API is ready.

          <div className={'flex items-center gap-4'}>
            <CheckBoxComponent label={'Use 2FA'} value={use2FA} onChange={handleUse2FA} />
            {use2FA && (
              <CheckBoxComponent
                label={'Use 2FA Recovery'}
                value={use2FARecovery}
                onChange={(e) => setUse2FARecovery(e.target.checked)}
              />
            )}
          </div>
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
          */}

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
