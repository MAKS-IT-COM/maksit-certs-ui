'use client'

import { ApiRoutes, GetApiRoute } from '@/ApiRoutes'
import { httpService } from '@/services/httpService'
import { FormEvent, useEffect, useRef, useState } from 'react'
import {
  CustomButton,
  CustomCheckbox,
  CustomEnumSelect,
  CustomInput,
  CustomRadioGroup
} from '@/controls'
import { GetAccountResponse } from '@/models/letsEncryptServer/account/responses/GetAccountResponse'
import { deepCopy, enumToArray } from '../functions'
import { CacheAccount } from '@/entities/CacheAccount'
import { ChallengeTypes } from '@/entities/ChallengeTypes'
import { FaPlus, FaTrash } from 'react-icons/fa'
import { PageContainer } from '@/components/pageContainer'
import { OffCanvas } from '@/components/offcanvas'
import { AccountEdit } from '@/partials/accoutEdit'

export default function Page() {
  const [accounts, setAccounts] = useState<CacheAccount[]>([])
  const [editingAccount, setEditingAccount] = useState<CacheAccount | null>(
    null
  )

  const init = useRef(false)

  useEffect(() => {
    if (init.current) return

    console.log('Fetching accounts')

    const fetchAccounts = async () => {
      const newAccounts: CacheAccount[] = []
      const accounts = await httpService.get<GetAccountResponse[]>(
        GetApiRoute(ApiRoutes.ACCOUNTS)
      )

      accounts?.forEach((account) => {
        newAccounts.push({
          accountId: account.accountId,
          isDisabled: account.isDisabled,
          description: account.description,
          contacts: account.contacts.map((contact) => contact),
          challengeType: account.challengeType,
          hostnames:
            account.hostnames?.map((hostname) => ({
              hostname: hostname.hostname,
              expires: new Date(hostname.expires),
              isUpcomingExpire: hostname.isUpcomingExpire,
              isDisabled: hostname.isDisabled
            })) ?? [],
          isStaging: account.isStaging,
          isEditMode: false
        })
      })

      setAccounts(newAccounts)
    }

    fetchAccounts()
    init.current = true
  }, [])

  useEffect(() => {
    console.log(editingAccount)
  }, [editingAccount])

  const handleAccountUpdate = (updatedAccount: CacheAccount) => {
    setAccounts(
      accounts.map((account) =>
        account.accountId === updatedAccount.accountId
          ? updatedAccount
          : account
      )
    )
  }

  const deleteAccount = (accountId: string) => {
    setAccounts(accounts.filter((account) => account.accountId !== accountId))

    // TODO: Revoke all certificates
    // TODO: Remove from cache
  }

  return (
    <>
      <PageContainer title="LetsEncrypt Auto Renew">
        {accounts.map((account) => (
          <div
            key={account.accountId}
            className="bg-white shadow-lg rounded-lg p-6 mb-6"
          >
            <div className="flex justify-between items-center mb-4">
              <h2 className="text-2xl font-semibold">
                Account: {account.accountId}
              </h2>
              <CustomButton
                onClick={() => {
                  setEditingAccount(account)
                }}
                className="bg-blue-500 text-white px-3 py-1 rounded"
              >
                Edit
              </CustomButton>
            </div>

            <div className="mb-4">
              <h3 className="text-xl font-medium mb-2">
                Description: {account.description}
              </h3>
            </div>

            <div className="mb-4">
              <CustomCheckbox
                checked={account.isDisabled}
                label="Disabled"
                disabled={true}
              />
            </div>

            <div className="mb-4">
              <h3 className="text-xl font-medium mb-2">Contacts:</h3>
              <ul className="list-disc list-inside pl-4 mb-2">
                {account.contacts.map((contact) => (
                  <li key={contact} className="text-gray-700 mb-2">
                    {contact}
                  </li>
                ))}
              </ul>
            </div>
            <div className="mb-4">
              <CustomEnumSelect
                title="Challenge Type"
                enumType={ChallengeTypes}
                selectedValue={account.challengeType}
                onChange={(option) =>
                  //handleChallengeTypeChange(account.accountId, option)
                  console.log('')
                }
                disabled={true}
              />
            </div>
            <div className="mb-4">
              <h3 className="text-xl font-medium mb-2">Hostnames:</h3>
              <ul className="list-disc list-inside pl-4 mb-2">
                {account.hostnames?.map((hostname) => (
                  <li key={hostname.hostname} className="text-gray-700 mb-2">
                    <div className="inline-flex">
                      {hostname.hostname} - {hostname.expires.toDateString()} -
                      <span
                        className={`ml-2 px-2 py-1 rounded ${
                          hostname.isUpcomingExpire
                            ? 'bg-yellow-200 text-yellow-800'
                            : 'bg-green-200 text-green-800'
                        }`}
                      >
                        {hostname.isUpcomingExpire
                          ? 'Upcoming'
                          : 'Not Upcoming'}
                      </span>
                      <CustomCheckbox
                        checked={hostname.isDisabled}
                        label="Disabled"
                        disabled={true}
                      />
                    </div>
                  </li>
                ))}
              </ul>
            </div>

            <div className="mb-4">
              <CustomRadioGroup
                options={[
                  { value: 'staging', label: 'Staging' },
                  { value: 'production', label: 'Production' }
                ]}
                initialValue={account.isStaging ? 'staging' : 'production'}
                title="LetsEncrypt Environment"
                className=""
                radioClassName=""
                errorClassName="text-red-500 text-sm mt-1"
                disabled={true}
              />
            </div>
          </div>
        ))}
      </PageContainer>

      <OffCanvas
        title="Edit Account"
        isOpen={editingAccount !== null}
        onClose={() => setEditingAccount(null)}
      >
        {editingAccount && <AccountEdit account={editingAccount} />}
      </OffCanvas>
    </>
  )
}
