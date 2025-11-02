import { LoginResponse } from '../models/identity/login/LoginResponse'

const readIdentity = () => {
  const json = localStorage.getItem('identity')

  if (!json) return undefined

  return JSON.parse(json) as LoginResponse
}

const writeIdentity = (identity: LoginResponse) => {
  localStorage.setItem('identity', JSON.stringify(identity))
}

const removeIdentity = () => {
  localStorage.removeItem('identity')
}

export { readIdentity, writeIdentity, removeIdentity }