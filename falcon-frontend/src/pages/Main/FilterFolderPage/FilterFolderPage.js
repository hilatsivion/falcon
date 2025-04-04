import React from "react";
import "./FilterFolderPage.css";

const FilterFolderPage = () => {
  return (
    <div className="folder-grid">
      {/* Replace with real data when ready */}
      {[...Array(6)].map((_, i) => (
        <div className={`folder-card color-${i % 6}`} key={i}>
          <div className="folder-top-row">
            <p className="folder-title">Filter Folder Name</p>
            <div className="folder-dot">•••</div>
          </div>
          <p className="folder-subtext">5 new emails</p>
          <div className="folder-count-row">
            <span className="icon">📅</span>
            <span className="count">{Math.floor(Math.random() * 200)}</span>
          </div>
        </div>
      ))}

      {/* Add Folder card */}
      <div className="add-folder-card dashed">
        <div className="plus-circle">＋</div>
        <p className="add-folder-text">Add filter folder</p>
      </div>
    </div>
  );
};

export default FilterFolderPage;
