'use client'

import { FormEvent, useCallback, useState } from 'react'
import {
  useValidation,
  isBypass,
  isValidContact,
  isValidHostname
} from '@/hooks/useValidation'
import {
  CustomButton,
  CustomEnumSelect,
  CustomInput,
  CustomRadioGroup
} from '@/controls'
import { FaTrash, FaPlus } from 'react-icons/fa'
import { deepCopy } from '../../functions'
import {
  PostAccountRequest,
  validatePostAccountRequest
} from '@/models/letsEncryptServer/account/requests/PostAccountRequest'
import { useAppDispatch } from '@/redux/store'
import { showToast } from '@/redux/slices/toastSlice'
import { ChallengeTypes } from '@/entities/ChallengeTypes'
import { GetAccountResponse } from '@/models/letsEncryptServer/account/responses/GetAccountResponse'
import { httpService } from '@/services/HttpService'
import { ApiRoutes, GetApiRoute } from '@/ApiRoutes'
import { PageContainer } from '@/components/pageContainer'

const RegisterPage = () => {
  const [account, setAccount] = useState<PostAccountRequest>({
    description: '',
    contacts: [],
    challengeType: ChallengeTypes.http01,
    hostnames: [],
    isStaging: true
  })

  const [newHostname, setNewHostname] = useState('')
  const [newContact, setNewContact] = useState('')

  const dispatch = useAppDispatch()

  const {
    value: description,
    error: descriptionError,
    handleChange: handleDescriptionChange,
    reset: resetDescription
  } = useValidation<string>({
    defaultValue: '',
    externalValue: account.description,
    setExternalValue: (newDescription) => {
      setAccount((prev) => {
        const newAccount = deepCopy(prev)
        newAccount.description = newDescription
        return newAccount
      })
    },
    validateFn: isBypass,
    errorMessage: ''
  })

  const {
    value: contact,
    error: contactError,
    handleChange: handleContactChange,
    reset: resetContact
  } = useValidation<string>({
    defaultValue: '',
    externalValue: newContact,
    setExternalValue: setNewContact,
    validateFn: isValidContact,
    errorMessage: 'Invalid contact. Must be a valid email or phone number.'
  })

  const {
    value: hostname,
    error: hostnameError,
    handleChange: handleHostnameChange,
    reset: resetHostname
  } = useValidation<string>({
    defaultValue: '',
    externalValue: newHostname,
    setExternalValue: setNewHostname,
    validateFn: isValidHostname,
    errorMessage: 'Invalid hostname format.'
  })

  const {
    value: challengeType,
    error: challengeTypeError,
    handleChange: handleChallengeTypeChange,
    reset: resetChallengeType
  } = useValidation<string>({
    defaultValue: ChallengeTypes.http01,
    externalValue: account.challengeType,
    setExternalValue: (newChallengeType) => {
      setAccount((prev) => {
        const newAccount = deepCopy(prev)
        newAccount.challengeType = newChallengeType
        return newAccount
      })
    },
    validateFn: isBypass,
    errorMessage: ''
  })

  const handleAddContact = () => {
    if (contactError !== '') {
      resetContact()
      return
    }

    setAccount((prev) => {
      const newAccount = deepCopy(prev)
      if (!newAccount.contacts.includes(contact)) {
        newAccount.contacts.push(contact)
      }
      return newAccount
    })

    resetContact()
  }

  const handleDeleteContact = (contact: string) => {
    setAccount((prev) => {
      const newAccount = deepCopy(prev)
      newAccount.contacts = newAccount.contacts.filter((c) => c !== contact)
      return newAccount
    })
  }

  const handleAddHostname = () => {
    if (hostnameError !== '') {
      resetHostname()
      return
    }

    setAccount((prev) => {
      const newAccount = deepCopy(prev)
      if (!newAccount.hostnames.includes(hostname)) {
        newAccount.hostnames.push(hostname)
      }
      return newAccount
    })

    resetHostname()
  }

  const handleDeleteHostname = (hostname: string) => {
    setAccount((prev) => {
      const newAccount = deepCopy(prev)
      newAccount.hostnames = newAccount.hostnames.filter((h) => h !== hostname)
      return newAccount
    })
  }

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()

    const errors = validatePostAccountRequest(account)

    if (errors.length > 0) {
      errors.forEach((error) => {
        dispatch(showToast({ message: error, type: 'error' }))
      })
    } else {
      dispatch(
        showToast({ message: 'Request model is valid', type: 'success' })
      )

      httpService
        .post<
          PostAccountRequest,
          GetAccountResponse
        >(GetApiRoute(ApiRoutes.ACCOUNT), account)
        .then((response) => {
          console.log(response)
          dispatch(showToast({ message: 'Account created', type: 'success' }))
        })
    }
  }

  return (
    <PageContainer title="Register LetsEncrypt Account">
      <form onSubmit={handleSubmit}>
        <div className="mb-4">
          <CustomInput
            value={description}
            onChange={handleDescriptionChange}
            placeholder="Account Description"
            type="text"
            error={descriptionError}
            title="Description"
            inputClassName="border p-2 rounded w-full"
            errorClassName="text-red-500 text-sm mt-1"
            className="mr-2 flex-grow"
          />
        </div>
        <div className="mb-4">
          <h3 className="text-xl font-medium mb-2">Contacts:</h3>
          <ul className="list-disc list-inside pl-4 mb-2">
            {account.contacts.map((contact) => (
              <li
                key={contact}
                className="text-gray-700 flex justify-between items-center mb-2"
              >
                {contact}
                <CustomButton
                  type="button"
                  onClick={() => handleDeleteContact(contact)}
                  className="bg-red-500 text-white p-2 rounded ml-2"
                >
                  <FaTrash />
                </CustomButton>
              </li>
            ))}
          </ul>
          <div className="flex items-center mb-4">
            <CustomInput
              value={contact}
              onChange={handleContactChange}
              placeholder="Add contact"
              type="text"
              error={contactError}
              title="New Contact"
              inputClassName="border p-2 rounded w-full"
              errorClassName="text-red-500 text-sm mt-1"
              className="mr-2 flex-grow"
            />
            <CustomButton
              type="button"
              onClick={handleAddContact}
              className="bg-green-500 text-white p-2 rounded ml-2"
            >
              <FaPlus />
            </CustomButton>
          </div>
        </div>
        <div className="mb-4">
          <CustomEnumSelect
            error={challengeTypeError}
            title="Challenge Type"
            enumType={ChallengeTypes}
            selectedValue={challengeType}
            onChange={handleChallengeTypeChange}
            selectBoxClassName="border p-2 rounded w-full"
            errorClassName="text-red-500 text-sm mt-1"
            className="mr-2 flex-grow"
          />
        </div>

        <div className="mb-4">
          <h3 className="text-xl font-medium mb-2">Hostnames:</h3>
          <ul className="list-disc list-inside pl-4 mb-2">
            {account.hostnames.map((hostname) => (
              <li
                key={hostname}
                className="text-gray-700 flex justify-between items-center mb-2"
              >
                {hostname}
                <CustomButton
                  type="button"
                  onClick={() => handleDeleteHostname(hostname)}
                  className="bg-red-500 text-white p-2 rounded ml-2"
                >
                  <FaTrash />
                </CustomButton>
              </li>
            ))}
          </ul>
          <div className="flex items-center">
            <CustomInput
              value={hostname}
              onChange={handleHostnameChange}
              placeholder="Add hostname"
              type="text"
              error={hostnameError}
              title="New Hostname"
              inputClassName="border p-2 rounded w-full"
              errorClassName="text-red-500 text-sm mt-1"
              className="mr-2 flex-grow"
            />
            <CustomButton
              type="button"
              onClick={handleAddHostname}
              className="bg-green-500 text-white p-2 rounded ml-2"
            >
              <FaPlus />
            </CustomButton>
          </div>
        </div>

        <div className="mb-4">
          <CustomRadioGroup
            options={[
              { value: 'staging', label: 'Staging' },
              { value: 'production', label: 'Production' }
            ]}
            initialValue={account.isStaging ? 'staging' : 'production'}
            onChange={(value) => {
              setAccount((prev) => {
                const newAccount = deepCopy(prev)
                newAccount.isStaging = value === 'staging'
                return newAccount
              })
            }}
            title="LetsEncrypt Environment"
            className=""
            radioClassName=""
            errorClassName="text-red-500 text-sm mt-1"
          />
        </div>

        <CustomButton
          type="submit"
          className="bg-green-500 text-white px-3 py-1 rounded"
        >
          Create Account
        </CustomButton>
      </form>
    </PageContainer>
  )
}

export default RegisterPage
