import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import "./FilterFolderPage.css";
import NewFilterPopup from "./NewFilterPopup";
import { ReactComponent as NumberOfEmailIcon } from "../../../assets/icons/black/email-enter-icon.svg";
import { ReactComponent as Dots } from "../../../assets/icons/black/more-dots.svg";

// demo data. delete after server functions are done
const dummyFilters = [
  {
    id: "f1",
    name: "Work",
    newEmailsCount: 5,
    totalEmails: 87,
    color: "color1", // --> --folder-green-1
  },
  {
    id: "f2",
    name: "School",
    newEmailsCount: 2,
    totalEmails: 21,
    color: "color2", // --> --folder-green-2
  },
  {
    id: "f3",
    name: "Promotions",
    newEmailsCount: 9,
    totalEmails: 90,
    color: "color3", // --> --folder-orange
  },
  {
    id: "f4",
    name: "Personal",
    newEmailsCount: 3,
    totalEmails: 5,
    color: "color4", // --> --folder-purple
  },
  {
    id: "f5",
    name: "Updates",
    newEmailsCount: 0,
    totalEmails: 192,
    color: "color5", // --> --folder-blue
  },
  {
    id: "f6",
    name: "Family",
    newEmailsCount: 1,
    totalEmails: 5,
    color: "color6", // --> --folder-yellow
  },
];

const FilterFolderPage = ({ setIsListView }) => {
  const [isPopupOpen, setIsPopupOpen] = useState(false);
  const navigate = useNavigate();

  const handleFilterClick = (filter) => {
    navigate(`/filters/${filter.id}`, { state: filter });
    setIsListView(true); // 👈 this will switch back to email list view
  };

  return (
    <>
      <div className="folder-grid">
        {dummyFilters.map((filter) => (
          <div
            className={`folder-card`}
            key={filter.id}
            onClick={() => handleFilterClick(filter)}
          >
            <div className={`folder-top-row ${filter.color}`}>
              <Dots className="folder-dot" />
            </div>

            <div className="body-folder">
              <div>
                <p className="folder-title">{filter.name}</p>
                <p className="folder-subtext">
                  {filter.newEmailsCount} new emails
                </p>
              </div>
              <div className="folder-count-row">
                <span className="num-email-icon">
                  <NumberOfEmailIcon />
                </span>
                <span className="count-num">{filter.totalEmails}</span>
              </div>
            </div>
          </div>
        ))}

        {/* Add new filter button */}
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
            setIsPopupOpen(false);
          }}
        />
      )}
    </>
  );
};

export default FilterFolderPage;
