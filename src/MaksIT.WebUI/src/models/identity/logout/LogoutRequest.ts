import { boolean, object, Schema, string } from 'zod'

export interface LogoutRequest {
  logoutFromAllDevices?: boolean
  token: string
}

export const LogoutRequestSchema: Schema<LogoutRequest> = object({
  logoutFromAllDevices: boolean().optional(),
  token: string(),
})