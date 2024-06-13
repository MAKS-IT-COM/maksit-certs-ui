"use client"; // Add this line

import { ApiRoutes, GetApiRoute } from "@/ApiRoutes";
import { GetAccountsResponse } from "@/models/letsEncryptServer/cache/GetAccountsResponse";
import { GetContactsResponse } from "@/models/letsEncryptServer/cache/GetContactsResponse";
import { GetHostnamesResponse } from "@/models/letsEncryptServer/cache/GetHostnamesResponse";
import { httpService } from "@/services/HttpService";
import { useEffect, useRef, useState } from "react";
import { useValidation, isValidEmail, isValidHostname } from "@/hooks/useValidation"; // Assuming hooks are in a hooks directory

interface CacheAccountHostname {
    hostname: string
    expires: Date,
    isUpcomingExpire: boolean
}

interface CacheAccount {
    accountId: string
    contacts: string[]
    hostnames: CacheAccountHostname[]
}

// `app/page.tsx` is the UI for the `/` URL
export default function Page() {
    const [accounts, setAccounts] = useState<CacheAccount[]>([]);
    const [isEditMode, setIsEditMode] = useState(false);
    const {
        value: newContact,
        error: contactError,
        handleChange: handleContactChange
    } = useValidation("", isValidEmail, "Invalid email format.");
    const {
        value: newHostname,
        error: hostnameError,
        handleChange: handleHostnameChange
    } = useValidation("", isValidHostname, "Invalid hostname format.");

    const init = useRef(false);

    useEffect(() => {
        if (init.current)
            return;

        const fetchAccounts = async () => {
            const newAccounts: CacheAccount[] = [];

            const accountsResponse = await httpService.get<GetAccountsResponse>(GetApiRoute(ApiRoutes.CACHE_GET_ACCOUNTS));
            for (const accountId of accountsResponse.accountIds) {
                const contactsResponse = await httpService.get<GetContactsResponse>(GetApiRoute(ApiRoutes.CACHE_GET_CONTACTS, accountId));
                const hostnamesResponse = await httpService.get<GetHostnamesResponse>(GetApiRoute(ApiRoutes.CACHE_GET_HOSTNAMES, accountId));

                newAccounts.push({
                    accountId: accountId,
                    contacts: contactsResponse.contacts,
                    hostnames: hostnamesResponse.hostnames.map(h => ({
                        hostname: h.hostname,
                        expires: new Date(h.expires),
                        isUpcomingExpire: h.isUpcomingExpire
                    }))
                });
            }

            setAccounts(newAccounts);
        };

        fetchAccounts();
        init.current = true;
    }, []);

    const deleteAccount = (accountId: string) => {
        setAccounts(accounts.filter(account => account.accountId !== accountId));
    }

    const deleteContact = (accountId: string, contact: string) => {
        setAccounts(accounts.map(account =>
            account.accountId === accountId
                ? { ...account, contacts: account.contacts.filter(c => c !== contact) }
                : account
        ));
    }

    const addContact = (accountId: string) => {
        if (newContact.trim() === "" || contactError) {
            return;
        }

        setAccounts(accounts.map(account =>
            account.accountId === accountId
                ? { ...account, contacts: [...account.contacts, newContact.trim()] }
                : account
        ));
        handleContactChange("");
    }

    const deleteHostname = (accountId: string, hostname: string) => {
        setAccounts(accounts.map(account =>
            account.accountId === accountId
                ? { ...account, hostnames: account.hostnames.filter(h => h.hostname !== hostname) }
                : account
        ));
    }

    const addHostname = (accountId: string) => {
        if (newHostname.trim() === "" || hostnameError) {
            return;
        }

        setAccounts(accounts.map(account =>
            account.accountId === accountId
                ? { ...account, hostnames: [...account.hostnames, { hostname: newHostname.trim(), expires: new Date(), isUpcomingExpire: false }] }
                : account
        ));
        handleHostnameChange("");
    }

    useEffect(() => {
        if (isEditMode) {
            handleContactChange(newContact);
            handleHostnameChange(newHostname);
        }
    }, [isEditMode, newContact, newHostname]);

    return (
        <div className="container mx-auto p-4">
            <div className="flex justify-between items-center mb-8">
                <h1 className="text-4xl font-bold text-center">LetsEncrypt Client Dashboard</h1>
                <button
                    onClick={() => setIsEditMode(!isEditMode)}
                    className="bg-blue-500 text-white px-3 py-1 rounded">
                    {isEditMode ? "View Mode" : "Edit Mode"}
                </button>
            </div>
            {
                accounts.map(account => (
                    <div key={account.accountId} className="bg-white shadow-lg rounded-lg p-6 mb-6">
                        <div className="flex justify-between items-center mb-4">
                            <h2 className="text-2xl font-semibold">Account: {account.accountId}</h2>
                            {isEditMode && (
                                <button
                                    onClick={() => deleteAccount(account.accountId)}
                                    className="bg-red-500 text-white px-3 py-1 rounded h-10">
                                    Delete Account
                                </button>
                            )}
                        </div>
                        <div className="mb-4">
                            <h3 className="text-xl font-medium mb-2">Contacts:</h3>
                            <ul className="list-disc list-inside pl-4 mb-2">
                                {
                                    account.contacts.map(contact => (
                                        <li key={contact} className="text-gray-700 flex justify-between items-center mb-2">
                                            {contact}
                                            {isEditMode && (
                                                <button
                                                    onClick={() => deleteContact(account.accountId, contact)}
                                                    className="bg-red-500 text-white px-2 py-1 rounded ml-4 h-10">
                                                    Delete
                                                </button>
                                            )}
                                        </li>
                                    ))
                                }
                            </ul>
                            {isEditMode && (
                                <div className="flex mb-4">
                                    <input
                                        type="text"
                                        value={newContact}
                                        onChange={(e) => handleContactChange(e.target.value)}
                                        className="border p-2 rounded mr-2 flex-grow h-10"
                                        placeholder="Add new contact"
                                    />
                                    <button
                                        onClick={() => addContact(account.accountId)}
                                        className="bg-blue-500 text-white px-3 py-1 rounded h-10">
                                        Add Contact
                                    </button>
                                </div>
                            )}
                            {isEditMode && contactError && <p className="text-red-500">{contactError}</p>}
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
                                            {isEditMode && (
                                                <button
                                                    onClick={() => deleteHostname(account.accountId, hostname.hostname)}
                                                    className="bg-red-500 text-white px-2 py-1 rounded ml-4 h-10">
                                                    Delete
                                                </button>
                                            )}
                                        </li>
                                    ))
                                }
                            </ul>
                            {isEditMode && (
                                <div className="flex">
                                    <input
                                        type="text"
                                        value={newHostname}
                                        onChange={(e) => handleHostnameChange(e.target.value)}
                                        className="border p-2 rounded mr-2 flex-grow h-10"
                                        placeholder="Add new hostname"
                                    />
                                    <button
                                        onClick={() => addHostname(account.accountId)}
                                        className="bg-blue-500 text-white px-3 py-1 rounded h-10">
                                        Add Hostname
                                    </button>
                                </div>
                            )}
                            {isEditMode && hostnameError && <p className="text-red-500">{hostnameError}</p>}
                        </div>
                    </div>
                ))
            }
        </div>
    );
}
