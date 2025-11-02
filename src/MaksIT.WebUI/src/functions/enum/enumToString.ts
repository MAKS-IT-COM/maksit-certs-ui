import { EnumArrayProps, enumToArr } from './enumToArr'

const getEnumValue = <T>(enumType: T, enumValue: number) : EnumArrayProps | undefined => {
  return enumToArr(enumType).find((item) => item.value == enumValue)
}

const enumToString = <T>(enumType: T, enumValue?: number | null): string => {

  if (enumValue === undefined || enumValue === null) {
    return ''
  }

  const enumVal = getEnumValue(enumType, enumValue)

  if (!enumVal)
    return ''

  return `${enumVal.value} - ${enumVal.displayValue}`
}

export {
  enumToString
}