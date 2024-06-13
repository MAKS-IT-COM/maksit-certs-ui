interface HostnameResponse {
    hostname: string
    expires: string,
    isUpcomingExpire: boolean
}

export interface GetHostnamesResponse {
    hostnames: HostnameResponse[]
}