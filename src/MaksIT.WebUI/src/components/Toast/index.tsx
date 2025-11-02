import { useState, useEffect, FC } from 'react'
import { v4 as uuidv4 } from 'uuid'

// Define types for a toast
interface Toast {
    id: string;
    message: string;
    type: 'info' | 'success' | 'warning' | 'error';
    duration?: number;
}

const Toast: FC = () => {
  const [toasts, setToasts] = useState<Toast[]>([])

  useEffect(() => {
    const handleAddToast = (event: CustomEvent<Toast>) => {
      const { message, type, duration } = event.detail

      // Add the new toast
      const id = uuidv4()
      setToasts((prev) => [...prev, { id, message, type, duration }])

      // Auto-remove if a duration is specified
      if (duration) {
        setTimeout(() => {
          setToasts((prev) => prev.filter((toast) => toast.id !== id))
        }, duration)
      }
    }

    // Listen for the custom event
    window.addEventListener('add-toast', handleAddToast as EventListener)

    return () => {
      // Cleanup event listener on component unmount
      window.removeEventListener('add-toast', handleAddToast as EventListener)
    }
  }, [])

  // Remove toast manually
  const handleClose = (id: string) => {
    setToasts((prev) => prev.filter((toast) => toast.id !== id))
  }

  return (
    <div className={'fixed bottom-16 right-4 flex flex-col gap-2 z-50'}>
      {toasts.map((toast) => (
        <div
          key={toast.id}
          className={`relative flex items-center justify-between gap-3 px-4 py-3 pr-10 rounded-md shadow-md text-white
            ${toast.type === 'success' ? 'bg-green-500' : ''}
            ${toast.type === 'error' ? 'bg-red-500' : ''}
            ${toast.type === 'warning' ? 'bg-yellow-500' : ''}
            ${toast.type === 'info' ? 'bg-blue-500' : ''}
          `}
        >
          <span>{toast.message}</span>
          <button
            onClick={() => handleClose(toast.id)}
            className={'absolute top-2 right-2 text-xl font-bold text-white hover:opacity-75'}
          >&times;</button>
        </div>
      ))}
    </div>
  )
}

export {
  Toast
}