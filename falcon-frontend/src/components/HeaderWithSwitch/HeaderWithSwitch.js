import React from "react";
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
  colorBar, // 🎨 פס צבע אם קיים
}) => {
  return (
    <div className="header-wrapper">
      <div
        id="header-inbox"
        className="space-between-full-wid bottom-line-grey"
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
          <h1>{title}</h1>
        </div>

        {onToggleView && (
          <div className="switch-button" onClick={onToggleView}>
            <div
              className={`switch-circle ${isListView ? "left" : "right"}`}
            ></div>
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
        <div
          className="header-color-bar"
          style={{ background: colorBar, height: "6px", width: "100%" }}
        />
      )}
    </div>
  );
};

export default HeaderWithSwitch;
