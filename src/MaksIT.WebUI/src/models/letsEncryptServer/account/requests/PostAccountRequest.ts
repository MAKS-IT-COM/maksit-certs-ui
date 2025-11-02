import z, { array, boolean, object, Schema, string } from 'zod'
import { ChallengeType } from '../../../../entities/ChallengeType'

export interface PostAccountRequest {
  description: string
  contacts: string[]
  challengeType: string
  hostnames: string[]
  isStaging: boolean
}

export const PostAccountRequestSchema: Schema<PostAccountRequest> = object({
  description: string(),
  contacts: array(string()),
  hostnames: array(string()),
  challengeType: z.enum(ChallengeType),
  isStaging: boolean()
})
