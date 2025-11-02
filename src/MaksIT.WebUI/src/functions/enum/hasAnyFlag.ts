const hasAnyFlag = <T extends number>(current: T = 0 as T, flags: T): boolean => {
  return (current & flags) !== 0
}

export { hasAnyFlag }