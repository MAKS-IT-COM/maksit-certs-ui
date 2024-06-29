const deepCopy = <T>(input: T): T => {
  const map = new Map()

  const clone = (item: any): any => {
    if (item === null || typeof item !== 'object') {
      return item
    }

    if (map.has(item)) {
      return map.get(item)
    }

    let result: any

    if (Array.isArray(item)) {
      result = []
      map.set(item, result)
      item.forEach((element, index) => {
        result[index] = clone(element)
      })
    } else if (item instanceof Date) {
      result = new Date(item)
      map.set(item, result)
    } else if (item instanceof RegExp) {
      result = new RegExp(item)
      map.set(item, result)
    } else {
      result = Object.create(Object.getPrototypeOf(item))
      map.set(item, result)
      Object.keys(item).forEach((key) => {
        result[key] = clone(item[key])
      })
    }

    return result
  }

  return clone(input)
}

export { deepCopy }
