import { readIdentity } from '@maks-it.com/webui-core'
import { store } from './redux/store'
import { clearIdentity, refreshJwt } from './redux/slices/identitySlice'

let isRefreshing = false
let refreshPromise: Promise<unknown> | null = null

/** Single in-flight refresh for axios (and SignalR when an app adds hubs). */
export const refreshWebUiAccessToken = async (): Promise<void> => {
  if (!isRefreshing) {
    isRefreshing = true
    refreshPromise = store.dispatch(refreshJwt()).finally(() => {
      isRefreshing = false
    })
  }
  await refreshPromise
}

/** Access token for hub connect/reconnect — same expiry rules as axios request interceptor. */
export const resolveWebUiAccessToken = async (): Promise<string> => {
  let identity = readIdentity()
  if (!identity)
    return ''

  const now = new Date()

  if (new Date(identity.expiresAt) < now) {
    if (new Date(identity.refreshTokenExpiresAt) <= now)
      return ''

    try {
      await refreshWebUiAccessToken()
      identity = readIdentity()
      if (!identity) {
        store.dispatch(clearIdentity())
        return ''
      }
      return identity.token
    } catch {
      store.dispatch(clearIdentity())
      return ''
    }
  }

  return identity.token
}
