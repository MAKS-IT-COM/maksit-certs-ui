import { store } from '@/redux/store'
import { increment, decrement } from '@/redux/slices/loaderSlice'
import { showToast } from '@/redux/slices/toastSlice'

interface RequestInterceptor {
  (req: XMLHttpRequest): void
}

interface ResponseInterceptor<T> {
  (response: T | null, error: ProblemDetails | null): T | void
}

interface ProblemDetails {
  Title: string
  Detail: string | null
  Status: number
}

interface HttpServiceCallbacks {
  onIncrement?: () => void
  onDecrement?: () => void
  onShowToast?: (message: string, type: 'info' | 'error') => void
}

class HttpService {
  private requestInterceptors: RequestInterceptor[] = []
  private responseInterceptors: Array<ResponseInterceptor<any>> = []
  private callbacks: HttpServiceCallbacks

  constructor(callbacks: HttpServiceCallbacks) {
    this.callbacks = callbacks
  }

  private invokeIncrement(): void {
    this.callbacks.onIncrement?.()
  }

  private invokeDecrement(): void {
    this.callbacks.onDecrement?.()
  }

  private invokeShowToast(message: string, type: 'info' | 'error'): void {
    this.callbacks.onShowToast?.(message, type)
  }

  private async request<TResponse>(
    method: string,
    url: string,
    data?: any
  ): Promise<TResponse | null> {
    const xhr = new XMLHttpRequest()
    xhr.open(method, url)

    this.handleRequestInterceptors(xhr)

    if (data && typeof data !== 'string') {
      xhr.setRequestHeader('Content-Type', 'application/json')
    }

    this.invokeIncrement()

    return new Promise<TResponse | null>((resolve) => {
      xhr.onload = () => this.handleLoad<TResponse>(xhr, resolve)
      xhr.onerror = () => this.handleNetworkError(resolve)
      xhr.send(data ? JSON.stringify(data) : null)
    })
  }

  private handleRequestInterceptors(xhr: XMLHttpRequest): void {
    this.requestInterceptors.forEach((interceptor) => {
      try {
        interceptor(xhr)
      } catch (error) {
        const problemDetails = this.createProblemDetails(
          'Request Interceptor Error',
          error,
          0
        )
        this.showProblemDetails(problemDetails)
      }
    })
  }

  private handleResponseInterceptors<TResponse>(
    response: TResponse | null,
    error: ProblemDetails | null
  ): TResponse | null {
    this.responseInterceptors.forEach((interceptor) => {
      try {
        interceptor(response, error)
      } catch (e) {
        const problemDetails = this.createProblemDetails(
          'Response Interceptor Error',
          e,
          0
        )
        this.showProblemDetails(problemDetails)
      }
    })
    return response
  }

  private handleLoad<TResponse>(
    xhr: XMLHttpRequest,
    resolve: (value: TResponse | null) => void
  ): void {
    this.invokeDecrement()
    if (xhr.status >= 200 && xhr.status < 300) {
      this.handleSuccessfulResponse<TResponse>(xhr, resolve)
    } else {
      this.handleErrorResponse(xhr, resolve)
    }
  }

  private handleSuccessfulResponse<TResponse>(
    xhr: XMLHttpRequest,
    resolve: (value: TResponse | null) => void
  ): void {
    try {
      if (xhr.response) {
        const response = JSON.parse(xhr.response)
        resolve(this.handleResponseInterceptors(response, null) as TResponse)
      } else {
        resolve(null)
      }
    } catch (error) {
      const problemDetails = this.createProblemDetails(
        'Response Parse Error',
        error,
        xhr.status
      )
      this.showProblemDetails(problemDetails)
      resolve(null)
    }
  }

  private handleErrorResponse<TResponse>(
    xhr: XMLHttpRequest,
    resolve: (value: TResponse | null) => void
  ): void {
    const problemDetails = this.createProblemDetails(
      xhr.statusText,
      xhr.responseText,
      xhr.status
    )
    this.showProblemDetails(problemDetails)
    resolve(this.handleResponseInterceptors(null, problemDetails))
  }

  private handleNetworkError<TResponse>(
    resolve: (value: TResponse | null) => void
  ): void {
    const problemDetails = this.createProblemDetails('Network Error', null, 0)
    this.showProblemDetails(problemDetails)
    resolve(this.handleResponseInterceptors(null, problemDetails))
  }

  private createProblemDetails(
    title: string,
    detail: any,
    status: number
  ): ProblemDetails {
    return {
      Title: title,
      Detail: detail instanceof Error ? detail.message : String(detail),
      Status: status
    }
  }

  private showProblemDetails(problemDetails: ProblemDetails): void {
    if (problemDetails.Detail) {
      const errorMessages = problemDetails.Detail.split(',')
      errorMessages.forEach((message) => {
        this.invokeShowToast(message.trim(), 'error')
      })
    } else {
      this.invokeShowToast('Unknown error', 'error')
    }
  }

  public async get<TResponse>(url: string): Promise<TResponse | null> {
    return await this.request<TResponse>('GET', url)
  }

  public async post<TRequest, TResponse>(
    url: string,
    data: TRequest
  ): Promise<TResponse | null> {
    return await this.request<TResponse>('POST', url, data)
  }

  public async put<TRequest, TResponse>(
    url: string,
    data: TRequest
  ): Promise<TResponse | null> {
    return await this.request<TResponse>('PUT', url, data)
  }

  public async delete<TResponse>(url: string): Promise<TResponse | null> {
    return await this.request<TResponse>('DELETE', url)
  }

  public addRequestInterceptor(interceptor: RequestInterceptor): void {
    this.requestInterceptors.push(interceptor)
  }

  public addResponseInterceptor<TResponse>(
    interceptor: ResponseInterceptor<TResponse>
  ): void {
    this.responseInterceptors.push(interceptor)
  }
}

// Instance of HttpService
const httpService = new HttpService({
  onIncrement: () => store.dispatch(increment()),
  onDecrement: () => store.dispatch(decrement()),
  onShowToast: (message: string, type: 'info' | 'error') =>
    store.dispatch(showToast({ message, type }))
})

// Add loader state handling via interceptors
httpService.addRequestInterceptor((xhr) => {
  // Additional request logic can be added here
})

httpService.addResponseInterceptor((response, error) => {
  // Additional response logic can be added here
  return response
})

export { httpService }

// Example usage of the httpService
// async function fetchData() {
//     const data = await httpService.get<any>('/api/data');
//     if (data) {
//         console.log('Data received:', data);
//     } else {
//         console.error('Failed to fetch data');
//     }
// }
