interface RequestInterceptor {
    (req: XMLHttpRequest): void;
}

interface ResponseInterceptor<T> {
    (response: T): T;
}

interface ProblemDetails {
    Title: string;
    Detail: string | null;
    Status: number;
}

class HttpService {
    private requestInterceptors: Array<RequestInterceptor> = [];
    private responseInterceptors: Array<ResponseInterceptor<any>> = [];

    private request<TResponse>(method: string, url: string, data?: any): Promise<TResponse> {
        return new Promise((resolve, reject) => {
            const xhr = new XMLHttpRequest();
            xhr.open(method, url);

            // Apply request interceptors
            this.requestInterceptors.forEach(interceptor => {
                try {
                    interceptor(xhr);
                } catch (error) {
                    reject({
                        Title: 'Request Interceptor Error',
                        Detail: error instanceof Error ? error.message : 'Unknown error',
                        Status: 0
                    });
                    return;
                }
            });

            // Set Content-Type header for JSON data
            if (data && typeof data !== 'string') {
                xhr.setRequestHeader('Content-Type', 'application/json');
            }

            xhr.onload = () => {
                if (xhr.status >= 200 && xhr.status < 300) {
                    let response: TResponse;
                    try {
                        response = JSON.parse(xhr.response);
                    } catch (error) {
                        reject({
                            Title: 'Response Parse Error',
                            Detail: error instanceof Error ? error.message : 'Unknown error',
                            Status: xhr.status
                        });
                        return;
                    }

                    // Apply response interceptors
                    try {
                        this.responseInterceptors.forEach(interceptor => {
                            response = interceptor(response);
                        });
                    } catch (error) {
                        reject({
                            Title: 'Response Interceptor Error',
                            Detail: error instanceof Error ? error.message : 'Unknown error',
                            Status: xhr.status
                        });
                        return;
                    }

                    resolve(response);
                } else {
                    const problemDetails: ProblemDetails = {
                        Title: xhr.statusText,
                        Detail: xhr.responseText,
                        Status: xhr.status
                    };
                    reject(problemDetails);
                }
            };

            xhr.onerror = () => {
                const problemDetails: ProblemDetails = {
                    Title: 'Network Error',
                    Detail: null,
                    Status: 0
                };
                reject(problemDetails);
            };

            xhr.send(data ? JSON.stringify(data) : null);
        });
    }

    public get<TResponse>(url: string): Promise<TResponse | ProblemDetails> {
        return this.request<TResponse>('GET', url);
    }

    public post<TRequest, TResponse>(url: string, data: TRequest): Promise<TResponse | ProblemDetails> {
        return this.request<TResponse>('POST', url, data);
    }

    public put<TRequest, TResponse>(url: string, data: TRequest): Promise<TResponse | ProblemDetails> {
        return this.request<TResponse>('PUT', url, data);
    }

    public delete<TResponse>(url: string): Promise<TResponse | ProblemDetails> {
        return this.request<TResponse>('DELETE', url);
    }

    public addRequestInterceptor(interceptor: RequestInterceptor): void {
        this.requestInterceptors.push(interceptor);
    }

    public addResponseInterceptor<TResponse>(interceptor: ResponseInterceptor<TResponse | ProblemDetails>): void {
        this.responseInterceptors.push(interceptor);
    }
}

export {
    HttpService
};
