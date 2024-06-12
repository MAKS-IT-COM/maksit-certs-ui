import React from 'react';
import './loader.css'; // Add your loader styles here

const Loader: React.FC = () => {
  return (
    <div className="loader-overlay">
      <div className="spinner"></div>
      <div className="loading-text">Loading...</div>
    </div>
  );
};

export default Loader;
