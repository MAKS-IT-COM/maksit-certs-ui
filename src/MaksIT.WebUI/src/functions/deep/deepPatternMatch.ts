const deepPatternMatch = <T extends object>(pattern: T, obj: unknown): boolean => {
  if (typeof obj !== 'object' || obj === null) return false
  const objKeys = Object.keys(obj as object)
  const patternKeys = Object.keys(pattern)
  // obj must not have more keys than pattern
  if (objKeys.length > patternKeys.length) return false
  for (const key of objKeys) {
    if (!(key in pattern)) return false
    if (typeof (obj as T)[key as keyof T] !== typeof pattern[key as keyof T]) return false
  }
  return true
}

export {
  deepPatternMatch
}
