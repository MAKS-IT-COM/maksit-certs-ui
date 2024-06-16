import { configureStore } from '@reduxjs/toolkit'
import loaderReducer from '@/redux/slices//loaderSlice'
import toastReducer from '@/redux/slices/toastSlice'

export const store = configureStore({
    reducer: {
        loader: loaderReducer,
        toast: toastReducer,
    },
})

export type RootState = ReturnType<typeof store.getState>
export type AppDispatch = typeof store.dispatch