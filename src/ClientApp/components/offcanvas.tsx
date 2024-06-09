import React, { FC } from 'react';

interface OffCanvasProps {
    isOpen: boolean;
    onClose: () => void;
}

const OffCanvas: FC<OffCanvasProps> = ({ isOpen, onClose }) => {
  return (
    <div
      className={`fixed inset-0 bg-gray-800 bg-opacity-50 z-50 transform transition-transform duration-300 ${
        isOpen ? 'translate-x-0' : 'translate-x-full'
      }`}
      onClick={onClose}
    >
      <div
        className="absolute top-0 right-0 bg-white w-64 h-full shadow-lg"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="p-4">
          <h2 className="text-xl font-bold">Settings</h2>
          <button onClick={onClose} className="mt-4 text-red-500">
            Close
          </button>
        </div>
        {/* Your off-canvas content goes here */}
      </div>
    </div>
  );
};

export {
    OffCanvas
};
