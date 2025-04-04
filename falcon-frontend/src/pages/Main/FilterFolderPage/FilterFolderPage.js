import React from "react";
import "./FilterFolderPage.css";
import { ReactComponent as NumberOfEmailIcon } from "../../../assets/icons/black/email-enter-icon.svg";
import { ReactComponent as Dots } from "../../../assets/icons/black/more-dots.svg";

const FilterFolderPage = () => {
  return (
    <div className="folder-grid">
      {/* Replace with real data when ready */}
      {[...Array(6)].map((_, i) => (
        <div className={`folder-card`} key={i}>
          <div className={`folder-top-row color-folder-${i % 6}`}>
            <div>
              <Dots className="folder-dot" />
            </div>
          </div>

          <div className="body-folder">
            <div>
              <p className="folder-title">Filter Folder Name</p>
              <p className="folder-subtext">5 new emails</p>
            </div>
            <div className="folder-count-row">
              <span className="num-email-icon">
                <NumberOfEmailIcon />
              </span>
              <span className="count-num">
                {Math.floor(Math.random() * 200)}
              </span>
            </div>
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
