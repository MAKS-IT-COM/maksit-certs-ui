/* eslint-disable @typescript-eslint/no-explicit-any -- axios config bags use dynamic fields (skipLoader) */
import axios from 'axios'
import { readIdentity } from './localStorage/identity'
import { ApiRoutes, GetApiRoute } from './AppMap'
import { store } from './redux/store'
import { refreshJwt, clearIdentity } from './redux/slices/identitySlice'
import { hideLoader, showLoader } from './redux/slices/loaderSlice'
import { addToast } from './components/Toast/addToast'
import { ProblemDetails } from './models/ProblemDetails'


interface RequestOptions {
  skipLoader?: boolean
}

// Create an Axios instance
const axiosInstance = axios.create({
  timeout: 10000, // Set a timeout if needed
})

let isRefreshing = false
let refreshPromise: Promise<unknown> | null = null

const getExcludeUrls = () => [
  GetApiRoute(ApiRoutes.identityLogin).route,
  GetApiRoute(ApiRoutes.identityRefresh).route
]

const isAuthExcludedUrl = (url: string | undefined) =>
  url !== undefined && getExcludeUrls().includes(url)

// Add a request interceptor
axiosInstance.interceptors.request.use(
  async config => {
    // Dispatch request (unless explicitly skipped)
    const skipLoader = (config as any).skipLoader as boolean | undefined
    if (!skipLoader) {
      store.dispatch(showLoader())
    }

    // List of URLs to exclude from adding Bearer token
    const excludeUrls = getExcludeUrls()

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
          else {
            // Refresh failed (e.g. 401); identity was cleared by identitySlice. Do not send request with expired token.
            store.dispatch(clearIdentity())
            if (!skipLoader) store.dispatch(hideLoader())
            return Promise.reject(new Error('Session expired. Please sign in again.'))
          }
        }
      }
      else {
        config.headers.Authorization = `${identity.tokenType} ${identity.token}`
      }
    }

    return config
  },
  error => {
    // Handle request error
    const skipLoader = (error.config as any)?.skipLoader as boolean | undefined
    if (!skipLoader) {
      store.dispatch(hideLoader())
    }
    return Promise.reject(error)
  }
)
  
// Add a response interceptor
axiosInstance.interceptors.response.use(
  response => {
    // Dispatch request end (unless explicitly skipped)
    const skipLoader = (response.config as any)?.skipLoader as boolean | undefined
    if (!skipLoader) {
      store.dispatch(hideLoader())
    }
    return response
  },
  async error => {
    const originalRequest = error.config

    const skipLoader = (originalRequest as any)?.skipLoader as boolean | undefined
    if (!skipLoader) {
      store.dispatch(hideLoader())
    }

    if (error.response?.status === 401 && originalRequest && !originalRequest._retryAfterRefresh && !isAuthExcludedUrl(originalRequest.url)) {
      const identity = readIdentity()
      if (identity && new Date(identity.refreshTokenExpiresAt) > new Date()) {
        originalRequest._retryAfterRefresh = true
        try {
          if (!isRefreshing) {
            isRefreshing = true
            refreshPromise = store.dispatch(refreshJwt())
              .finally(() => { isRefreshing = false })
          }
          await refreshPromise
          const newIdentity = readIdentity()
          if (newIdentity) {
            originalRequest.headers.Authorization = `${newIdentity.tokenType} ${newIdentity.token}`
            return axiosInstance(originalRequest)
          }
        }
        catch {
          // Refresh failed (e.g. 401); clear identity so UI redirects to login
          store.dispatch(clearIdentity())
        }
      }
    }

    if (error.response) {
      const contentType = error.response.headers['content-type']
      const data = error.response.data

      if (contentType && contentType.includes('application/problem+json')) {
        const problem = data as ProblemDetails
        const detail = problem.detail ?? ''
        const errors = problem.errors
          ? Object.entries(problem.errors)
            .flatMap(([key, msgs]) => (msgs ?? []).map(m => `${key}: ${m}`))
            .join('; ')
          : ''
        const message = [detail, errors].filter(Boolean).join(' ') || problem.title || 'Request failed'
        addToast(message, 'error')
      }
      else if (error.response.status === 401) {
        const problem = data as ProblemDetails
        const message = problem.detail ?? problem.title ?? 'Unauthorized'
        addToast(message, 'error')
      }
    }

    return Promise.reject(error)
  }
)

/**
 * Performs a GET request and returns the response data.
 * @param url The endpoint URL.
 * @param timeout Optional timeout in milliseconds to override the default.
 * @returns The response data, or undefined if an error occurs.
 */
const getData = async <TResponse>(
  url: string,
  timeout?: number,
  options?: RequestOptions
): Promise<TResponse | undefined> => {
  try {
    const config: any = {
      headers: {
        'Content-Type': 'application/json'
      },
      ...(timeout ? { timeout } : {})
    }

    if (options?.skipLoader) {
      config.skipLoader = true
    }

    const response = await axiosInstance.get<TResponse>(url, config)
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
 * @returns The response data, or undefined if an error occurs.
 */
const postData = async <TRequest, TResponse>(
  url: string,
  data?: TRequest,
  timeout?: number,
  options?: RequestOptions
): Promise<TResponse | undefined> => {
  try {
    const config: any = {
      headers: {
        'Content-Type': 'application/json'
      },
      ...(timeout ? { timeout } : {})
    }

    if (options?.skipLoader) {
      config.skipLoader = true
    }

    const response = await axiosInstance.post<TResponse>(url, data, config)

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
 * @returns The response data, or undefined if an error occurs.
 */
const patchData = async <TRequest, TResponse>(
  url: string,
  data: TRequest,
  timeout?: number,
  options?: RequestOptions
): Promise<TResponse | undefined> => {
  try {
    const config: any = {
      headers: {
        'Content-Type': 'application/json'
      },
      ...(timeout ? { timeout } : {})
    }

    if (options?.skipLoader) {
      config.skipLoader = true
    }

    const response = await axiosInstance.patch<TResponse>(url, data, config)
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
 * @returns The response data, or undefined if an error occurs.
 */
const putData = async <TRequest, TResponse>(
  url: string,
  data: TRequest,
  timeout?: number,
  options?: RequestOptions
): Promise<TResponse | undefined> => {
  try {
    const config: any = {
      headers: {
        'Content-Type': 'application/json'
      },
      ...(timeout ? { timeout } : {})
    }

    if (options?.skipLoader) {
      config.skipLoader = true
    }

    const response = await axiosInstance.put<TResponse>(url, data, config)
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
 * @returns The response data, or undefined if an error occurs.
 */
const deleteData = async <TResponse>(
  url: string,
  timeout?: number,
  options?: RequestOptions
): Promise<TResponse | undefined> => {
  try {
    const config: any = {
      headers: {
        'Content-Type': 'application/json'
      },
      ...(timeout ? { timeout } : {})
    }

    if (options?.skipLoader) {
      config.skipLoader = true
    }

    const response = await axiosInstance.delete<TResponse>(url, config)
    return response.data
  } catch {
    // Error is already handled by interceptors, so just return undefined
    return undefined
  }
}

/**
 * Performs a POST request with binary payload (e.g., file upload) and returns the response data.
 * @param url The endpoint URL.
 * @param data The binary request payload.
 * @param timeout Optional timeout in milliseconds to override the default.
 * @returns The response data, or undefined if an error occurs.
 */
const postBinary = async <TResponse>(
  url: string,
  data: Blob | ArrayBuffer | Uint8Array,
  timeout?: number,
  options?: RequestOptions
): Promise<TResponse | undefined> => {
  try {
    const config: any = {
      headers: {
        'Content-Type': 'application/octet-stream'
      },
      ...(timeout ? { timeout } : {})
    }

    if (options?.skipLoader) {
      config.skipLoader = true
    }

    const response = await axiosInstance.post<TResponse>(url, data, config)
    return response.data
  } catch {
    // Error is already handled by interceptors, so just return undefined
    return undefined
  }
}

/**
 * Performs a GET request to retrieve binary data (e.g., file download).
 * @param url The endpoint URL.
 * @param timeout Optional timeout in milliseconds to override the default.
 * @param as The format to retrieve the binary data as ('arraybuffer' or 'blob').
 * @returns The binary data and headers, or undefined if an error occurs.
 */
const getBinary = async (
  url: string,
  timeout?: number,
  as: 'arraybuffer' | 'blob' = 'arraybuffer',
  options?: RequestOptions
): Promise<{ data: ArrayBuffer | Blob, headers: Record<string, string> } | undefined> => {
  try {
    const config: any = {
      responseType: as,
      ...(timeout ? { timeout } : {})
    }

    if (options?.skipLoader) {
      config.skipLoader = true
    }

    const response = await axiosInstance.get(url, config)

    return {
      data: response.data,
      headers: response.headers as Record<string, string>
    }
  } catch {
    // Error is already handled by interceptors, so just return undefined
    return undefined
  }
}

/**
 * Performs a POST request using multipart/form-data.
 * Accepts either a ready FormData or a record of fields to be converted into FormData.
 * Note: Do NOT set the Content-Type header manually; the browser will include the boundary.
 * @param url The endpoint URL.
 * @param form The FormData instance or a record of fields.
 *             Values can be string | Blob | File | (string | Blob | File)[]
 * @param timeout Optional timeout in milliseconds to override the default.
 * @returns The response data, or undefined if an error occurs.
 */
const postFormData = async <TResponse>(
  url: string,
  form: FormData | Record<string, string | Blob | File | (string | Blob | File)[]>,
  timeout?: number,
  options?: RequestOptions
): Promise<TResponse | undefined> => {
  try {
    const formData =
      form instanceof FormData
        ? form
        : (() => {
          const fd = new FormData()
          Object.entries(form).forEach(([key, value]) => {
            if (Array.isArray(value)) {
              value.forEach(v => fd.append(key, v))
            } else {
              fd.append(key, value)
            }
          })
          return fd
        })()

    const config: any = {
      // Do NOT set Content-Type; the browser will set the correct multipart boundary
      ...(timeout ? { timeout } : {})
    }

    if (options?.skipLoader) {
      config.skipLoader = true
    }

    const response = await axiosInstance.post<TResponse>(url, formData, config)

    return response.data
  } catch {
    // Error is already handled by interceptors, so just return undefined
    return undefined
  }
}

/**
 * Convenience helper for uploading a single file via multipart/form-data.
 * @param url The endpoint URL.
 * @param file The file/blob to upload.
 * @param fieldName The form field name for the file (default: "file").
 * @param filename Optional filename; if omitted and "file" is a File, the File.name is used.
 * @param extraFields Optional extra key/value fields to include in the form.
 * @param timeout Optional timeout in milliseconds to override the default.
 * @returns The response data, or undefined if an error occurs.
 */
const postFile = async <TResponse>(
  url: string,
  file: Blob | File,
  fieldName: string = 'file',
  filename?: string,
  extraFields?: Record<string, string>,
  timeout?: number,
  options?: RequestOptions
): Promise<TResponse | undefined> => {
  const fd = new FormData()
  const inferredName = filename ?? (file instanceof File ? file.name : 'file')
  fd.append(fieldName, file, inferredName)

  if (extraFields) {
    Object.entries(extraFields).forEach(([k, v]) => fd.append(k, v))
  }

  return postFormData<TResponse>(url, fd, timeout, options)
}

/** Options that disable the global loader for a request (for background/UI-only fetches). */
const noLoaderOptions: RequestOptions = { skipLoader: true }

/**
 * GET without showing the global loader. Use for background fetches (e.g. table filters, remote labels).
 */
const getDataWithoutLoader = async <TResponse>(
  url: string,
  timeout?: number
): Promise<TResponse | undefined> =>
  getData<TResponse>(url, timeout, noLoaderOptions)

/**
 * POST without showing the global loader. Use for background fetches (e.g. remote selects, table filters).
 */
const postDataWithoutLoader = async <TRequest, TResponse>(
  url: string,
  data?: TRequest,
  timeout?: number
): Promise<TResponse | undefined> =>
  postData<TRequest, TResponse>(url, data, timeout, noLoaderOptions)

export {
  axiosInstance,
  getData,
  postData,
  patchData,
  putData,
  deleteData,
  postBinary,
  getBinary,
  postFormData,
  postFile,
  getDataWithoutLoader,
  postDataWithoutLoader
}
