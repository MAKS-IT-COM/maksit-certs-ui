"use client"; // Add this line

import React, { FC, useState } from 'react';
import { FaCog, FaBars } from 'react-icons/fa';
import Link from 'next/link';

interface TopMenuProps {
  onToggleOffCanvas: () => void;
}

const TopMenu: FC<TopMenuProps> = ({ onToggleOffCanvas }) => {
  const [isMenuOpen, setIsMenuOpen] = useState(false);

  const toggleMenu = () => {
    setIsMenuOpen(!isMenuOpen);
  };

  return (
    <header className="bg-gray-900 text-white flex items-center p-4">
      <nav className="flex-1 flex justify-between items-center">
        <ul className="hidden md:flex space-x-4">
          <li className="hover:bg-gray-700 p-2 rounded">
            <Link href="/">Home</Link>
          </li>
          <li className="hover:bg-gray-700 p-2 rounded">
            <Link href="/about">About</Link>
          </li>
          <li className="hover:bg-gray-700 p-2 rounded">
            <Link href="/contact">Contact</Link>
          </li>
        </ul>
        <button onClick={toggleMenu} className="md:hidden">
          <FaBars />
        </button>
        {isMenuOpen && (
          <ul className="absolute top-16 right-0 bg-gray-900 w-48 md:hidden">
            <li className="hover:bg-gray-700 p-2">
              <Link href="/">Home</Link>
            </li>
            <li className="hover:bg-gray-700 p-2">
              <Link href="/about">About</Link>
            </li>
            <li className="hover:bg-gray-700 p-2">
              <Link href="/contact">Contact</Link>
            </li>
          </ul>
        )}
      </nav>
      <button onClick={onToggleOffCanvas} className="ml-4">
        <FaCog />
      </button>
    </header>
  );
};

export {
  TopMenu
};
