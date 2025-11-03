import axios from 'axios'
import { readIdentity } from './localStorage/identity'
import { ApiRoutes, GetApiRoute } from './AppMap'
import { store } from './redux/store'
import { refreshJwt } from './redux/slices/identitySlice'
import { hideLoader, showLoader } from './redux/slices/loaderSlice'
import { addToast } from './components/Toast/addToast'
import { de } from 'zod/v4/locales'
import { deepPatternMatch } from './functions'
import { ProblemDetails, ProblemDetailsProto } from './models/ProblemDetails'
import { add } from 'lodash'

// Create an Axios instance
const axiosInstance = axios.create({
  timeout: 10000, // Set a timeout if needed
})


let isRefreshing = false
let refreshPromise: Promise<unknown> | null = null

// Add a request interceptor
axiosInstance.interceptors.request.use(
  async config => {
    // Dispatch request
    store.dispatch(showLoader())

    // List of URLs to exclude from adding Bearer token
    const excludeUrls = [
      GetApiRoute(ApiRoutes.identityLogin).route,
      GetApiRoute(ApiRoutes.identityRefresh).route
    ]

    // Check if the URL is in the exclude list
    if (config.url && excludeUrls.includes(config.url)) {
      return config
    }

    const identity = readIdentity()
    const now = new Date()

    if (identity) {
      if (new Date(identity.expiresAt) < now) {
        // Token expired, refresh if possible
        if (new Date(identity.refreshTokenExpiresAt) > now) {
          if (!isRefreshing) {
            isRefreshing = true
            refreshPromise = store.dispatch(refreshJwt())
              .finally(() => { isRefreshing = false })
          }
          await refreshPromise
          const newIdentity = readIdentity()
          if (newIdentity) {
            config.headers.Authorization = `${newIdentity.tokenType} ${newIdentity.token}`
          }
        }
      } else {
        config.headers.Authorization = `${identity.tokenType} ${identity.token}`
      }
    }

    return config
  },
  error => {
    // Handle request error
    store.dispatch(hideLoader())
    return Promise.reject(error)
  }
)
  
// Add a response interceptor
axiosInstance.interceptors.response.use(
  response => {
    // Dispatch request end
    store.dispatch(hideLoader())
    return response
  },
  error => {
    // Handle response error
    store.dispatch(hideLoader())
    if (error.response) {
      if (error.response.status === 401) {
      // Handle unauthorized error (e.g., redirect to login)
      }
      else {
        const contentType = error.response.headers['content-type']

        if (contentType && contentType.includes('application/problem+json')) {
          const problem = error.response.data as ProblemDetails
          addToast(`${problem.title}: ${problem.detail}`, 'error')
        }
      }
    }
    return Promise.reject(error)
  }
)

/**
 * Performs a GET request and returns the response data.
 * @param url The endpoint URL.
 * @param timeout Optional timeout in milliseconds to override the default.
 */
const getData = async <TResponse>(url: string, timeout?: number): Promise<TResponse | undefined> => {
  try {
    const response = await axiosInstance.get<TResponse>(url, {
      headers: {
        'Content-Type': 'application/json'
      },
      ...(timeout ? { timeout } : {})
    })
    return response.data
  } catch {
    // Error is already handled by interceptors, so just return undefined
    return undefined
  }
}

/**
 * Performs a POST request with the given data and returns the response data.
 * @param url The endpoint URL.
 * @param data The request payload.
 * @param timeout Optional timeout in milliseconds to override the default.
 */
const postData = async <TRequest, TResponse>(url: string, data?: TRequest, timeout?: number): Promise<TResponse | undefined> => {
  try {
    const response = await axiosInstance.post<TResponse>(url, data, {
      headers: {
        'Content-Type': 'application/json'
      },
      ...(timeout ? { timeout } : {})
    })

    return response.data
  } catch {
    // Error is already handled by interceptors, so just return undefined
    return undefined
  }
}

/**
 * Performs a PATCH request with the given data and returns the response data.
 * @param url The endpoint URL.
 * @param data The request payload.
 * @param timeout Optional timeout in milliseconds to override the default.
 */
const patchData = async <TRequest, TResponse>(url: string, data: TRequest, timeout?: number): Promise<TResponse | undefined> => {
  try {
    const response = await axiosInstance.patch<TResponse>(url, data, {
      headers: {
        'Content-Type': 'application/json'
      },
      ...(timeout ? { timeout } : {})
    })
    return response.data
  } catch {
    // Error is already handled by interceptors, so just return undefined
    return undefined
  }
}
  
/**
 * Performs a PUT request with the given data and returns the response data.
 * @param url The endpoint URL.
 * @param data The request payload.
 * @param timeout Optional timeout in milliseconds to override the default.
 */
const putData = async <TRequest, TResponse>(url: string, data: TRequest, timeout?: number): Promise<TResponse | undefined> => {
  try {
    const response = await axiosInstance.put<TResponse>(url, data, {
      headers: {
        'Content-Type': 'application/json'
      },
      ...(timeout ? { timeout } : {})
    })
    return response.data
  } catch {
    // Error is already handled by interceptors, so just return undefined
    return undefined
  }
}
  
/**
 * Performs a DELETE request and returns the response data.
 * @param url The endpoint URL.
 * @param timeout Optional timeout in milliseconds to override the default.
 */
const deleteData = async <TResponse>(url: string, timeout?: number): Promise<TResponse | undefined> => {
  try {
    const response = await axiosInstance.delete<TResponse>(url, {
      headers: {
        'Content-Type': 'application/json'
      },
      ...(timeout ? { timeout } : {})
    })
    return response.data
  } catch {
    // Error is already handled by interceptors, so just return undefined
    return undefined
  }
}

export {
  axiosInstance,
  getData,
  postData,
  patchData,
  putData,
  deleteData
}