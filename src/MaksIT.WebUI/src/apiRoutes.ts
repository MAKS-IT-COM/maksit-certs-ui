/** API route definitions — keep free of AppMap/pages imports to avoid circular deps with axiosConfig. */

export enum ApiRoutes {
  ACCOUNTS_GET = 'GET|/accounts',

  ACCOUNT_POST = 'POST|/account',
  ACCOUNT_GET = 'GET|/account/{accountId}',
  ACCOUNT_PATCH = 'PATCH|/account/{accountId}',
  ACCOUNT_DELETE = 'DELETE|/account/{accountId}',

  CERTS_FLOW_CONFIGURE_CLIENT = 'POST|/certs/configure-client',
  CERTS_FLOW_TERMS_OF_SERVICE = 'GET|/certs/{sessionId}/terms-of-service',
  CERTS_FLOW_CERTIFICATES_APPLY = 'POST|/certs/{accountId}/certificates/apply',

  FULL_CACHE_DOWNLOAD_GET = 'GET|/cache/download',
  FULL_CACHE_UPLOAD_POST = 'POST|/cache/upload',
  FULL_CACHE_DELETE = 'DELETE|/cache',

  CACHE_DOWNLOAD_GET = 'GET|/cache/{accountId}/download/',
  CACHE_UPLOAD_POST = 'POST|/cache/{accountId}/upload/',

  AGENT_TEST = 'GET|/agent/test',

  generateSecret = 'GET|/secret/generatesecret',

  identitySearch = 'POST|/identity/search',
  identitySearchUserScopes = 'POST|/identity/scopes/search',
  identityGet = 'GET|/identity/user/{userId}',
  identityPost = 'POST|/identity/user',
  identityPatch = 'PATCH|/identity/user/{userId}',
  identityDelete = 'DELETE|/identity/user/{userId}',

  identityLogin = 'POST|/identity/login',
  identityRefresh = 'POST|/identity/refresh',
  identityLogout = 'POST|/identity/logout',

  apikeySearch = 'POST|/apikey/search',
  apikeySearchEntityScopes = 'POST|/apikey/search/entity-scopes',
  apikeyPost = 'POST|/apikey',
  apikeyGet = 'GET|/apikey/{apiKeyId}',
  apikeyPatch = 'PATCH|/apikey/{apiKeyId}',
  apikeyDelete = 'DELETE|/apikey/{apiKeyId}',
}

export interface ApiRoute {
  method: string
  route: string
}

export function GetApiRoute(apiRoute: ApiRoutes): ApiRoute {
  const apiUrl = window.RUNTIME_CONFIG?.API_URL || import.meta.env.VITE_API_URL
  const [method, route] = apiRoute.split('|')

  return {
    method,
    route: `${apiUrl}${route}`,
  }
}
