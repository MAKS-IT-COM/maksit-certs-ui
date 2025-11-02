import React, { FC } from 'react'
import {
  FaHome,
  FaUserPlus,
  FaBars,
  FaSyncAlt,
  FaThermometerHalf
} from 'react-icons/fa'
import Link from 'next/link'

interface SideMenuProps {
  isCollapsed: boolean
  toggleSidebar: () => void
}

const menuItems = [
  { icon: <FaSyncAlt />, label: 'Auto Renew', path: '/' },
  { icon: <FaUserPlus />, label: 'Register', path: '/register' },
  { icon: <FaThermometerHalf />, label: 'Utils', path: '/utils' }
]

const SideMenu: FC<SideMenuProps> = ({ isCollapsed, toggleSidebar }) => {
  return (
    <div
      className={`flex flex-col bg-gray-800 text-white transition-all duration-300 ${isCollapsed ? 'w-16' : 'w-64'} h-full`}
    >
      <div className="flex items-center h-16 bg-gray-900 relative">
        <button onClick={toggleSidebar} className="absolute left-4">
          <FaBars />
        </button>
        <h1
          className={`${isCollapsed ? 'hidden' : 'block'} text-2xl font-bold ml-12`}
        >
          Certs UI
        </h1>
      </div>
      <nav className="flex-1">
        <ul>
          {menuItems.map((item, index) => (
            <li key={index} className="hover:bg-gray-700">
              <Link href={item.path} className="flex items-center w-full p-4">
                <span className={`${isCollapsed ? 'mr-0' : 'mr-4'}`}>
                  {item.icon}
                </span>
                <span className={`${isCollapsed ? 'hidden' : 'block'}`}>
                  {item.label}
                </span>
              </Link>
            </li>
          ))}
        </ul>
      </nav>
    </div>
  )
}

export { SideMenu }
