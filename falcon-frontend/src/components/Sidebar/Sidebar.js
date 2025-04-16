import React, { useState, useEffect } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { API_BASE_URL } from "../../config/constants";
import { useAuth } from "../../context/AuthContext";

import { ReactComponent as XIcon } from "../../assets/icons/black/x.svg";
import { ReactComponent as FalconLogo } from "../../assets/images/Falcon-sidebar.svg";
import { ReactComponent as AnalyticsIcon } from "../../assets/icons/black/analytics-icon-sidebar.svg";
import { ReactComponent as AnalyticsIconBlue } from "../../assets/icons/blue/analytics-icon-sidebar-blue.svg";
import { ReactComponent as InboxIcon } from "../../assets/icons/black/inbox-icon-sidebar.svg";
import { ReactComponent as InboxIconBlue } from "../../assets/icons/blue/inbox-icon-sidebar-blue.svg";
import { ReactComponent as SentIcon } from "../../assets/icons/black/sent-icon-sidebar.svg";
import { ReactComponent as SentIconBlue } from "../../assets/icons/blue/sent-icon-sidebar-blue.svg";
import { ReactComponent as LogoutIcon } from "../../assets/icons/black/sign-out-icon.svg";
import { ReactComponent as StaredIcon } from "../../assets/icons/black/stared-icon-sidebar.svg";
import { ReactComponent as StaredIconBlue } from "../../assets/icons/blue/stared-icon-sidebar-blue.svg";
import { ReactComponent as UnreadIcon } from "../../assets/icons/black/unread-icon-sidebar.svg";
import { ReactComponent as UnreadIconBlue } from "../../assets/icons/blue/unread-icon-sidebar-blue.svg";

import "./Sidebar.css";
import ConfirmPopup from "../Popup/ConfirmPopup";
import { getOrCreateAvatarColor, getUserInitial } from "../../utils/avatar";

const Sidebar = ({ isOpen, closeSidebar }) => {
  const { logout } = useAuth();
  const [isClosing, setIsClosing] = useState(false);
  const [userData, setUserData] = useState({ fullName: "", email: "" });
  const navigate = useNavigate();
  const location = useLocation();
  const currentPath = location.pathname;
  const [showLogoutPopup, setShowLogoutPopup] = useState(false);

  const avatarColor = getOrCreateAvatarColor();
  const userInitial = getUserInitial(userData.fullName);

  const confirmLogout = () => {
    setShowLogoutPopup(true);
  };

  useEffect(() => {
    const fetchUserProfile = async () => {
      try {
        const response = await fetch(`${API_BASE_URL}/api/auth/profile`, {
          headers: {
            Authorization: `Bearer ${localStorage.getItem("token")}`,
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
          sessionStorage.removeItem("avatarColor"); // remove avatar color
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
            <Link
              to="/inbox"
              className={`sidebar-item ${
                currentPath === "/inbox" ? "active" : ""
              }`}
            >
              {currentPath === "/inbox" ? <InboxIconBlue /> : <InboxIcon />}
              <span>Inbox</span>
            </Link>

            <Link
              to="/unread"
              className={`sidebar-item ${
                currentPath === "/unread" ? "active" : ""
              }`}
            >
              {currentPath === "/unread" ? <UnreadIconBlue /> : <UnreadIcon />}
              <span>Unread Emails</span>
            </Link>

            <Link
              to="/favorite"
              className={`sidebar-item ${
                currentPath === "/favorite" ? "active" : ""
              }`}
            >
              {currentPath === "/favorite" ? (
                <StaredIconBlue />
              ) : (
                <StaredIcon />
              )}
              <span>Favorite Emails</span>
            </Link>

            <Link
              to="/sent"
              className={`sidebar-item ${
                currentPath === "/sent" ? "active" : ""
              }`}
            >
              {currentPath === "/sent" ? <SentIconBlue /> : <SentIcon />}
              <span>Sent</span>
            </Link>

            <Link
              to="/analytics"
              className={`sidebar-item ${
                currentPath === "/analytics" ? "active" : ""
              }`}
            >
              {currentPath === "/analytics" ? (
                <AnalyticsIconBlue />
              ) : (
                <AnalyticsIcon />
              )}
              <span>Analytics</span>
            </Link>
          </div>

          <div className="sidebar-footer">
            <div className="sidebar-item logout" onClick={confirmLogout}>
              <LogoutIcon className="sidebar-icon" />
              <span>Logout</span>
            </div>

            <div className="sidebar-user">
              <div
                className="user-avatar"
                style={{ backgroundColor: avatarColor }}
              >
                {userInitial}
              </div>
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
