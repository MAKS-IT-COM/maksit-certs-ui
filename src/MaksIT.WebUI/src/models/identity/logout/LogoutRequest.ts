import { z } from 'zod'

export interface LogoutRequest {
    logOutFromAllDevices?: boolean;
}

export const LoginRequestSchema: z.Schema<LogoutRequest> = z.object({
  logOutFromAllDevices: z.boolean().optional()
})