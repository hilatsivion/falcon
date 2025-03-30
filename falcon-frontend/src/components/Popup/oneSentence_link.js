import React from "react";
import "./popup.css";

const OneSentenceLink = ({ message, linkText, onLinkClick, onClose }) => {
  return (
    <div className="popup-overlay">
      <div className="popup-box">
        <h2 className="popup-message">{message}</h2>
        <p className="popup-link" onClick={onLinkClick}>
          {linkText}
        </p>
      </div>
    </div>
  );
};

export default OneSentenceLink;
