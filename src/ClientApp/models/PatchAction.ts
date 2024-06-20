export interface PatchAction<T> {
  op: PatchOperation // Enum for operation type
  index?: number // Index for the operation (for arrays/lists)
  value?: T // Value for the operation
}
