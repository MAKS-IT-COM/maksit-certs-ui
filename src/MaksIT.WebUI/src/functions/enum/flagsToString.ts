import { enumToArr } from './enumToArr'

const flagsToString = <T>(enumType: T, flags: number): string => {
  return enumToArr(enumType)
    .filter(opt => (flags & opt.value as number) === opt.value && opt.value !== 0)
    .map(opt => opt.displayValue)
    .join(', ') || 'None'
}

export { flagsToString }