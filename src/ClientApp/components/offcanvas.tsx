import React, { FC } from 'react'

interface OffCanvasProps {
  title?: string
  children: React.ReactNode
  isOpen: boolean
  onClose?: () => void
}

const OffCanvas: FC<OffCanvasProps> = (props) => {
  const { title, children, isOpen, onClose } = props

  const handleOnClose = () => {
    onClose?.()
  }

  return (
    <div
      className={`fixed inset-0 bg-gray-800 bg-opacity-50 z-50 transform transition-transform duration-300 ${
        isOpen ? 'translate-x-0' : 'translate-x-full'
      }`}
      onClick={handleOnClose}
    >
      <div
        className="absolute top-0 right-0 bg-white max-w-full md:max-w-md lg:max-w-lg xl:max-w-xl h-full shadow-lg overflow-y-auto"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="p-4">
          {title && <h2 className="text-xl font-bold">{title}</h2>}
          <button onClick={handleOnClose} className="mt-4 text-red-500">
            Close
          </button>
        </div>
        <div className="p-4">{children}</div>
      </div>
    </div>
  )
}

export { OffCanvas }
