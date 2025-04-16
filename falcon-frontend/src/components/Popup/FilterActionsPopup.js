import React from "react";
import "./FilterActionsPopup.css";
import { ReactComponent as EditIcon } from "../../assets/icons/black/edit-icon.svg";
import { ReactComponent as TrashIcon } from "../../assets/icons/black/trash-red-icon.svg";
import { ReactComponent as XIcon } from "../../assets/icons/black/x.svg";

const FilterActionsPopup = ({ onClose, onEdit, onDelete }) => {
  return (
    <div className="popup-overlay">
      <div className="popup-box">
        <div className="popup-header">
          <h2>Filter Actions</h2>
          <button className="close-btn" onClick={onClose}>
            <XIcon />
          </button>
        </div>

        <div className="popup-actions">
          <button className="edit-btn" onClick={onEdit}>
            <EditIcon />
            <span>Edit Filter</span>
          </button>
          <button className="delete-btn" onClick={onDelete}>
            <TrashIcon />
            <span>Delete Filter</span>
          </button>
        </div>
      </div>
    </div>
  );
};

export default FilterActionsPopup;
