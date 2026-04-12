import { createSlice, PayloadAction, createAsyncThunk } from '@reduxjs/toolkit'
import { RootState } from '../store'

import { postData } from '../../axiosConfig'
import { LoginResponse } from '../../models/identity/login/LoginResponse'
import { LoginRequest } from '../../models/identity/login/LoginRequest'
import { ApiRoutes, GetApiRoute } from '../../AppMap'
import { LogoutRequest } from '../../models/identity/logout/LogoutRequest'
import { LogoutResponse } from '../../models/identity/logout/LogoutResponse'
import { readIdentity, removeIdentity, writeIdentity, normalizeLoginResponse } from '../../localStorage/identity'
import { RefreshTokenRequest } from '../../models/identity/login/RefreshTokenRequest'
import { jwtDecode } from 'jwt-decode'
import { Claims } from '../../models/identity/Claims'

interface Identity extends LoginResponse {
  userId?: string,
  username?: string
}

interface IdentityState {
  identity: Identity | null
  showUserOffcanvas: boolean
  status: 'idle' | 'loading' | 'failed'
  /** Indicates whether identity has been hydrated from localStorage at least once. */
  hydrated: boolean
}

const initialState: IdentityState = {
  identity: null,
  showUserOffcanvas: false,
  status: 'idle',
  hydrated: false,
}

const login = createAsyncThunk(
  'auth/login',
  async (requestData: LoginRequest) => {
    const apiRoute = GetApiRoute(ApiRoutes.identityLogin)
    const response = await postData<LoginRequest, LoginResponse>(apiRoute.route, requestData)
    return response
  }
)

const logout = createAsyncThunk(
  'auth/logout',
  async (logOutFromAllDevices: boolean = false) => {
    const identity = readIdentity()
    if (!identity || new Date(identity.refreshTokenExpiresAt) < new Date())
      return

    const apiRoute = GetApiRoute(ApiRoutes.identityLogout)
    const response = await postData<LogoutRequest, LogoutResponse>(apiRoute.route, {
      logOutFromAllDevices,
      token: identity.token
    })
    return response
  }
)

const refreshJwt = createAsyncThunk(
  'auth/refreshJwt',
  async (force?: boolean) => {
    const identity = readIdentity()
    if (!identity || new Date(identity.refreshTokenExpiresAt) < new Date())
      return

    const apiRoute = GetApiRoute(ApiRoutes.identityRefresh)
    const response = await postData<RefreshTokenRequest, LoginResponse>(apiRoute.route, {
      refreshToken: identity.refreshToken,
      force
    })

    return response
  }
)

const enrichStateWithJwtContent = (token: string, identity: Identity) => {
  let jwtContent: Record<string, unknown>
  try {
    jwtContent = jwtDecode(token) as Record<string, unknown>
  } catch {
    return
  }

  if (jwtContent) {
    if (jwtContent[Claims.NameIdentifier])
      identity.userId = jwtContent[Claims.NameIdentifier] as string

    // Keep the original username: prefer username from login/refresh response, then JWT claims (do not replace with a display name like "Organization Admin" from the name claim)
    if (identity.username == null || identity.username?.trim() === '') {
      const nameClaim = jwtContent[Claims.Name] as string | undefined
      const usernameClaim = (jwtContent['username'] ?? jwtContent['preferred_username']) as string | undefined
      identity.username = (usernameClaim?.trim()) ? usernameClaim : nameClaim
    }
  }

  console.log('Enriched identity:', identity)
}

const identitySlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    setIdentityFromLocalStorage: (state) => {
      const identity = readIdentity()

      if (identity) {
        state.identity = {
          ...identity
        }
        enrichStateWithJwtContent(identity.token, state.identity)
      }

      state.hydrated = true
    },
    setShowUserOffcanvas: (state) => {
      state.showUserOffcanvas = true
    },
    setHideUserOffcanvas: (state) => {
      state.showUserOffcanvas = false
    },
    /** Clears identity from state and localStorage (e.g. after refresh failed with 401). Does not call logout API. */
    clearIdentity: (state) => {
      state.identity = null
      state.showUserOffcanvas = false
      state.status = 'idle'
      removeIdentity()
    }
  },
  extraReducers: (builder) => {
    builder

      // Login
      .addCase(login.pending, (state) => {
        state.status = 'loading'
      })
      .addCase(login.fulfilled, (state, action: PayloadAction<LoginResponse | undefined>) => {
        state.status = 'idle'
        const normalized = normalizeLoginResponse(action.payload)
        if (normalized) {
          state.identity = {
            ...normalized
          }
          writeIdentity(normalized)

          enrichStateWithJwtContent(normalized.token, state.identity)
        }
      })
      .addCase(login.rejected, (state) => {
        state.status = 'failed'
      })

      // Logout
      .addCase(logout.pending, (state) => {
        state.status = 'loading'
      })
      .addCase(logout.fulfilled, (state, _: PayloadAction<LogoutResponse | undefined>) => {
        state.status = 'idle'

        state.identity = null
        state.showUserOffcanvas = false
        removeIdentity()
      })
      .addCase(logout.rejected, (state) => {
        state.status = 'failed'
      })

      // Refresh token
      .addCase(refreshJwt.pending, (state) => {
        state.status = 'loading'
      })
      .addCase(refreshJwt.fulfilled, (state, action: PayloadAction<LoginResponse | undefined>) => {
        state.status = 'idle'

        const normalized = normalizeLoginResponse(action.payload)
        if (normalized) {
          state.identity = {
            ...normalized
          }
          writeIdentity(normalized)

          enrichStateWithJwtContent(normalized.token, state.identity)
        }
        else {
          // Refresh API returned error (e.g. 401 Invalid refresh token); treat as logged out
          state.identity = null
          state.showUserOffcanvas = false
          removeIdentity()
        }
      })
      .addCase(refreshJwt.rejected, (state) => {
        state.status = 'idle'
        state.identity = null
        state.showUserOffcanvas = false
        removeIdentity()
      })
  },
})

export { login, logout, refreshJwt }
export const { setIdentityFromLocalStorage, setShowUserOffcanvas, setHideUserOffcanvas, clearIdentity } = identitySlice.actions
export const selectIdentity = (state: RootState) => state

export default identitySlice.reducer
