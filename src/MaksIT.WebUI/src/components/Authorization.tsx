import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAppDispatch, useAppSelector } from '../redux/hooks'
import { setIdentityFromLocalStorage } from '../redux/slices/identitySlice'

interface AuthorizationProps {
  children: React.ReactNode;
}

const Authorization = (props: AuthorizationProps) => {
  const { children } = props

  const navigate = useNavigate()
  const dispatch = useAppDispatch()
  const { identity } = useAppSelector((state) => state.identity)

  const [loading, setLoading] = useState(true)

  useEffect(() => {
    dispatch(setIdentityFromLocalStorage())
    setLoading(false)
  }, [dispatch])

  useEffect(() => {
    if (!loading) {
      if (!identity || new Date(identity.refreshTokenExpiresAt) < new Date()) {
        navigate('/login', { replace: true })
      }
    }
  }, [identity, navigate, loading])

  // Render a simple loading spinner while loading (Tailwind v4 compatible)
  if (loading) {
    return (
      <div className={'flex items-center justify-center h-screen w-screen bg-white'}>
        <div className={'animate-spin rounded-full h-8 w-8 border-t-2 border-b-2 border-blue-500'}></div>
      </div>
    )
  }

  if (!identity || new Date(identity.refreshTokenExpiresAt) < new Date()) {
    return null
  }

  return <>{children}</>
}

export {
  Authorization
}