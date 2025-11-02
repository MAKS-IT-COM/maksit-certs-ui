export interface EnumArrayProps {
    value: number | string;
    displayValue: string;
}


const enumToArr = (enumType: unknown): EnumArrayProps[] => {
  if (!enumType) return []

  const enumEntries = Object.entries(enumType)
  const addedValues = new Set()
  const result: EnumArrayProps[] = []

  enumEntries.forEach(([key, value]) => {
    // Skip numeric keys to avoid reverse mapping duplicates in numeric enums
    if (!isNaN(Number(key))) return

    // Skip already added values for string enums with reverse mapping
    if (addedValues.has(value)) return
    addedValues.add(value)

    result.push({
      value: value,
      displayValue: key,
    })
  })

  // Sort the result array by displayValue (key)
  result.sort((a, b) => {
    if (typeof a.displayValue === 'string' && typeof b.displayValue === 'string') {
      return a.displayValue.localeCompare(b.displayValue)
    }
    return 0
  })

  return result
}

export { enumToArr }
