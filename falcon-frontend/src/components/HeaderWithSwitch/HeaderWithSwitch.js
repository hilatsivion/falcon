import React from "react";
import listIcon from "../../assets/icons/black/list.svg";
import folderIcon from "../../assets/icons/black/folder.svg";
import "./HeaderWithSwitch.css";

const HeaderWithSwitch = ({ title, isListView, onToggleView }) => {
  return (
    <div id="header-inbox" className="space-between-full-wid bottom-line-grey">
      <h1>{title}</h1>
      <div className="switch-button" onClick={onToggleView}>
        <div className={`switch-circle ${isListView ? "left" : "right"}`}></div>
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
    </div>
  );
};

export default HeaderWithSwitch;
