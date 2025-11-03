export interface ProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
  instance?: string;
  extensions: { [key: string]: never };
}

export const ProblemDetailsProto = (): ProblemDetails => ({
  status: undefined,
  title: undefined,
  detail: undefined,
  instance: undefined,
  extensions: {}
})