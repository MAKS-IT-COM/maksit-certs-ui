import { parseISO, isValid } from 'date-fns'



const isValidISODateString = (dateString: string): boolean => {
  if (!dateString) return false
  const parsed = parseISO(dateString)
  return isValid(parsed)
}



export {
  isValidISODateString
}