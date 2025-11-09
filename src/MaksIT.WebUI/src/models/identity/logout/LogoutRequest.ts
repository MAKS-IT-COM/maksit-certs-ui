import { boolean, object, Schema, string } from 'zod'

export interface LogoutRequest {
    logOutFromAllDevices?: boolean;
    token: string;
}

export const LoginRequestSchema: Schema<LogoutRequest> = object({
  logOutFromAllDevices: boolean().optional(),
  token: string()
})