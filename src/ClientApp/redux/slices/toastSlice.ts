// store/toastSlice.ts
import { createSlice, PayloadAction } from '@reduxjs/toolkit'
import { v4 as uuidv4 } from 'uuid' // Assuming UUID is used for generating unique IDs

interface ToastMessage {
  id: string // Add an id field
  message: string
  type: 'success' | 'error' | 'info' | 'warning'
}

interface ToastState {
  messages: ToastMessage[]
}

const initialState: ToastState = {
  messages: []
}

const toastSlice = createSlice({
  name: 'toast',
  initialState,
  reducers: {
    showToast: (state, action: PayloadAction<Omit<ToastMessage, 'id'>>) => {
      // Generate a unique ID for each toast message
      const id = uuidv4()
      const newMessage = { ...action.payload, id }
      state.messages.push(newMessage)
    },
    clearToast: (state) => {
      state.messages = []
    },
    removeToast: (state, action: PayloadAction<string>) => {
      // Remove a specific toast message by ID
      state.messages = state.messages.filter(
        (message) => message.id !== action.payload
      )
    }
  }
})

export const { showToast, clearToast, removeToast } = toastSlice.actions
export default toastSlice.reducer
