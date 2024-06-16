// components/Toast.tsx
import React, { useEffect } from 'react'
import { ToastContainer, toast } from 'react-toastify'
import 'react-toastify/dist/ReactToastify.css'
import { useDispatch, useSelector } from 'react-redux'
import { RootState } from '@/redux/store'
import { clearToast } from '@/redux/slices/toastSlice'

const Toast = () => {
  const dispatch = useDispatch()
  const toastState = useSelector((state: RootState) => state.toast)

  useEffect(() => {
    if (toastState.message) {
      switch (toastState.type) {
        case 'success':
          toast.success(toastState.message)
          break
        case 'error':
          toast.error(toastState.message)
          break
        case 'info':
          toast.info(toastState.message)
          break
        case 'warning':
          toast.warn(toastState.message)
          break
        default:
          toast(toastState.message)
          break
      }
      dispatch(clearToast())
    }
  }, [toastState, dispatch])

  return (
    <ToastContainer 
      position="bottom-right" 
      theme="dark"
      autoClose={5000}
      hideProgressBar={false}
      newestOnTop={false}
      closeOnClick
      rtl={false}
      pauseOnFocusLoss
      draggable
      pauseOnHover
    />
  )
}

export { Toast }
