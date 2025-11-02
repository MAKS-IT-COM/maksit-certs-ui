import { ResponseModelBase } from './ResponseModelBase'

export interface SearchResponseBase extends ResponseModelBase {
    id: string;
    name: string;
}