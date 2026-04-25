import { FC } from 'react'
import { ButtonComponent } from '../editors'

export interface VaultStyleListFooterProps {
  pageNumber: number
  pageSize: number
  totalRecords: number
  loading: boolean
  onPrevious: () => void
  onNext: () => void
}

/** Footer: “Showing a–b of n” + prev/next — same information pattern as Vault paged lists. */
const VaultStyleListFooter: FC<VaultStyleListFooterProps> = (props) => {
  const { pageNumber, pageSize, totalRecords, loading, onPrevious, onNext } = props

  const size = Math.max(1, pageSize)
  const total = Math.max(0, totalRecords)
  const from = total === 0 ? 0 : (pageNumber - 1) * size + 1
  const to = total === 0 ? 0 : Math.min(pageNumber * size, total)
  const totalPages = Math.max(1, Math.ceil(total / size))

  return (
    <div
      className={
        'mt-0 flex flex-col gap-3 border-t border-neutral-200 bg-neutral-50 px-3 py-3 sm:flex-row sm:items-center sm:justify-between'
      }
    >
      <p className={'text-sm text-neutral-600'}>
        Showing <span className={'font-medium text-neutral-800'}>{from}</span>–
        <span className={'font-medium text-neutral-800'}>{to}</span> of{' '}
        <span className={'font-medium text-neutral-800'}>{total}</span>
        <span className={'ml-2 text-neutral-400'}>
          (page {pageNumber} of {totalPages})
        </span>
      </p>
      <div className={'flex flex-wrap items-center gap-2'}>
        <ButtonComponent
          label={'Previous'}
          buttonHierarchy={'secondary'}
          disabled={loading || pageNumber <= 1}
          onClick={onPrevious}
        />
        <ButtonComponent
          label={'Next'}
          buttonHierarchy={'secondary'}
          disabled={loading || pageNumber >= totalPages || total === 0}
          onClick={onNext}
        />
      </div>
    </div>
  )
}

export { VaultStyleListFooter }
