import { configureStore } from '@reduxjs/toolkit'
import {
  TypedUseSelectorHook,
  useDispatch,
  useSelector,
  useStore
} from 'react-redux'
import loaderReducer from '@/redux/slices/loaderSlice'
import toastReducer from '@/redux/slices/toastSlice'

export const store = configureStore({
  reducer: {
    loader: loaderReducer,
    toast: toastReducer
  }
})

export type RootState = ReturnType<typeof store.getState>
export type AppDispatch = typeof store.dispatch
export type AppStore = typeof store

// Use throughout your app instead of plain `useDispatch` and `useSelector`
export const useAppDispatch = () => useDispatch<AppDispatch>()
export const useAppSelector: TypedUseSelectorHook<RootState> = useSelector
export const useAppStore = () => useStore<AppStore>()
