import z, { array, boolean, object, Schema, string } from 'zod'
import { ChallengeType } from '../../../../entities/ChallengeType'
import { RequestModelBase, RequestModelBaseSchema } from '../../../RequestModelBase'

export interface PostAccountRequest extends RequestModelBase {
  description: string
  contacts: string[]
  challengeType: string
  hostnames: string[]
  isStaging: boolean
  agreeToS: boolean
}

export const PostAccountRequestSchema: Schema<PostAccountRequest> = RequestModelBaseSchema.and(
  object({
    description: string(),
    contacts: array(string()),
    hostnames: array(string()),
    challengeType: z.enum(ChallengeType),
    isStaging: boolean(),
    agreeToS: boolean()
  })
)