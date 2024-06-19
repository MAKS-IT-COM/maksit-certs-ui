"use client"

import { ApiRoutes, GetApiRoute } from "@/ApiRoutes"
import { httpService } from "@/services/httpService"
import { FormEvent, useEffect, useRef, useState } from "react"
import { useValidation, isValidEmail, isValidHostname } from "@/hooks/useValidation"
import { CustomButton, CustomInput } from "@/controls"
import { TrashIcon, PlusIcon } from "@heroicons/react/24/solid"
import { GetAccountResponse } from "@/models/letsEncryptServer/cache/responses/GetAccountResponse"

interface CacheAccountHostname {
    hostname: string
    expires: Date
    isUpcomingExpire: boolean
}

interface CacheAccount {
    accountId: string
    description?: string
    contacts: string[]
    hostnames: CacheAccountHostname[]
    isEditMode: boolean
}

export default function Page() {
    const [accounts, setAccounts] = useState<CacheAccount[]>([])
    const [initialAccounts, setInitialAccounts] = useState<CacheAccount[]>([])

    const {
        value: newContact,
        error: contactError,
        handleChange: handleContactChange
    } = useValidation({
        initialValue:"",
        validateFn: isValidEmail,
        errorMessage: "Invalid email format."
    })
    const {
        value: newHostname,
        error: hostnameError,
        handleChange: handleHostnameChange
    } = useValidation({
        initialValue: "",
        validateFn: isValidHostname,
        errorMessage: "Invalid hostname format."})

    const init = useRef(false)

    useEffect(() => {
        if (init.current) return


        console.log("Fetching accounts")

        const fetchAccounts = async () => {
            const newAccounts: CacheAccount[] = []
            const accounts = await httpService.get<GetAccountResponse []>(GetApiRoute(ApiRoutes.CACHE_ACCOUNTS))

            accounts?.forEach((account) => {
                newAccounts.push({
                    accountId: account.accountId,
                    contacts: account.contacts,
                    hostnames: account.hostnames.map(h => ({
                        hostname: h.hostname,
                        expires: new Date(h.expires),
                        isUpcomingExpire: h.isUpcomingExpire
                    })),
                    isEditMode: false
                })
            });

            setAccounts(newAccounts)
            setInitialAccounts(JSON.parse(JSON.stringify(newAccounts))) // Clone initial state
        }

        fetchAccounts()
        init.current = true
    }, [])

    const toggleEditMode = (accountId: string) => {
        setAccounts(accounts.map(account =>
            account.accountId === accountId ? { ...account, isEditMode: !account.isEditMode } : account
        ))
    }

    const deleteAccount = (accountId: string) => {
        setAccounts(accounts.filter(account => account.accountId !== accountId))

        // TODO: Revoke all certificates
        // TODO: Remove from cache
    }

    const deleteContact = (accountId: string, contact: string) => {
        const account = accounts.find(account => account.accountId === accountId)
        if (account?.contacts.length ?? 0 < 1) return

        // TODO: Remove from cache
        httpService.delete(GetApiRoute(ApiRoutes.CACHE_ACCOUNT_CONTACT, accountId, contact))

        setAccounts(accounts.map(account =>
            account.accountId === accountId
                ? { ...account, contacts: account.contacts.filter(c => c !== contact) }
                : account
        ))
    }

    const addContact = (accountId: string) => {
        if (newContact.trim() === "" || contactError) {
            return
        }

        if (accounts.find(account => account.accountId === accountId)?.contacts.includes(newContact.trim()))
            return

        setAccounts(accounts.map(account =>
            account.accountId === accountId
                ? { ...account, contacts: [...account.contacts, newContact.trim()] }
                : account
        ))
        handleContactChange("")
    }

    const deleteHostname = (accountId: string, hostname: string) => {
        const account = accounts.find(account => account.accountId === accountId)
        if (account?.hostnames.length ?? 0 < 1) return

        // TODO: Revoke certificate
        // TODO: Remove from cache

        setAccounts(accounts.map(account =>
            account.accountId === accountId
                ? { ...account, hostnames: account.hostnames.filter(h => h.hostname !== hostname) }
                : account
        ))
    }

    const addHostname = (accountId: string) => {
        if (newHostname.trim() === "" || hostnameError) {
            return
        }

        if (accounts.find(account => account.accountId === accountId)?.hostnames.some(h => h.hostname === newHostname.trim()))
            return

        setAccounts(accounts.map(account =>
            account.accountId === accountId
                ? { ...account, hostnames: [...account.hostnames, { hostname: newHostname.trim(), expires: new Date(), isUpcomingExpire: false }] }
                : account
        ))
        handleHostnameChange("")
    }

    const handleSubmit = async (e: FormEvent<HTMLFormElement>, accountId: string) => {
        e.preventDefault()

        const account = accounts.find(acc => acc.accountId === accountId)
        const initialAccount = initialAccounts.find(acc => acc.accountId === accountId)

        if (!account || !initialAccount) return

        const contactChanges = {
            added: account.contacts.filter(contact => !initialAccount.contacts.includes(contact)),
            removed: initialAccount.contacts.filter(contact => !account.contacts.includes(contact))
        }

        const hostnameChanges = {
            added: account.hostnames.filter(hostname => !initialAccount.hostnames.some(h => h.hostname === hostname.hostname)),
            removed: initialAccount.hostnames.filter(hostname => !account.hostnames.some(h => h.hostname === hostname.hostname))
        }

        // Handle contact changes
        if (contactChanges.added.length > 0) {
            // TODO: POST new contacts
            console.log("Added contacts:", contactChanges.added)
        }
        if (contactChanges.removed.length > 0) {
            // TODO: DELETE removed contacts
            console.log("Removed contacts:", contactChanges.removed)
        }

        // Handle hostname changes
        if (hostnameChanges.added.length > 0) {
            // TODO: POST new hostnames
            console.log("Added hostnames:", hostnameChanges.added)
        }
        if (hostnameChanges.removed.length > 0) {
            // TODO: DELETE removed hostnames
            console.log("Removed hostnames:", hostnameChanges.removed)
        }

        // Save current state as initial state
        setInitialAccounts(JSON.parse(JSON.stringify(accounts)))
        toggleEditMode(accountId)
    }

    return (
        <div className="container mx-auto p-4">
            <h1 className="text-4xl font-bold text-center mb-8">LetsEncrypt Auto Renew</h1>
            {
                accounts.map(account => (
                    <div key={account.accountId} className="bg-white shadow-lg rounded-lg p-6 mb-6">
                        <div className="flex justify-between items-center mb-4">
                            <h2 className="text-2xl font-semibold">Account: {account.accountId}</h2>
                            <CustomButton onClick={() => toggleEditMode(account.accountId)} className="bg-blue-500 text-white px-3 py-1 rounded">
                                {account.isEditMode ? "View Mode" : "Edit Mode"}
                            </CustomButton>
                        </div>
                        {account.isEditMode ? (
                            <form onSubmit={(e) => handleSubmit(e, account.accountId)}>
                                <div className="mb-4">
                                    <h3 className="text-xl font-medium mb-2">Description:</h3>
                                </div>
                                <div className="mb-4">
                                    <h3 className="text-xl font-medium mb-2">Contacts:</h3>
                                    <ul className="list-disc list-inside pl-4 mb-2">
                                        {
                                            account.contacts.map(contact => (
                                                <li key={contact} className="text-gray-700 flex justify-between items-center mb-2">
                                                    {contact}
                                                    <button onClick={() => deleteContact(account.accountId, contact)} className="bg-red-500 text-white px-2 py-1 rounded ml-4 h-10">
                                                        <TrashIcon className="h-5 w-5 text-white" />
                                                    </button>
                                                </li>
                                            ))
                                        }
                                    </ul>
                                    <div className="flex items-center mb-4">
                                        <CustomInput
                                            value={newContact}
                                            onChange={handleContactChange}
                                            placeholder="Add new contact"
                                            type="email"
                                            error={contactError}
                                            title="New Contact"
                                            inputClassName="border p-2 rounded w-full"
                                            errorClassName="text-red-500 text-sm mt-1"
                                            className="mr-2 flex-grow"
                                        />
                                        <button onClick={() => addContact(account.accountId)} className="bg-green-500 text-white p-2 rounded ml-2 h-10 flex items-center">
                                            <PlusIcon className="h-5 w-5 text-white" />
                                        </button>
                                    </div>
                                </div>
                                <div>
                                    <h3 className="text-xl font-medium mb-2">Hostnames:</h3>
                                    <ul className="list-disc list-inside pl-4 mb-2">
                                        {
                                            account.hostnames.map(hostname => (
                                                <li key={hostname.hostname} className="text-gray-700 flex justify-between items-center mb-2">
                                                    <div>
                                                        {hostname.hostname} - {hostname.expires.toDateString()} -
                                                        <span className={`ml-2 px-2 py-1 rounded ${hostname.isUpcomingExpire ? 'bg-yellow-200 text-yellow-800' : 'bg-green-200 text-green-800'}`}>
                                                            {hostname.isUpcomingExpire ? 'Upcoming' : 'Not Upcoming'}
                                                        </span>
                                                    </div>
                                                    <button onClick={() => deleteHostname(account.accountId, hostname.hostname)} className="bg-red-500 text-white px-2 py-1 rounded ml-4 h-10">
                                                        <TrashIcon className="h-5 w-5 text-white" />
                                                    </button>
                                                </li>
                                            ))
                                        }
                                    </ul>
                                    <div className="flex items-center">
                                        <CustomInput
                                            value={newHostname}
                                            onChange={handleHostnameChange}
                                            placeholder="Add new hostname"
                                            type="text"
                                            error={hostnameError}
                                            title="New Hostname"
                                            inputClassName="border p-2 rounded w-full"
                                            errorClassName="text-red-500 text-sm mt-1"
                                            className="mr-2 flex-grow"
                                        />
                                        <button onClick={() => addHostname(account.accountId)} className="bg-green-500 text-white p-2 rounded ml-2 h-10 flex items-center">
                                            <PlusIcon className="h-5 w-5 text-white" />
                                        </button>
                                    </div>
                                </div>
                                <div className="flex justify-between mt-4">
                                    <button onClick={() => deleteAccount(account.accountId)} className="bg-red-500 text-white px-3 py-1 rounded">
                                        <TrashIcon className="h-5 w-5 text-white" />
                                    </button>
                                    <CustomButton type="submit" className="bg-green-500 text-white px-3 py-1 rounded">
                                        Submit
                                    </CustomButton>
                                </div>
                            </form>
                        ) : (
                            <>
                                <div className="mb-4">
                                    <h3 className="text-xl font-medium mb-2">Description:</h3>
                                </div>
                                <div className="mb-4">
                                    <h3 className="text-xl font-medium mb-2">Contacts:</h3>
                                    <ul className="list-disc list-inside pl-4 mb-2">
                                        {
                                            account.contacts.map(contact => (
                                                <li key={contact} className="text-gray-700 mb-2">
                                                    {contact}
                                                </li>
                                            ))
                                        }
                                    </ul>
                                </div>
                                <div>
                                    <h3 className="text-xl font-medium mb-2">Hostnames:</h3>
                                    <ul className="list-disc list-inside pl-4 mb-2">
                                        {
                                            account.hostnames.map(hostname => (
                                                <li key={hostname.hostname} className="text-gray-700 mb-2">
                                                    {hostname.hostname} - {hostname.expires.toDateString()} -
                                                    <span className={`ml-2 px-2 py-1 rounded ${hostname.isUpcomingExpire ? 'bg-yellow-200 text-yellow-800' : 'bg-green-200 text-green-800'}`}>
                                                        {hostname.isUpcomingExpire ? 'Upcoming' : 'Not Upcoming'}
                                                    </span>
                                                </li>
                                            ))
                                        }
                                    </ul>
                                </div>
                            </>
                        )}
                    </div>
                ))
            }
        </div>
    )
}
