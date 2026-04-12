export interface ProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
  instance?: string;
  /** Validation errors: property name -> list of messages (ASP.NET ValidationProblemDetails) */
  errors?: Record<string, string[]>;
  extensions: { [key: string]: never };
}

export const ProblemDetailsProto = (): ProblemDetails => ({
  status: undefined,
  title: undefined,
  detail: undefined,
  instance: undefined,
  extensions: {}
})
