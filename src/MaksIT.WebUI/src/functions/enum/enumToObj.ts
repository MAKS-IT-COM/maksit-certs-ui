
const enumToObj = (enumType: unknown) => {
  if (!enumType) return {}

  const enumEntries = Object.entries(enumType)
  const result: { [key: string]: number | string } = {}

  enumEntries.forEach(([key, value]) => {
    // Skip numeric keys to avoid reverse mapping duplicates in numeric enums
    if (!isNaN(Number(key))) return
    result[key] = value
  })

  return result
}

export { enumToObj }