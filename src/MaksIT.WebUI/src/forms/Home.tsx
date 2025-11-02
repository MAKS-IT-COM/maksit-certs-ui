import { FC, useEffect, useState } from 'react'
import { FormContainer, FormContent, FormFooter, FormHeader } from '../components/FormLayout'
import { ButtonComponent, CheckBoxComponent, RadioGroupComponent, SelectBoxComponent } from '../components/editors'
import { CacheAccount } from '../entities/CacheAccount'
import { GetAccountResponse } from '../models/letsEncryptServer/account/responses/GetAccountResponse'
import { deleteData, getData } from '../axiosConfig'
import { ApiRoutes, GetApiRoute } from '../AppMap'
import { enumToArr, formatISODateString } from '../functions'
import { ChallengeType } from '../entities/ChallengeType'
import { Radio } from 'lucide-react'


const Home: FC = () => {
  const [rawd, setRawd] = useState<GetAccountResponse[]>([])
  const [editingAccount, setEditingAccount] = useState<GetAccountResponse | null>(
    null
  )
  
  useEffect(() => {
    console.log(GetApiRoute(ApiRoutes.ACCOUNTS).route)

    getData<GetAccountResponse []>(GetApiRoute(ApiRoutes.ACCOUNTS).route).then((response) => {
      if (!response) return

      setRawd(response)
    })

  }, [])

  const handleAccountUpdate = (updatedAccount: CacheAccount) => {
    // setAccounts(
    //   accounts.map((account) =>
    //     account.accountId === updatedAccount.accountId
    //       ? updatedAccount
    //       : account
    //   )
    // )
  }

  const deleteAccount = (accountId: string) => {
    deleteData<void>(
      GetApiRoute(ApiRoutes.ACCOUNT_DELETE)
        .route.replace('{accountId}', accountId)
    ).then((result) => {
      if (!result) return

      setRawd(rawd.filter((account) => account.accountId !== accountId))
    })
  }

  return <FormContainer>
    <FormHeader>Home</FormHeader>
    <FormContent>
      <div className={'grid grid-cols-12 gap-4 w-full'}>
        {rawd.length === 0 ?
          <div className={'text-center text-gray-600 col-span-12'}>
            No accounts registered.
          </div> :
          rawd.map((acc) => (
            <div key={acc.accountId} className={'bg-white shadow-lg rounded-lg p-6 mb-6 col-span-12'}>
              <div className={'grid grid-cols-12 gap-4 w-full'}>
                <h2 className={'col-span-8'}>
                  Account: {acc.accountId}
                </h2>
                <ButtonComponent
                  colspan={2}
                  onClick={() => deleteAccount(acc.accountId)}
                  label={'Delete'}
                  buttonHierarchy={'error'}
                />
                <ButtonComponent
                  colspan={2}
                  onClick={() => setEditingAccount(acc)}
                  label={'Edit'}
                />
                <h3 className={'col-span-12'}>
                  Description: {acc.description}
                </h3>
                <CheckBoxComponent
                  colspan={12}
                  value={acc.isDisabled}
                  label={'Disabled'}
                  disabled={true}
                />
                <h3 className={'col-span-12'}>Contacts:</h3>
                <ul className={'col-span-12'}>
                  {acc.contacts.map((contact) => (
                    <li key={contact} className={'pb-2'}>
                      {contact}
                    </li>
                  ))}
                </ul>
                <RadioGroupComponent
                  colspan={12}
                  label={'LetsEncrypt Environment'}
                  options={[
                    { value: 'staging', label: 'Staging' },
                    { value: 'production', label: 'Production' }
                  ]}

                  value={acc.challengeType ? 'staging' : 'production'}
                  disabled={true}
                />
                <h3 className={'col-span-12'}>Hostnames:</h3>
                <ul className={'col-span-12'}>
                  {acc.hostnames?.map((hostname) => (
                    <li key={hostname.hostname} className={'grid grid-cols-12 gap-4 w-full pb-2'}>
                      <span className={'col-span-3'}>{hostname.hostname}</span>
                      <span className={'col-span-3'}>{formatISODateString(hostname.expires)}</span>
                      <span className={'col-span-3'}>
                        <span className={`${hostname.isUpcomingExpire
                          ? 'bg-yellow-200 text-yellow-800'
                          : 'bg-green-200 text-green-800'}`}>{hostname.isUpcomingExpire
                            ? 'Upcoming'
                            : 'Not Upcoming'}
                        </span>
                      </span>
                       
                      <CheckBoxComponent
                        colspan={3}
                        value={hostname.isDisabled}
                        label={'Disabled'}
                        disabled={true}
                      />
                      
                    </li>
                  ))}
                </ul>

                <SelectBoxComponent
                  label={'Environment'}
                  options={[
                    { value: 'production', label: 'Production' },
                    { value: 'staging', label: 'Staging' }
                  ]}
                  value={acc.isStaging ? 'staging' : 'production'}
                  disabled={true}
                />
              </div>
            </div>
          ))}

      </div>
     



   
    </FormContent>
    <FormFooter />
  </FormContainer>
}

export { Home }