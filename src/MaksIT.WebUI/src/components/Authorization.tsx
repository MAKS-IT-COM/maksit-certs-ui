import { FC, useEffect, useMemo } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { useAppDispatch, useAppSelector } from '../redux/hooks'
import { setIdentityFromLocalStorage } from '../redux/slices/identitySlice'

interface AuthorizationProps {
  children: React.ReactNode
}

const Authorization: FC<AuthorizationProps> = ({ children }) => {
  const dispatch = useAppDispatch()
  const { identity, hydrated } = useAppSelector((s) => s.identity)

  const navigate = useNavigate()
  const location = useLocation()

  const isTokenExpired = useMemo(() => {
    if (!identity?.refreshTokenExpiresAt)
      return true

    return new Date(identity.refreshTokenExpiresAt) < new Date()
  }, [identity])

  useEffect(() => {
    if (!hydrated)
      dispatch(setIdentityFromLocalStorage())
  }, [dispatch, hydrated])

  useEffect(() => {
    if (!hydrated)
      return

    if (isTokenExpired) {
      navigate('/login', { replace: true, state: { from: location } })
    }
  }, [hydrated, isTokenExpired, navigate, location])

  if (!hydrated)
    return null

  return !isTokenExpired ? children : null
}

export { Authorization }
