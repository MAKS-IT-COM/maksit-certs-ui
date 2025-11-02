import { createSlice } from '@reduxjs/toolkit'

const initialState = {
  loading: false,
  disabled: false,
  counter: 0
}

const loaderSlice = createSlice({
  name: 'loader',
  initialState,
  reducers: {
    showLoader: state => {
      if(state.disabled) {
        state.counter = 0
        state.loading = false
      }
      else {
        state.counter++
        state.loading = true
      }
    },
    hideLoader: state => {
      if (state.disabled || state.counter === 0) return

      state.counter--
      
      if (state.counter === 0)
        state.loading = false
    },
    enableLoader: state => {
      state.disabled = false
    },
    disableLoader: state => {
      state.disabled = true
      state.loading = false
      state.counter = 0
    }
  }
})

export const { showLoader, hideLoader, enableLoader, disableLoader } = loaderSlice.actions
export default loaderSlice.reducer