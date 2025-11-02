const hasFlag = <T extends number>(current: T = 0 as T, flag: T): boolean => {
  return (current & flag) === flag
}

export { hasFlag }
