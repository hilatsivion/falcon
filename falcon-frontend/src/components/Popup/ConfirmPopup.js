import React from "react";
import { ReactComponent as XIcon } from "../../assets/icons/black/x.svg";
import "./popup.css";

const ConfirmPopup = ({
  isOpen,
  message = "Are you sure?",
  confirmText = "Confirm",
  cancelText = "Cancel",
  onConfirm,
  onCancel,
}) => {
  if (!isOpen) return null;

  return (
    <div className="popup-overlay">
      <div className="popup-box">
        <button className="popup-close" onClick={onCancel}>
          <XIcon />
        </button>
        <p className="popup-message">{message}</p>
        <div className="popup-buttons">
          <button className="cancel-btn" onClick={onCancel}>
            {cancelText}
          </button>
          <button className="confirm-btn" onClick={onConfirm}>
            {confirmText}
          </button>
        </div>
      </div>
    </div>
  );
};

export default ConfirmPopup;
