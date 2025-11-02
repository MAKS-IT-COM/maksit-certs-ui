import { useEffect, useState } from 'react'
import { getData } from '../../axiosConfig'
import { useAppDispatch } from '../../redux/hooks'
import { formatISODateString } from '../../functions'

interface NormalLabelProps {
  type: 'normal',
  value?: string,
  dataType?: 'string' | 'date'
}

interface RemoteLabelProps {
  type: 'remote',
  route: string,
  columnId: string,
  accessorKey: string,
}

type LabelProps = NormalLabelProps | RemoteLabelProps
  
const DataTableLabel = <T extends { [key: string]: never }>(props: LabelProps) => {
  const dispatch = useAppDispatch()

  const [label, setLabel] = useState<string>('')

  useEffect(() => {
    const { type } = props
    
    if (type === 'normal') {
      const { value = '', dataType = 'string' } = props as NormalLabelProps

      switch (dataType) {
      case 'date':
        setLabel(formatISODateString(value))
        break
      case 'string':
      default:
        setLabel(value)
        break
      }
    }

    if (type === 'remote') {
      const { route, accessorKey } = props as RemoteLabelProps

      getData<T>(route)
        .then(response => {
          if (!response) return
          
          setLabel(response[accessorKey])
        }).finally(() => {
          
        })
    }
    
  }, [props, dispatch])
  
  return <p>{label}</p>
}

export {
  DataTableLabel
}