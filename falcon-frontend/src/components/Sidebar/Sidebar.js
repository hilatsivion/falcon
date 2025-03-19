import React, { useState, useEffect } from "react";
import { Link } from "react-router-dom";
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
    setIsClosing(true);
    setTimeout(() => {
      closeSidebar();
    }, 300);
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
          <Link to="/inbox" onClick={(e) => e.stopPropagation()}>
            <FalconLogo />
          </Link>

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
