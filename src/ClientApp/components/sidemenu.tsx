import React, { FC } from 'react';
import { FaHome, FaUser, FaCog, FaBars } from 'react-icons/fa';

interface SideMenuProps {
  isCollapsed: boolean;
  toggleSidebar: () => void;
}

const SideMenu: FC<SideMenuProps> = ({ isCollapsed, toggleSidebar }) => {
  return (
    <div className={`flex flex-col bg-gray-800 text-white transition-all duration-300 ${isCollapsed ? 'w-16' : 'w-64'} h-full`}>
      <div className="flex items-center h-16 bg-gray-900 relative">
        {/* <button onClick={toggleSidebar} className="absolute left-4">
          <FaBars />
        </button> */}
        <h1 className={`${isCollapsed ? 'hidden' : 'block'} text-2xl font-bold ml-12`}>Logo</h1>
      </div>
      <nav className="flex-1">
        <ul>
          <li className="flex items-center p-4 hover:bg-gray-700">
            <FaHome className="mr-4" />
            <span className={`${isCollapsed ? 'hidden' : 'block'}`}>Home</span>
          </li>
          <li className="flex items-center p-4 hover:bg-gray-700">
            <FaUser className="mr-4" />
            <span className={`${isCollapsed ? 'hidden' : 'block'}`}>Profile</span>
          </li>
          <li className="flex items-center p-4 hover:bg-gray-700">
            <FaCog className="mr-4" />
            <span className={`${isCollapsed ? 'hidden' : 'block'}`}>Settings</span>
          </li>
        </ul>
      </nav>
    </div>
  );
};

export {
  SideMenu
};
