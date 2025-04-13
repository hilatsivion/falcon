import React from "react";
import { ReactComponent as StarIconEmpty } from "../../assets/icons/black/empty-star.svg";
import { ReactComponent as StarIconFull } from "../../assets/icons/black/full-star.svg";
import { Tag } from "../../pages/Main/Inbox/Inbox";
import { formatEmailTime } from "../../utils/formatters";
import "./EmailItem.css";
import "../../styles/global.css";

const EmailItem = ({
  mailId,
  sender,
  subject,
  bodySnippet,
  tags,
  timeReceived,
  avatarColor,
  isRead,
  isFavorite,
  onClick,
  onStarToggle,
}) => {
  const senderInitial = sender ? sender.charAt(0).toUpperCase() : "?";
  const hashCode = (str) => {
    let hash = 0;
    if (!str) return hash;
    for (let i = 0; i < str.length; i++) {
      hash = str.charCodeAt(i) + ((hash << 5) - hash);
    }
    return hash;
  };
  const intToRGB = (i) => {
    const c = (i & 0x00ffffff).toString(16).toUpperCase();
    return "00000".substring(0, 6 - c.length) + c;
  };
  const avatarColorCalculated =
    avatarColor || `#${intToRGB(hashCode(sender || ""))}`;

  const handleItemClick = () => {
    if (onClick && mailId !== undefined) {
      onClick(mailId);
    } else {
      console.warn("EmailItem: onClick handler or mailId prop is missing");
    }
  };

  const handleStarClick = (e) => {
    e.stopPropagation();
    if (onStarToggle && mailId !== undefined) {
      onStarToggle(mailId, !isFavorite);
    } else {
      console.warn("EmailItem: onStarToggle handler or mailId prop is missing");
    }
  };

  const itemClasses = [
    "email-item",
    isRead ? "read" : "",
    isFavorite ? "starred" : "",
  ]
    .filter(Boolean)
    .join(" ");

  const unreadTextStyle = !isRead ? { fontWeight: "bold" } : {};

  return (
    <div className={itemClasses} onClick={handleItemClick}>
      <div className="email-header">
        <div className="email-sender-container">
          <div
            className="email-avatar"
            style={{ backgroundColor: avatarColorCalculated }}
          >
            {senderInitial}
          </div>
          <span className="email-sender" style={unreadTextStyle}>
            {sender}
          </span>
        </div>
        <span className="email-time">{formatEmailTime(timeReceived)}</span>
      </div>

      <div className="email-body">
        <div className="email-subject" style={unreadTextStyle}>
          {subject}
        </div>
        <div className="email-preview">{bodySnippet}</div>
      </div>

      <div className="email-footer">
        <div className="email-tags">
          {Array.isArray(tags) &&
            tags.map((tag, index) => (
              <Tag key={`${mailId}-tag-${index}`} name={tag} />
            ))}
        </div>

        <div className="email-star" onClick={handleStarClick}>
          {isFavorite ? <StarIconFull /> : <StarIconEmpty />}
        </div>
      </div>
    </div>
  );
};

export { EmailItem };
