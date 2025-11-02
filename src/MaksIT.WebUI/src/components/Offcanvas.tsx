import { FC, ReactNode, useCallback, useEffect } from 'react'

export interface OffcanvasProps {
  children: ReactNode
  isOpen?: boolean
  onOpen?: () => void
  onClose?: () => void
  colspan?: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12
}

const Offcanvas: FC<OffcanvasProps> = (props) => {
  const {
    children,
    isOpen = false,
    onOpen,
    onClose,
    colspan = 6
  } = props

  const handleOnOpen = useCallback(() => {
    onOpen?.()
  }, [onOpen])

  const handleOnClose = useCallback(() => {
    onClose?.()
  }, [onClose])

  useEffect(() => {
    if (isOpen) handleOnOpen()
    else handleOnClose()
  }, [isOpen, handleOnOpen, handleOnClose])

  const leftSpan = 12 - colspan

  return (
    <div
      className={[
        'fixed inset-0 h-screen w-screen',
        'bg-black/20 backdrop-blur-md',
        'z-40 transition-opacity duration-300 ease-in-out',
        isOpen ? 'opacity-100' : 'opacity-0 pointer-events-none',
      ].join(' ')}
    >
      <div className={'grid grid-cols-12 h-full w-full'}>
        {/* colonna di offset */}
        <div className={`col-span-${leftSpan}`} />
        {/* area principale */}
        <div className={`col-span-${colspan} min-h-0`}>
          {children}
        </div>
      </div>
    </div>
  )
}

export { Offcanvas }
