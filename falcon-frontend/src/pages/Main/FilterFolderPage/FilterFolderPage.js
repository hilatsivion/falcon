import React, { useState } from "react";
import "./FilterFolderPage.css";
import NewFilterPopup from "./NewFilterPopup";
import { ReactComponent as NumberOfEmailIcon } from "../../../assets/icons/black/email-enter-icon.svg";
import { ReactComponent as Dots } from "../../../assets/icons/black/more-dots.svg";

const FilterFolderPage = () => {
  const [isPopupOpen, setIsPopupOpen] = useState(false);

  return (
    <>
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
        <div
          className="add-folder-card dashed"
          onClick={() => setIsPopupOpen(true)}
        >
          <div className="plus-circle">＋</div>
          <p className="add-folder-text">Add filter folder</p>
        </div>
      </div>

      {isPopupOpen && (
        <NewFilterPopup
          onClose={() => setIsPopupOpen(false)}
          onSave={(data) => {
            console.log("Folder data to send to server:", data);
            // You can also add it to a state array if you want to preview the new folder
            setIsPopupOpen(false);
          }}
        />
      )}
    </>
  );
};

export default FilterFolderPage;
