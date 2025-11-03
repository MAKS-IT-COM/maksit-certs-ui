import React, { useRef, useState } from 'react'
import { ButtonComponent } from './ButtonComponent'
import { TrashIcon } from 'lucide-react'

interface FileUploadComponentProps {
  label?: string
  colspan?: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12
  multiple?: boolean
  onChange?: (files: File[]) => void
  disabled?: boolean
}

const FileUploadComponent: React.FC<FileUploadComponentProps> = ({
  label = 'Select files',
  colspan = 6,
  multiple = true,
  onChange,
  disabled = false,
}) => {
  const [selectedFiles, setSelectedFiles] = useState<File[]>([])
  const [showPopup, setShowPopup] = useState(false)
  const [popupPos, setPopupPos] = useState<{x: number, y: number}>({x: 0, y: 0})
  const popupRef = useRef<HTMLDivElement>(null)
  const inputRef = useRef<HTMLInputElement>(null)

  // Focus popup when it opens
  React.useEffect(() => {
    if (showPopup && popupRef.current) {
      popupRef.current.focus()
    }
  }, [showPopup])

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files ? Array.from(e.target.files) : []
    setSelectedFiles(files)
    onChange?.(files)
  }

  const handleClear = () => {
    setSelectedFiles([])
    if (inputRef.current) inputRef.current.value = ''
    onChange?.([])
  }

  const handleSelectFiles = () => {
    if (!disabled) inputRef.current?.click()
  }

  return (
    <div className={`grid grid-cols-4 gap-2 ${colspan ? `col-span-${colspan}` : 'w-full'}`}>
      {/* File input (hidden) */}
      <input
        ref={inputRef}
        type={'file'}
        multiple={multiple}
        style={{ display: 'none' }}
        onChange={handleFileChange}
        disabled={disabled}
      />

      {/* Files counter with hover popup */}
      <div
        className={'col-span-1 flex items-center justify-center relative'}
        onMouseEnter={e => {
          setShowPopup(true)
          if (!showPopup) {
            setPopupPos({ x: e.clientX, y: e.clientY })
          }
        }}
        onMouseLeave={e => {
          // Only close if not moving into popup
          if (!popupRef.current || !popupRef.current.contains(e.relatedTarget as Node)) {
            setShowPopup(false)
          }
        }}
      >
        <span
          className={'bg-gray-200 px-4 py-2 rounded w-full text-center select-none block'}
          style={{ minHeight: '40px', display: 'flex', alignItems: 'center', justifyContent: 'center' }}
        >
          {selectedFiles.length} file{selectedFiles.length !== 1 ? 's' : ''}
        </span>
        {showPopup && selectedFiles.length > 0 && (
          <div
            ref={popupRef}
            className={'fixed z-50 bg-white border border-gray-300 rounded shadow-lg p-2 text-sm'}
            tabIndex={0}
            style={{
              left: popupPos.x + 2,
              top: popupPos.y + 2,
              maxWidth: '400px',
              whiteSpace: 'nowrap',
              minWidth: '120px',
              pointerEvents: 'auto',
              outline: 'none',
            }}
            onBlur={e => {
              // Only close if focus moves outside popup and counter
              if (!e.relatedTarget || (!popupRef.current?.contains(e.relatedTarget as Node) && !(e.relatedTarget as HTMLElement)?.closest('.col-span-1'))) {
                setShowPopup(false)
              }
            }}
            onMouseLeave={e => {
              // Only close if not moving back to counter
              const parent = (e.relatedTarget as HTMLElement)?.closest('.col-span-1')
              if (!parent) setShowPopup(false)
            }}
            onKeyDown={e => {
              if (e.key === 'Escape') setShowPopup(false)
            }}
            onFocus={() => {}}
          >
            <ul className={'max-h-40 overflow-auto'} tabIndex={0} style={{outline: 'none'}}>
              {selectedFiles.map((file, idx) => (
                <li key={file.name + idx} className={'truncate'} title={file.name}>
                  {file.name}
                </li>
              ))}
            </ul>
          </div>
        )}


      </div>

      {/* Clear selection button */}
      <ButtonComponent
        buttonHierarchy={'secondary'}
        onClick={handleClear}
        disabled={disabled || selectedFiles.length === 0}
        colspan={1}
      >
        <TrashIcon />
      </ButtonComponent>

      {/* Select files button */}
      <ButtonComponent
        colspan={2}        
        children={label}
        buttonHierarchy={'primary'}
        onClick={handleSelectFiles}
        disabled={disabled}
      />

    </div>
  )
}

export { FileUploadComponent }
