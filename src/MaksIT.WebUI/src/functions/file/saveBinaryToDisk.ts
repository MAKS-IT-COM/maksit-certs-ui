/**
 * Saves binary data to disk by creating a downloadable link.
 * @param data The binary data to save (ArrayBuffer or Blob).
 * @param filename The desired filename for the saved file.
 */
const saveBinaryToDisk = (data: ArrayBuffer | Blob, filename: string) => {
  const blob = data instanceof Blob ? data : new Blob([data])
  const url = URL.createObjectURL(blob)

  const a = document.createElement('a')
  a.href = url
  a.download = filename

  document.body.appendChild(a)
  a.click()
  a.remove()

  setTimeout(() => URL.revokeObjectURL(url), 1000)
}

export {
  saveBinaryToDisk
}