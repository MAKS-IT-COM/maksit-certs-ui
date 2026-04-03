import { FC, ReactNode } from 'react'

interface FormContentProps {
    children?: ReactNode
    /** Merged after base layout; use e.g. `flex flex-col overflow-hidden` when a child should fill height (iframe). */
    className?: string
}

const FormContent: FC<FormContentProps> = (props) => {
  const {
    children,
    className
  } = props

  const base = 'bg-gray-100 w-full h-full min-h-0 p-4'
  return <div className={className ? `${base} ${className}` : `${base} overflow-y-auto`}>
    {children}
  </div>
}

export {
  FormContent
}