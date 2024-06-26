interface EnumKeyValue {
  key: string
  value: string | number
}

const enumToArray = <T extends { [key: string]: string | number }>(
  enumObj: T
): EnumKeyValue[] => {
  return Object.keys(enumObj)
    .filter((key) => isNaN(Number(key))) // Ensure that only string keys are considered
    .map((key) => ({
      key,
      value: enumObj[key as keyof typeof enumObj]
    }))
    .map((entry) => ({
      key: entry.key,
      value:
        typeof entry.value === 'string' && !isNaN(Number(entry.value))
          ? Number(entry.value)
          : entry.value
    }))
}

const enumToObject = <T extends { [key: string]: string | number }>(
  enumObj: T
): { [key: string]: EnumKeyValue } => {
  return Object.keys(enumObj)
    .filter((key) => isNaN(Number(key))) // Ensure that only string keys are considered
    .reduce(
      (acc, key) => {
        const value = enumObj[key as keyof typeof enumObj]
        acc[key] = {
          key,
          value:
            typeof value === 'string' && !isNaN(Number(value))
              ? Number(value)
              : value
        }
        return acc
      },
      {} as { [key: string]: EnumKeyValue }
    )
}

export { enumToArray, enumToObject }
