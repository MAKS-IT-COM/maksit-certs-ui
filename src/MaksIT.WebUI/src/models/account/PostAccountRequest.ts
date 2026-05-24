import z, { array, boolean, object, string, type ZodType } from 'zod'
import { ChallengeType } from '../../entities/ChallengeType'
import { RequestModelBase, RequestModelBaseSchema } from '@maks-it.com/webui-contracts'

export interface PostAccountRequest extends RequestModelBase {
  description: string
  contacts: string[]
  challengeType: string
  hostnames: string[]
  isStaging: boolean
  agreeToS: boolean
}

export const PostAccountRequestSchema: ZodType<PostAccountRequest> = RequestModelBaseSchema.and(
  object({
    description: string(),
    contacts: array(string()),
    hostnames: array(string()),
    challengeType: z.enum(ChallengeType),
    isStaging: boolean(),
    agreeToS: boolean()
  })
)