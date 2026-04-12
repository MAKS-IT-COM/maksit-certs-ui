import { createSlice, PayloadAction, createAsyncThunk } from '@reduxjs/toolkit'
import { RootState } from '../store'

import { postData } from '../../axiosConfig'
import { LoginResponse } from '../../models/identity/login/LoginResponse'
import { LoginRequest } from '../../models/identity/login/LoginRequest'
import { ApiRoutes, GetApiRoute } from '../../AppMap'
import { LogoutRequest } from '../../models/identity/logout/LogoutRequest'
import { LogoutResponse } from '../../models/identity/logout/LogoutResponse'
import { readIdentity, removeIdentity, writeIdentity } from '../../localStorage/identity'
import { RefreshTokenRequest } from '../../models/identity/login/RefreshTokenRequest'
import { jwtDecode } from 'jwt-decode'
import { Claims } from '../../models/identity/Claims'
import { enumToArr, parseAclEntries } from '../../functions'
import { Role } from '../../models/identity/Role'
import { AclEntry } from '../../functions/acl/parseAclEntry'

interface IdentityRole {
  value: string | number,
  label: string
}

interface Identity extends LoginResponse {
  userId?: string,
  username?: string
  roles?: IdentityRole []
  isGlobalAdmin: boolean
  acls?: AclEntry []
}

interface IdentityState {
  identity: Identity | null
  showUserOffcanvas: boolean
  status: 'idle' | 'loading' | 'failed'
  /** Indicates whether identity has been hydrated from localStorage at least once. */
  hydrated: boolean
}

/** API JSON may be camelCase or PascalCase; normalize so Authorization and axios see stable keys. */
const normalizeLoginResponse = (raw: LoginResponse | undefined): LoginResponse | undefined => {
  if (!raw) return undefined
  const r = raw as Record<string, unknown>
  const str = (camel: keyof LoginResponse, pascal: string) => {
    const v = r[camel] ?? r[pascal]
    if (v == null) return ''
    return typeof v === 'string' ? v : String(v)
  }
  return {
    tokenType: str('tokenType', 'TokenType'),
    token: str('token', 'Token'),
    expiresAt: str('expiresAt', 'ExpiresAt'),
    refreshToken: str('refreshToken', 'RefreshToken'),
    refreshTokenExpiresAt: str('refreshTokenExpiresAt', 'RefreshTokenExpiresAt'),
  }
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
  const jwtContent = jwtDecode(token) as Record<string, unknown>

  if (jwtContent) {
    if (jwtContent[Claims.NameIdentifier])
      identity.userId = jwtContent[Claims.NameIdentifier] as string

    if (identity.username == null || identity.username?.trim() === '') {
      const nameClaim = jwtContent[Claims.Name] as string | undefined
      const usernameClaim = (jwtContent['username'] ?? jwtContent['preferred_username']) as string | undefined
      identity.username = (usernameClaim?.trim()) ? usernameClaim : nameClaim
    }

    if (jwtContent[Claims.Role]) {

      const appKnownRoles = enumToArr(Role)?.map(item => {
        return {
          value: item.value,
          label: item.displayValue
        }
      })

      const jwtRoles: string[] = Array.isArray(jwtContent[Claims.Role])
        ? jwtContent[Claims.Role] as string[]
        : jwtContent[Claims.Role]
          ? [jwtContent[Claims.Role] as string]
          : []

      const identityRoles: IdentityRole [] = []
      jwtRoles.forEach(identityRole => {
        const foundRole = appKnownRoles.find(role => role.label === identityRole)
        if (foundRole) {
          identityRoles.push(foundRole)
        }
      })

      identity.roles = identityRoles
    }

    if (jwtContent[Claims.AclEntry]) {
      const jwtAcls: string[] = Array.isArray(jwtContent[Claims.AclEntry])
        ? jwtContent[Claims.AclEntry] as string[]
        : jwtContent[Claims.AclEntry]
          ? [jwtContent[Claims.AclEntry] as string]
          : []

      if (jwtAcls?.includes('global:admin') ?? false) {
        jwtAcls.splice(jwtAcls.indexOf('global:admin'), 1)
        identity.isGlobalAdmin = true
      }
      else {
        identity.isGlobalAdmin = false
      }

      identity.acls = parseAclEntries(jwtAcls)
    }
  }
}

const identitySlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    setIdentityFromLocalStorage: (state) => {
      const raw = readIdentity()
      const identity = normalizeLoginResponse(raw)

      if (identity?.token && identity.refreshTokenExpiresAt) {
        writeIdentity(identity)
        state.identity = {
          isGlobalAdmin: false,
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
        const payload = normalizeLoginResponse(action.payload)
        if (payload?.token && payload.refreshTokenExpiresAt) {
          state.identity = {
            isGlobalAdmin: false,
            ...payload
          }
          writeIdentity(payload)

          enrichStateWithJwtContent(payload.token, state.identity)
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

        const payload = normalizeLoginResponse(action.payload)
        if (payload?.token && payload.refreshTokenExpiresAt) {
          state.identity = {
            isGlobalAdmin: false,
            ...payload
          }
          writeIdentity(payload)

          enrichStateWithJwtContent(payload.token, state.identity)
        }
        else {
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
