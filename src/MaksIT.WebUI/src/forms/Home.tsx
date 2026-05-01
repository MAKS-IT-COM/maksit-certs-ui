import { FC, useCallback, useEffect, useState } from 'react'
import { FormContainer, FormContent, FormFooter, FormHeader } from '../components/FormLayout'
import { ButtonComponent, CheckBoxComponent, RadioGroupComponent, SelectBoxComponent } from '../components/editors'
import { GetAccountResponse } from '../models/letsEncryptServer/account/responses/GetAccountResponse'
import { deleteData, getData, postData } from '../axiosConfig'
import { ApiRoutes, GetApiRoute } from '../AppMap'
import { enumToArr, formatISODateString } from '../functions'
import { addToast } from '../components/Toast/addToast'
import { Offcanvas } from '../components/Offcanvas'
import { EditAccount } from './EditAccount'
import { ChallengeType } from '../entities/ChallengeType'


const Home: FC = () => {
  const [rawd, setRawd] = useState<GetAccountResponse[]>([])
  const [accountId, setAccountId] = useState<string | undefined>(undefined)
  
  const loadData = useCallback(() => {
    getData<GetAccountResponse[]>(GetApiRoute(ApiRoutes.ACCOUNTS_GET).route).then((response) => {
      if (!response.ok || !response.payload) return
      setRawd(response.payload)
    })
  }, [])

  useEffect(() => {
    loadData()
  }, [loadData])

  const handleDeleteAccount = (accountId: string) => {
    deleteData<void>(
      GetApiRoute(ApiRoutes.ACCOUNT_DELETE)
        .route.replace('{accountId}', accountId)
    ).then(response => {
      if (!response.ok) return
      setRawd(rawd.filter((account) => account.accountId !== accountId))
    })
  }

  const handleEditCancel = () => {
    setAccountId(undefined)
  }

  const handleRedeployCerts = (accountId: string) => {
    postData<void, { [key: string]: string }>(GetApiRoute(ApiRoutes.CERTS_FLOW_CERTIFICATES_APPLY).route
      .replace('{accountId}', accountId)
    ).then(response => {
      if (!response.ok || !response.payload?.message) return

      addToast(response.payload.message, 'info')
    })
  }

  const handleOnSubmitted = (_: GetAccountResponse) => {
    setAccountId(undefined)
    loadData()
  }

  return <>
    <FormContainer>
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
                  <h2 className={'col-span-6'}>
                  Account: {acc.accountId}
                  </h2>
                  <ButtonComponent
                    colspan={2}
                    onClick={() => handleDeleteAccount(acc.accountId)}
                    label={'Delete Account'}
                    buttonHierarchy={'error'}
                  />
                  <ButtonComponent
                    colspan={2}
                    children={'Redeploy certs'}
                    buttonHierarchy={'success'}
                    onClick={() => handleRedeployCerts(acc.accountId)}
                  />
                  <ButtonComponent
                    colspan={2}
                    onClick={() => setAccountId(acc.accountId)}
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
                  <SelectBoxComponent
                    label={'Challenge Type'}
                    options={enumToArr(ChallengeType)
                      .map(ct => ({ value: ct.value, label: ct.displayValue }))
                      .filter(ct => ct.value !== ChallengeType.dns01)}
                    value={acc.challengeType}
                    disabled={true}
                  />
                  <h3 className={'col-span-12'}>Hostnames:</h3>
                  <ul className={'col-span-12'}>
                    {acc.hostnames?.map((hostname) => (
                      <li key={hostname.hostname} className={'grid grid-cols-12 gap-4 w-full pb-2'}>
                        <span className={'col-span-3'}>{hostname.hostname}</span>
                        <span className={'col-span-3'}>Exp: {formatISODateString(hostname.expires)}</span>
                        <span className={'col-span-3'}>
                          <span className={`${hostname.isUpcomingExpire
                            ? 'bg-yellow-200 text-yellow-800'
                            : 'bg-green-200 text-green-800'}`}>{hostname.isUpcomingExpire
                              ? 'Upcoming'
                              : 'Not Upcoming'}
                          </span>
                        </span>
                        <span className={'col-span-3'}>
                          <label className={'mr-2'}>Disabled:</label>
                          <input
                            type={'checkbox'}
                            checked={hostname.isDisabled}
                            disabled={true}
                          />
                        </span>
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

    <Offcanvas isOpen={accountId !== undefined}>
      {accountId && <EditAccount
        accountId={accountId}
        cancelEnabled={true}
        onSubmitted={handleOnSubmitted}
        onCancel={handleEditCancel}
      />}

    </Offcanvas>  
  </>
}

export { Home }