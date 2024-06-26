import React from 'react'
import {
  CustomSelect,
  CustomSelectOption,
  CustomSelectPropsBase
} from './customSelect'
import { enumToArray } from '@/app/functions'

interface CustomEnumSelectProps extends CustomSelectPropsBase {
  enumType: any
}

const CustomEnumSelect: React.FC<CustomEnumSelectProps> = (props) => {
  const { enumType, ...customSelectProps } = props

  const options = enumToArray(enumType).map((item) => {
    const option: CustomSelectOption = {
      value: `${item.value}`,
      label: item.key
    }

    return option
  })

  return <CustomSelect options={options} {...customSelectProps} />
}

export { CustomEnumSelect }
