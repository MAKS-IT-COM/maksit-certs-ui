// components/Loader.tsx
import React, { useEffect } from 'react'
import { useSelector, useDispatch } from 'react-redux'
import { RootState } from '@/redux/store'
import { reset } from '@/redux/slices/loaderSlice'
import './loader.css'

const Loader: React.FC = () => {
  const dispatch = useDispatch()
  const activeRequests = useSelector((state: RootState) => state.loader.activeRequests)

  useEffect(() => {
    let timeout: NodeJS.Timeout | null = null
    if (activeRequests > 0) {
      timeout = setTimeout(() => {
        dispatch(reset())
      }, 10000) // Adjust the timeout as necessary
    }

    return () => {
      if (timeout) {
        clearTimeout(timeout)
      }
    }
  }, [activeRequests, dispatch])

  if (activeRequests === 0) {
    return null
  }

  return (
    <div className="loader-overlay">
      <div className="spinner"></div>
      <div className="loading-text">Loading...</div>
    </div>
  )
}

export {
  Loader
}
