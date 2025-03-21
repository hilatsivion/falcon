import React from "react";
import { ReactComponent as StarIconEmpty } from "../../assets/icons/black/empty-star.svg";
import { ReactComponent as StarIconFull } from "../../assets/icons/black/full-star.svg";
import { Tag } from "../../pages/Main/Inbox/Inbox";
import "./EmailItem.css";
import "../../styles/global.css";

const EmailItem = ({
  sender,
  subject,
  preview,
  tags,
  time,
  avatarColor,
  isRead,
  isStarred,
  onClick,
  onStarToggle,
}) => {
  return (
    <div
      className={`email-item ${isStarred ? "starred" : ""} ${
        isRead ? "read" : ""
      }`}
      onClick={() => {
        if (onClick) onClick();
      }}
    >
      <div className="email-header">
        <div className="email-sender-container">
          <div
            className="email-avatar"
            style={{ backgroundColor: avatarColor || "#ccc" }}
          >
            {sender.charAt(0).toUpperCase()}
          </div>
          <span className="email-sender">{sender}</span>
        </div>
        <span className="email-time">{time}</span>
      </div>

      <div className="email-body">
        <div className="email-subject">{subject}</div>
        <div className="email-preview">{preview}</div>
      </div>

      <div className="email-footer">
        <div className="email-tags">
          {tags.map((tag, index) => (
            <Tag key={index} name={tag} />
          ))}
        </div>

        <div
          className="email-star"
          onClick={(e) => {
            e.stopPropagation();
            onStarToggle();
          }}
        >
          {isStarred ? <StarIconFull /> : <StarIconEmpty />}
        </div>
      </div>
    </div>
  );
};

export { EmailItem };
