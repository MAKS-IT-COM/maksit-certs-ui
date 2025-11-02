const toggleFlag = <T extends number>(current: T = 0 as T, flag: T): T => {
  return ((current & flag) === flag ? (current & ~flag) : (current | flag)) as T
}

export { toggleFlag }