'use client'

import { ApiRoutes, GetApiRoute } from '@/ApiRoutes'
import { httpService } from '@/services/httpService'
import { FormEvent, useEffect, useRef, useState } from 'react'
import {
  useValidation,
  isValidContact,
  isValidHostname
} from '@/hooks/useValidation'
import { CustomButton, CustomInput } from '@/controls'
import { FaTrash, FaPlus } from 'react-icons/fa'
import { GetAccountResponse } from '@/models/letsEncryptServer/cache/responses/GetAccountResponse'
import { deepCopy } from '../functions'

interface CacheAccount {
  description?: string
  contacts: string[]
  hostnames: string[]
}

const RegisterPage = () => {
  const [account, setAccount] = useState<CacheAccount | null>(null)

  const {
    value: newContact,
    error: contactError,
    handleChange: handleContactChange,
    reset: resetContact
  } = useValidation({
    initialValue: '',
    validateFn: isValidContact,
    errorMessage: 'Invalid contact. Must be a valid email or phone number.'
  })

  const {
    value: newHostname,
    error: hostnameError,
    handleChange: handleHostnameChange,
    reset: resetHostname
  } = useValidation({
    initialValue: '',
    validateFn: isValidHostname,
    errorMessage: 'Invalid hostname format.'
  })

  const init = useRef(false)

  useEffect(() => {
    if (init.current) return

    init.current = true
  }, [])

  const handleDescription = (description: string) => {}

  const handleAddContact = () => {
    if (newContact !== '' || contactError) return

    setAccount((prev) => {
      const newAccount: CacheAccount =
        prev !== null
          ? deepCopy(prev)
          : {
              contacts: [],
              hostnames: []
            }

      newAccount.contacts.push(newContact)

      return newAccount
    })

    resetContact()
  }

  const handleAddHostname = () => {
    if (newHostname !== '' || hostnameError) return

    setAccount((prev) => {
      const newAccount: CacheAccount =
        prev !== null
          ? deepCopy(prev)
          : {
              contacts: [],
              hostnames: []
            }

      newAccount.hostnames.push(newHostname)

      return newAccount
    })

    resetHostname()
  }

  const handleDeleteContact = (contact: string) => {
    setAccount((prev) => {
      if (prev === null) return null

      const newAccount = deepCopy(prev)
      newAccount.contacts = newAccount.contacts.filter((c) => c !== contact)

      return newAccount
    })
  }

  const handleDeleteHostname = (hostname: string) => {
    setAccount((prev) => {
      if (prev === null) return null

      const newAccount = deepCopy(prev)
      newAccount.hostnames = newAccount.hostnames.filter((h) => h !== hostname)

      return newAccount
    })
  }

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()

    console.log(account)
  }

  return (
    <div className="container mx-auto p-4">
      <h1 className="text-4xl font-bold text-center mb-8">
        Register LetsEncrypt Account
      </h1>
      <form onSubmit={handleSubmit}>
        <div className="mb-4">
          <CustomInput
            type="text"
            value={account?.description ?? ''}
            onChange={handleDescription}
            placeholder="Account Description"
            title="Description"
            inputClassName="border p-2 rounded w-full"
            className="mb-4"
          />
        </div>
        <div className="mb-4">
          <h3 className="text-xl font-medium mb-2">Contacts:</h3>
          <ul className="list-disc list-inside pl-4 mb-2">
            {account?.contacts.map((contact) => (
              <li
                key={contact}
                className="text-gray-700 flex justify-between items-center mb-2"
              >
                {contact}
                <button
                  type="button"
                  onClick={() => handleDeleteContact(contact)}
                  className="bg-red-500 text-white px-2 py-1 rounded ml-4"
                >
                  <FaTrash />
                </button>
              </li>
            ))}
          </ul>
          <div className="flex items-center mb-4">
            <CustomInput
              value={newContact}
              onChange={handleContactChange}
              placeholder="Add contact"
              type="text"
              error={contactError}
              title="New Contact"
              inputClassName="border p-2 rounded w-full"
              errorClassName="text-red-500 text-sm mt-1"
              className="mr-2 flex-grow"
            />
            <button
              type="button"
              onClick={handleAddContact}
              className="bg-green-500 text-white p-2 rounded ml-2 h-10 flex items-center"
            >
              <FaPlus />
            </button>
          </div>
        </div>
        <div className="mb-4">
          <h3 className="text-xl font-medium mb-2">Hostnames:</h3>
          <ul className="list-disc list-inside pl-4 mb-2">
            {account?.hostnames.map((hostname) => (
              <li
                key={hostname}
                className="text-gray-700 flex justify-between items-center mb-2"
              >
                {hostname}
                <button
                  type="button"
                  onClick={() => handleDeleteHostname(hostname)}
                  className="bg-red-500 text-white px-2 py-1 rounded ml-4"
                >
                  <FaTrash />
                </button>
              </li>
            ))}
          </ul>
          <div className="flex items-center">
            <CustomInput
              value={newHostname}
              onChange={handleHostnameChange}
              placeholder="Add hostname"
              type="text"
              error={hostnameError}
              title="New Hostname"
              inputClassName="border p-2 rounded w-full"
              errorClassName="text-red-500 text-sm mt-1"
              className="mr-2 flex-grow"
            />
            <button
              type="button"
              onClick={handleAddHostname}
              className="bg-green-500 text-white p-2 rounded ml-2 h-10 flex items-center"
            >
              <FaPlus />
            </button>
          </div>
        </div>
        <CustomButton
          type="submit"
          className="bg-green-500 text-white px-3 py-1 rounded"
        >
          Create Account
        </CustomButton>
      </form>
    </div>
  )
}

export default RegisterPage
