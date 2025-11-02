const deepMerge = <T>(target: T, source: T, seen = new WeakMap<object, unknown>()): T => {
  if (target === null || typeof target !== 'object') return source
  if (source === null || typeof source !== 'object') return target

  if (seen.has(target as object)) {
    return seen.get(target as object) as T
  }

  if (Array.isArray(target) && Array.isArray(source)) {
    const mergedArray: unknown[] = []
    seen.set(target, mergedArray)

    const maxLength = Math.max(target.length, source.length)
    for (let i = 0; i < maxLength; i++) {
      mergedArray[i] = deepMerge(target[i], source[i], seen)
    }

    return mergedArray as T
  }

  const mergedObject = { ...target } as Record<string | number | symbol, unknown>
  seen.set(target as object, mergedObject)

  for (const key of Object.keys(source) as Array<keyof T>) {
    const sourceValue = source[key]
    if (sourceValue !== undefined) {
      const targetValue = target[key]
      mergedObject[key] = deepMerge(targetValue, sourceValue, seen)
    }
  }

  return mergedObject as T
}

export { deepMerge }
