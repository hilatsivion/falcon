import React from "react";
import { useLocation } from "react-router-dom";
import { ReactComponent as ListIcon } from "../../assets/icons/black/list.svg";
import { ReactComponent as FolderIcon } from "../../assets/icons/black/folder.svg";
import { ReactComponent as BackIcon } from "../../assets/icons/black/arrow-left-full.svg";
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
            <BackIcon
              alt="Back"
              onClick={onBack}
              className="back-icon"
              style={{ marginRight: "12px", height: "20px" }}
            />
          )}
          <h1>{title || determineTitle()}</h1>
        </div>

        {onToggleView &&
          !["/unread", "/favorite", "/sent"].includes(location.pathname) && (
            <div className="switch-button" onClick={onToggleView}>
              <div
                className={`switch-circle ${isListView ? "left" : "right"}`}
              />
              <ListIcon
                alt="List"
                className={`switch-icon ${isListView ? "active" : "inactive"}`}
              />
              <FolderIcon
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
