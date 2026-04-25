import { useEffect, useMemo, useState } from 'react'
import { getDataWithoutLoader } from '../../axiosConfig'
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
  const [remoteLabel, setRemoteLabel] = useState<string>('')

  const label = useMemo(() => {
    if (props.type !== 'normal') {
      return remoteLabel
    }

    const { value = '', dataType = 'string' } = props

    switch (dataType) {
    case 'date':
      return formatISODateString(value)
    case 'string':
    default:
      return value
    }
  }, [props, remoteLabel])

  useEffect(() => {
    if (props.type !== 'remote') {
      return
    }

    const { route, accessorKey } = props

    getDataWithoutLoader<T>(route)
      .then(response => {
        if (!response) return

        setRemoteLabel(response[accessorKey])
      }).finally(() => {})
  }, [props])

  return <p>{label}</p>
}

export {
  DataTableLabel
}
