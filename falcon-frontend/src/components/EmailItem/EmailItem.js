import React, { useState } from "react";
import { ReactComponent as StarIconEmpty } from "../../assets/icons/black/empty-star.svg";
import { ReactComponent as StarIconFull } from "../../assets/icons/black/full-star.svg";
import "./EmailItem.css";
import "../../styles/global.css";

const tagColors = {
  Inbox: "#cbd5ff",
  Social: "#c8facc",
  School: "#f6d6b8",
  Work: "#b8ebf6",
  Personal: "#ffb3c6",
  Finance: "#ffd700",
  Promotions: "#ff9f43",
  Updates: "#6c757d",
  Forums: "#28a745",
  Travel: "#007bff",
};

const Tag = ({ name }) => (
  <span
    className="email-tag"
    style={{ backgroundColor: tagColors[name] || "#ddd" }}
  >
    {name}
  </span>
);

const EmailItem = ({ sender, subject, preview, tags, time, avatarColor }) => {
  const [isStarred, setIsStarred] = useState(false);
  const [isRead, setIsRead] = useState(false);

  const handleStarClick = (event) => {
    // event.stopPropagation(); // Prevent triggering read when clicking the star
    setIsStarred((prev) => !prev);
  };

  const handleEmailClick = () => {
    setIsRead(true); // Mark email as read
  };

  return (
    <div
      className={`email-item ${isStarred ? "starred" : ""} ${
        isRead ? "read" : ""
      }`}
      onClick={handleEmailClick}
    >
      {/* Sender Name + Time */}
      <div className="email-header">
        {/* Avatar + Sender */}
        <div className="email-sender-container">
          <div
            className="email-avatar"
            style={{ backgroundColor: avatarColor || "#ccc" }}
          >
            {sender.charAt(0).toUpperCase()}
          </div>
          <span className="email-sender">{sender}</span>
        </div>

        {/* Time Received */}
        <span className="email-time">{time}</span>
      </div>

      {/* Subject + Email Body */}
      <div className="email-body">
        <div className="email-subject">{subject}</div>
        <div className="email-preview">{preview}</div>
      </div>

      {/* Tags List + Star Icon */}
      <div className="email-footer">
        {/* Tags List */}
        <div className="email-tags">
          {tags.map((tag, index) => (
            <Tag key={index} name={tag} />
          ))}
        </div>

        {/* Star Icon */}
        <div className="email-star" onClick={handleStarClick}>
          {isStarred ? <StarIconFull /> : <StarIconEmpty />}
        </div>
      </div>
    </div>
  );
};

export { EmailItem, Tag };
