"use client"

import { ApiRoutes, GetApiRoute } from "@/ApiRoutes"
import { httpService } from "@/services/httpService"
import { FormEvent, useEffect, useRef, useState } from "react"
import { useValidation, isValidContact, isValidHostname } from "@/hooks/useValidation"
import { CustomButton, CustomInput } from "@/controls"
import { FaTrash, FaPlus } from "react-icons/fa"
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

const RegisterPage = () => {
    const [accounts, setAccounts] = useState<CacheAccount[]>([])
    const [initialAccounts, setInitialAccounts] = useState<CacheAccount[]>([])
    const [description, setDescription] = useState("")
    const [contacts, setContacts] = useState<string[]>([])
    const [hostnames, setHostnames] = useState<string[]>([])

    const {
        value: newContact,
        error: contactError,
        handleChange: handleContactChange,
        reset: resetContact
    } = useValidation({
        initialValue: "",
        validateFn: isValidContact,
        errorMessage: "Invalid contact. Must be a valid email or phone number."
    })

    const {
        value: newHostname,
        error: hostnameError,
        handleChange: handleHostnameChange,
        reset: resetHostname
    } = useValidation({
        initialValue: "",
        validateFn: isValidHostname,
        errorMessage: "Invalid hostname format."
    })

    const init = useRef(false)

    useEffect(() => {
        if (init.current) return

        const fetchAccounts = async () => {
            const newAccounts: CacheAccount[] = []
            const accounts = await httpService.get<GetAccountResponse[]>(GetApiRoute(ApiRoutes.CACHE_ACCOUNTS))

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
            })

            setAccounts(newAccounts)
            setInitialAccounts(JSON.parse(JSON.stringify(newAccounts))) // Clone initial state
        }

        fetchAccounts()
        init.current = true
    }, [])

    const handleAddContact = () => {
        if (newContact.trim() !== "" && !contactError) {
            setContacts([...contacts, newContact.trim()])
            resetContact()
        }
    }

    const handleAddHostname = () => {
        if (newHostname.trim() !== "" && !hostnameError) {
            setHostnames([...hostnames, newHostname.trim()])
            resetHostname()
        }
    }

    const handleDeleteContact = (contact: string) => {
        setContacts(contacts.filter(c => c !== contact))
    }

    const handleDeleteHostname = (hostname: string) => {
        setHostnames(hostnames.filter(h => h !== hostname))
    }

    const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault()

        if (!description || contacts.length === 0 || hostnames.length === 0) {
            return
        }

        const newAccount = {
            description,
            contacts,
            hostnames: hostnames.map(hostname => ({ hostname, expires: new Date(), isUpcomingExpire: false }))
        }

        // TODO: Implement API call to create new account
        console.log("New account data:", newAccount)

        // Reset form fields
        setDescription("")
        setContacts([])
        setHostnames([])
    }

    return (
        <div className="container mx-auto p-4">
            <h1 className="text-4xl font-bold text-center mb-8">Register LetsEncrypt Account</h1>
            <form onSubmit={handleSubmit}>
                <div className="mb-4">
                    <CustomInput
                        type="text"
                        value={description}
                        onChange={(e) => setDescription(e.target.value)}
                        placeholder="Account Description"
                        title="Description"
                        inputClassName="border p-2 rounded w-full"
                        className="mb-4"
                    />
                </div>
                <div className="mb-4">
                    <h3 className="text-xl font-medium mb-2">Contacts:</h3>
                    <ul className="list-disc list-inside pl-4 mb-2">
                        {contacts.map(contact => (
                            <li key={contact} className="text-gray-700 flex justify-between items-center mb-2">
                                {contact}
                                <button type="button" onClick={() => handleDeleteContact(contact)} className="bg-red-500 text-white px-2 py-1 rounded ml-4">
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
                        <button type="button" onClick={handleAddContact} className="bg-green-500 text-white p-2 rounded ml-2 h-10 flex items-center">
                            <FaPlus />
                        </button>
                    </div>
                </div>
                <div className="mb-4">
                    <h3 className="text-xl font-medium mb-2">Hostnames:</h3>
                    <ul className="list-disc list-inside pl-4 mb-2">
                        {hostnames.map(hostname => (
                            <li key={hostname} className="text-gray-700 flex justify-between items-center mb-2">
                                {hostname}
                                <button type="button" onClick={() => handleDeleteHostname(hostname)} className="bg-red-500 text-white px-2 py-1 rounded ml-4">
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
                        <button type="button" onClick={handleAddHostname} className="bg-green-500 text-white p-2 rounded ml-2 h-10 flex items-center">
                            <FaPlus />
                        </button>
                    </div>
                </div>
                <CustomButton type="submit" className="bg-green-500 text-white px-3 py-1 rounded">
                    Create Account
                </CustomButton>
            </form>
        </div>
    )
}

export default RegisterPage
