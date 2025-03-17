import React, { useState, useEffect } from "react";
import { ReactComponent as XIcon } from "../../assets/icons/black/x.svg";
import { ReactComponent as FalconLogo } from "../../assets/images/Falcon-sidebar.svg";
import "./Sidebar.css";

const Sidebar = ({ isOpen, closeSidebar }) => {
  const [isClosing, setIsClosing] = useState(false);

  useEffect(() => {
    if (!isOpen) {
      setIsClosing(false);
    }
  }, [isOpen]);

  const handleClose = () => {
    setIsClosing(true); // Start closing animation
    setTimeout(() => {
      closeSidebar(); // Hide sidebar completely after animation ends
    }, 300); // Matches transition duration
  };

  return (
    <div
      className={`sidebar-overlay ${isOpen ? "open" : ""}`}
      onClick={handleClose}
    >
      <div
        className={`sidebar-content ${
          isOpen && !isClosing ? "slide-in" : "slide-out"
        }`}
        onClick={(e) => e.stopPropagation()}
      >
        <div className="sidebar-header">
          <FalconLogo />
          <button className="close-btn" onClick={handleClose}>
            <XIcon />
          </button>
        </div>
        {/* Add menu items here */}
      </div>
    </div>
  );
};

export default Sidebar;
