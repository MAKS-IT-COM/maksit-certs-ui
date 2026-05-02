import { boolean, object, Schema } from 'zod'

export interface LogoutRequest {
  logoutFromAllDevices?: boolean
}

export const LogoutRequestSchema: Schema<LogoutRequest> = object({
  logoutFromAllDevices: boolean().optional(),
})