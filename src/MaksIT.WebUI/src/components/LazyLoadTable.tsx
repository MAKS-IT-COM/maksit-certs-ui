import { FC, useEffect, useRef, useState } from 'react'

interface LazyLoadTableColumnProps {
    key: string
    title: string
    dataIndex: string
    renderColumn?: (value: unknown) => React.ReactNode
}

interface LazyLoadTableProps {
    data: Record<string, unknown>[]
    columns: LazyLoadTableColumnProps[]
    loadMore: () => void
    colspan?: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12
}

const LazyLoadTable: FC<LazyLoadTableProps> = (props) => {
  const {
    data,
    columns,
    loadMore,
    colspan = 6
  } = props

  const [selectedRowIndex, setSelectedRowIndex] = useState<number | null>(null)
  const observerRef = useRef<HTMLDivElement | null>(null)
  
  useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting) {
          loadMore()
        }
      },
      { threshold: 1.0 }
    )
  
    if (observerRef.current) {
      observer.observe(observerRef.current)
    }
  
    return () => {
      if (observerRef.current) {
        observer.unobserve(observerRef.current)
      }
    }
  }, [loadMore])
  
  const handleRowClick = (index: number) => {
    setSelectedRowIndex(selectedRowIndex === index ? null : index)
  }
  
  return (
    <div className={`col-span-${colspan}`}>
      <table className={'w-full border-collapse'}>
        <thead>
          <tr>
            {columns.map((column, index) => (
              <th key={index} className={'bg-gray-50 text-left py-2 px-4 border-b'}>{column.title}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {data.map((row, index) => (
            <tr
              key={index}
              className={`hover:bg-gray-100 ${selectedRowIndex === index ? 'bg-blue-100' : ''} transition-colors`}
              onClick={() => handleRowClick(index)}
            >
              {columns.map((column, colIndex) => (
                <td className={'py-2 px-4 border-b'} key={colIndex}>
                  {column.renderColumn
                    ? column.renderColumn(row[column.dataIndex])
                    : String(row[column.dataIndex] ?? '')}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
      <div ref={observerRef} className={'text-center py-4'}>Loading more...</div>
    </div>
  )
}

export { LazyLoadTable }