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
}

const initialState: IdentityState = {
  identity: null,
  showUserOffcanvas: false,
  status: 'idle',
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
    const apiRoute = GetApiRoute(ApiRoutes.identityLogout)
    const response = await postData<LogoutRequest, LogoutResponse>(apiRoute.route, {
      logOutFromAllDevices
    })
    return response
  }
)

const refreshJwt = createAsyncThunk(
  'auth/refreshJwt',
  async () => {
    const identity = readIdentity()
    if (!identity || new Date(identity.refreshTokenExpiresAt) < new Date())
      return

    const apiRoute = GetApiRoute(ApiRoutes.identityRefresh)
    const response = await postData<RefreshTokenRequest, LoginResponse>(apiRoute.route, {
      refreshToken: identity.refreshToken
    })

    return response
  }
)

const enrichStateWithJwtContent = (token: string, identity: Identity) => {
  const jwtContent = jwtDecode(token) as never

  if (jwtContent) {
    if (jwtContent[Claims.NameIdentifier])
      identity.userId = jwtContent[Claims.NameIdentifier]

    if (jwtContent[Claims.Name])
      identity.username = jwtContent[Claims.Name]

    if (jwtContent[Claims.Role]) {

      const appKnownRoles = enumToArr(Role)?.map(item => {
        return {
          value: item.value,
          label: item.displayValue
        }
      })

      const jwtRoles: string[] = Array.isArray(jwtContent[Claims.Role])
        ? jwtContent[Claims.Role]
        : jwtContent[Claims.Role]
          ? [jwtContent[Claims.Role]]
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
        ? jwtContent[Claims.AclEntry]
        : jwtContent[Claims.AclEntry]
          ? [jwtContent[Claims.AclEntry]]
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
          isGlobalAdmin: false,
          ...identity
        }
        enrichStateWithJwtContent(identity.token, state.identity)
      }
    },
    setShowUserOffcanvas: (state) => {
      state.showUserOffcanvas = true
    },
    setHideUserOffcanvas: (state) => {
      state.showUserOffcanvas = false
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
        if (action.payload) {
          state.identity = { 
            isGlobalAdmin: false,
            ...action.payload
          }
          writeIdentity(action.payload)

          enrichStateWithJwtContent(action.payload.token, state.identity)
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
        
        if (action.payload) {
          state.identity = {
            isGlobalAdmin: false,
            ...action.payload
          }
          writeIdentity(action.payload)

          enrichStateWithJwtContent(action.payload.token, state.identity)
        }
        else {
          state.identity = null
          removeIdentity()
        }
      })
      .addCase(refreshJwt.rejected, (state) => {
        state.status = 'failed'

        state.identity = null
        removeIdentity()
      })
  },
})

export { login, logout, refreshJwt }
export const { setIdentityFromLocalStorage, setShowUserOffcanvas, setHideUserOffcanvas } = identitySlice.actions
export const selectIdentity = (state: RootState) => state

export default identitySlice.reducer