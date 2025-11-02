import { ResponseModelBase } from './ResponseModelBase'

export interface TrngResponse extends ResponseModelBase {
  secret: string;
}