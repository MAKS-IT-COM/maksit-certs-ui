// components/Toast.tsx
import React, { useEffect } from 'react'
import { ToastContainer, toast } from 'react-toastify'
import 'react-toastify/dist/ReactToastify.css'
import { useDispatch, useSelector } from 'react-redux'
import { RootState } from '@/redux/store'
import { clearToast, removeToast } from '@/redux/slices/toastSlice'

const Toast = () => {
  const dispatch = useDispatch()
  const toastState = useSelector((state: RootState) => state.toast)

  useEffect(() => {
    toastState.messages.forEach((toastMessage) => {
      switch (toastMessage.type) {
        case 'success':
          toast.success(toastMessage.message)
          break
        case 'error':
          toast.error(toastMessage.message)
          break
        case 'info':
          toast.info(toastMessage.message)
          break
        case 'warning':
          toast.warn(toastMessage.message)
          break
        default:
          toast(toastMessage.message)
          break
      }

      dispatch(removeToast(toastMessage.id))
    })
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
