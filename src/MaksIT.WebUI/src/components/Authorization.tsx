import { FC, useEffect, useMemo } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { useAppDispatch, useAppSelector } from '../redux/hooks'
import { setIdentityFromLocalStorage } from '../redux/slices/identitySlice'

interface AuthorizationProps {
  children: React.ReactNode;
}

const Authorization: FC<AuthorizationProps> = (props) => {
  const { children } = props

  const navigate = useNavigate()
  const location = useLocation()
  const dispatch = useAppDispatch()
  const { identity } = useAppSelector((state) => state.identity)

  const isTokenExpired = useMemo(() => {
    if (!identity || !identity.refreshTokenExpiresAt)
      return true

    return new Date(identity.refreshTokenExpiresAt) < new Date()
  }, [identity])

  useEffect(() => {
    // Load identity from local storage on mount
    dispatch(setIdentityFromLocalStorage())
  }, [dispatch])

  useEffect(() => {
    if (isTokenExpired) {
      // Optionally, pass the current location for redirect after login
      navigate('/login', { replace: true, state: { from: location } })
    }
  }, [isTokenExpired, navigate, location])

  return !isTokenExpired
    ? children
    : <></>
}

export { Authorization }