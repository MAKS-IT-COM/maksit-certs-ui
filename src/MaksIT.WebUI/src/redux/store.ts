import { configureStore } from '@reduxjs/toolkit'
import loaderReducer from './slices/loaderSlice'
import identityReducer from './slices/identitySlice'

export const store = configureStore({
  reducer: {
    loader: loaderReducer,
    //identity: identityReducer,
  },
})

// Infer the `RootState` and `AppDispatch` types from the store itself
export type RootState = ReturnType<typeof store.getState>
// Inferred type: {posts: PostsState, comments: CommentsState, users: UsersState}
export type AppDispatch = typeof store.dispatch