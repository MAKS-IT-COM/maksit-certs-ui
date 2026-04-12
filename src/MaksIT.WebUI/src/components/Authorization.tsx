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
  const { identity, hydrated } = useAppSelector((state) => state.identity)

  const isTokenExpired = useMemo(() => {
    if (!identity || !identity.refreshTokenExpiresAt)
      return true

    return new Date(identity.refreshTokenExpiresAt) < new Date()
  }, [identity])

  useEffect(() => {
    // Load identity from local storage on first mount
    if (!hydrated) {
      dispatch(setIdentityFromLocalStorage())
    }
  }, [dispatch, hydrated])

  useEffect(() => {
    if (!hydrated) return

    if (isTokenExpired) {
      navigate('/login', { replace: true, state: { from: location } })
    }
  }, [hydrated, isTokenExpired, navigate, location])

  if (!hydrated) {
    return <></>
  }

  return !isTokenExpired
    ? children
    : <></>
}

export { Authorization }
