// store/toastSlice.ts
import { createSlice, PayloadAction } from '@reduxjs/toolkit'

interface ToastState {
  message: string
  type: 'success' | 'error' | 'info' | 'warning'
}

const initialState: ToastState = {
  message: '',
  type: 'info'
}

const toastSlice = createSlice({
  name: 'toast',
  initialState,
  reducers: {
    showToast: (
      state,
      action: PayloadAction<{
        message: string
        type: 'success' | 'error' | 'info' | 'warning'
      }>
    ) => {
      state.message = action.payload.message
      state.type = action.payload.type
    },
    clearToast: (state) => {
      state.message = ''
      state.type = 'info'
    }
  }
})

export const { showToast, clearToast } = toastSlice.actions
export default toastSlice.reducer
