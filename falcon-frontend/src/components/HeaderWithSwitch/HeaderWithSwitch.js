import React from "react";
import { useLocation } from "react-router-dom";
import listIcon from "../../assets/icons/black/list.svg";
import folderIcon from "../../assets/icons/black/folder.svg";
import backIcon from "../../assets/icons/black/arrow-left-full.svg";
import "./HeaderWithSwitch.css";

const HeaderWithSwitch = ({
  title,
  isListView,
  onToggleView,
  showBackButton,
  onBack,
  colorBar,
}) => {
  const location = useLocation();

  const determineTitle = () => {
    if (!isListView && !location.pathname.startsWith("/filters/")) {
      return "Filters";
    }

    if (location.pathname === "/inbox") return "Inbox";
    if (location.pathname === "/unread") return "Unread";
    if (location.pathname === "/favorite") return "Favorite";
    if (location.pathname === "/sent") return "Sent";
    if (location.pathname === "/search-results") return "Results";
    if (location.pathname === "/filter-results") return "Filtered Results";
    if (location.pathname.startsWith("/filters/")) return "Filter Folder";

    return "Inbox";
  };

  return (
    <div className="header-wrapper">
      <div
        className="space-between-full-wid bottom-line-grey"
        id="header-inbox"
      >
        <div className="header-left">
          {showBackButton && (
            <img
              src={backIcon}
              alt="Back"
              onClick={onBack}
              className="back-icon"
              style={{ cursor: "pointer", marginRight: "12px", height: "20px" }}
            />
          )}
          <h1>{title || determineTitle()}</h1>
        </div>

        {onToggleView && (
          <div className="switch-button" onClick={onToggleView}>
            <div className={`switch-circle ${isListView ? "left" : "right"}`} />
            <img
              src={listIcon}
              alt="List"
              className={`switch-icon ${isListView ? "active" : "inactive"}`}
            />
            <img
              src={folderIcon}
              alt="Folder"
              className={`switch-icon ${isListView ? "inactive" : "active"}`}
            />
          </div>
        )}
      </div>
      {colorBar && (
        <div className="header-color-bar" style={{ background: colorBar }} />
      )}
    </div>
  );
};

export default HeaderWithSwitch;
