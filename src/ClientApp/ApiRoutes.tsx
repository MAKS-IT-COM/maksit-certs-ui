enum ApiRoutes {
  ACCOUNTS = 'api/accounts',
  ACCOUNT = 'api/account/{accountId}',
  ACCOUNT_CONTACTS = 'api/account/{accountId}/contacts',
  ACCOUNT_CONTACT = 'api/account/{accountId}/contact/{index}',
  ACCOUNT_HOSTNAMES = 'api/account/{accountId}/hostnames'

  // CERTS_FLOW_CONFIGURE_CLIENT =  `api/CertsFlow/ConfigureClient`,
  // CERTS_FLOW_TERMS_OF_SERVICE = `api/CertsFlow/TermsOfService/{sessionId}`,
  // CERTS_FLOW_INIT = `api/CertsFlow/Init/{sessionId}/{accountId}`,
  // CERTS_FLOW_NEW_ORDER = `api/CertsFlow/NewOrder/{sessionId}`,
  // CERTS_FLOW_GET_ORDER = `api/CertsFlow/GetOrder/{sessionId}`,
  // CERTS_FLOW_GET_CERTIFICATES = `api/CertsFlow/GetCertificates/{sessionId}`,
  // CERTS_FLOW_APPLY_CERTIFICATES = `api/CertsFlow/ApplyCertificates/{sessionId}`,
  // CERTS_FLOW_HOSRS_WITH_UPCOMING_SSL_EXPIRY = `api/CertsFlow/HostsWithUpcomingSslExpiry/{sessionId}`
}

const GetApiRoute = (route: ApiRoutes, ...args: string[]): string => {
  let result: string = route
  args.forEach((arg) => {
    result = result.replace(/{.*?}/, arg)
  })
  return `http://localhost:5000/${result}`
}

export { GetApiRoute, ApiRoutes }
