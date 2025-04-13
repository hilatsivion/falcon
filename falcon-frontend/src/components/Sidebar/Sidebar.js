import React, { useState, useEffect } from "react";
import { Link } from "react-router-dom";
import { API_BASE_URL } from "../../config/constants";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { toast } from "react-toastify";

import { ReactComponent as XIcon } from "../../assets/icons/black/x.svg";
import { ReactComponent as FalconLogo } from "../../assets/images/Falcon-sidebar.svg";
import { ReactComponent as AnalyticsIcon } from "../../assets/icons/black/analytics-icon-sidebar.svg";
import { ReactComponent as InboxIcon } from "../../assets/icons/black/inbox-icon-sidebar.svg";
import { ReactComponent as SentIcon } from "../../assets/icons/black/sent-icon-sidebar.svg";
import { ReactComponent as LogoutIcon } from "../../assets/icons/black/sign-out-icon.svg";
import { ReactComponent as StaredIcon } from "../../assets/icons/black/stared-icon-sidebar.svg";
import { ReactComponent as UnreadIcon } from "../../assets/icons/black/unread-icon-sidebar.svg";
import "./Sidebar.css";
import ConfirmPopup from "../Popup/ConfirmPopup";

const Sidebar = ({ isOpen, closeSidebar }) => {
  const [isClosing, setIsClosing] = useState(false);
  const [userData, setUserData] = useState({ fullName: "", email: "" });
  const { logout } = useAuth();
  const navigate = useNavigate();
  const [showLogoutPopup, setShowLogoutPopup] = useState(false);

  const confirmLogout = () => {
    setShowLogoutPopup(true);
  };

  useEffect(() => {
    const fetchUserProfile = async () => {
      try {
        const response = await fetch(`${API_BASE_URL}/api/auth/profile`, {
          headers: {
            Authorization: `Bearer ${localStorage.getItem("token")}`, // Or however you store your token
          },
        });

        if (!response.ok) {
          throw new Error("Failed to fetch user profile");
        }

        const data = await response.json();
        setUserData({ fullName: data.fullName, email: data.email });
      } catch (error) {
        console.error("Error fetching user profile:", error);
      }
    };

    fetchUserProfile();
  }, []);

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
    <>
      <ConfirmPopup
        isOpen={showLogoutPopup}
        message="Are you sure you want to logout?"
        confirmText="Logout"
        cancelText="Cancel"
        onConfirm={() => {
          logout();
          navigate("/login");
        }}
        onCancel={() => setShowLogoutPopup(false)}
      />

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

          <div className="sidebar-menu">
            <Link to="/inbox" className="sidebar-item">
              <InboxIcon className="sidebar-icon" />
              <span>Inbox</span>
            </Link>

            <Link to="/unread" className="sidebar-item">
              <UnreadIcon className="sidebar-icon" />
              <span>Unread Emails</span>
            </Link>

            <Link to="/important" className="sidebar-item">
              <StaredIcon className="sidebar-icon" />
              <span>Important Emails</span>
            </Link>

            <Link to="/sent" className="sidebar-item">
              <SentIcon className="sidebar-icon" />
              <span>Sent</span>
            </Link>

            <Link to="/analytics" className="sidebar-item">
              <AnalyticsIcon className="sidebar-icon" />
              <span>Analytics</span>
            </Link>
          </div>

          <div className="sidebar-footer">
            <div className="sidebar-item logout" onClick={confirmLogout}>
              <LogoutIcon className="sidebar-icon" />
              <span>Logout</span>
            </div>

            <div className="sidebar-user">
              <div className="user-avatar" />
              <div className="user-info">
                <div className="user-name">
                  {userData.fullName || "Loading..."}
                </div>
                <div className="user-email">{userData.email || ""}</div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
};

export default Sidebar;
