import { parseISO, isValid, format } from 'date-fns'

const DISPLAY_FORMAT = 'yyyy-MM-dd HH:mm'

const formatISODateString = (isoString: string): string => {
  if (!isoString)
    return ''

  const parsed = parseISO(isoString)

  if (!isValid(parsed))
    return 'ISO Date String is invalid'

  return format(parsed, DISPLAY_FORMAT)
}

export {
  formatISODateString
}