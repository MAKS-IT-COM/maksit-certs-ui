import { FC, ReactNode } from 'react'

/** Wraps filter toolbar + table + footer in one bordered panel (dense admin list layout). */
interface VaultStyleListSectionProps {
  title?: string
  description?: string
  toolbar: ReactNode
  /** Optional second strip (e.g. create form) below filters. */
  secondaryToolbar?: ReactNode
  children: ReactNode
}

const VaultStyleListSection: FC<VaultStyleListSectionProps> = (props) => {
  const { title, description, toolbar, secondaryToolbar, children } = props

  return (
    <div className={'rounded-lg border border-neutral-200 bg-white shadow-sm'}>
      {(title || description) && (
        <div className={'border-b border-neutral-200 px-4 py-3'}>
          {title ? <h2 className={'text-base font-semibold text-neutral-900'}>{title}</h2> : null}
          {description ? <p className={'mt-1 text-sm text-neutral-600'}>{description}</p> : null}
        </div>
      )}
      <div className={'border-b border-neutral-200 bg-neutral-50 px-4 py-3'}>{toolbar}</div>
      {secondaryToolbar ? (
        <div className={'border-b border-neutral-200 bg-white px-4 py-3'}>{secondaryToolbar}</div>
      ) : null}
      {children}
    </div>
  )
}

export { VaultStyleListSection }
