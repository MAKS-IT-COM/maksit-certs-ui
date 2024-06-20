// loaderSlice.ts
import { createSlice, PayloadAction } from '@reduxjs/toolkit'

interface LoaderState {
  activeRequests: number
}

const initialState: LoaderState = {
  activeRequests: 0
}

const loaderSlice = createSlice({
  name: 'loader',
  initialState,
  reducers: {
    increment: (state) => {
      state.activeRequests += 1
    },
    decrement: (state) => {
      if (state.activeRequests > 0) {
        state.activeRequests -= 1
      }
    },
    reset: (state) => {
      state.activeRequests = 0
    }
  }
})

export const { increment, decrement, reset } = loaderSlice.actions
export default loaderSlice.reducer
