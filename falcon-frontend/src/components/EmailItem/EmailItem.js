import React from "react";
import { ReactComponent as StarIcon } from "../assets/icons/black/empty-star.svg";

// Predefined tag colors
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

// Tag component
const Tag = ({ name }) => {
  return (
    <span
      style={{
        backgroundColor: tagColors[name] || "#ddd",
        color: "#333",
        padding: "4px 8px",
        borderRadius: "8px",
        fontSize: "12px",
        marginRight: "4px",
      }}
    >
      {name}
    </span>
  );
};

// EmailItem component
const EmailItem = ({ sender, subject, preview, tags, time, avatarColor }) => {
  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        padding: "12px",
        borderBottom: "1px solid #ddd",
      }}
    >
      {/* Left Section: Avatar & Email Details */}
      <div style={{ display: "flex", alignItems: "center" }}>
        {/* Sender Avatar */}
        <div
          style={{
            width: "32px",
            height: "32px",
            backgroundColor: avatarColor || "#ccc",
            color: "#fff",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            borderRadius: "50%",
            fontWeight: "bold",
            marginRight: "10px",
          }}
        >
          {sender.charAt(0).toUpperCase()}
        </div>

        {/* Email Info */}
        <div>
          <div style={{ fontWeight: "bold" }}>{sender}</div>
          <div style={{ fontSize: "14px", fontWeight: "bold" }}>{subject}</div>
          <div style={{ fontSize: "12px", color: "#666" }}>{preview}</div>
          {/* Tags */}
          <div style={{ marginTop: "4px" }}>
            {tags.map((tag, index) => (
              <Tag key={index} name={tag} />
            ))}
          </div>
        </div>
      </div>

      {/* Right Section: Time & Star Icon */}
      <div style={{ display: "flex", alignItems: "center" }}>
        <div style={{ fontSize: "12px", color: "#666", marginRight: "10px" }}>
          {time}
        </div>
        <StarIcon
          style={{ width: "20px", height: "20px", cursor: "pointer" }}
        />
      </div>
    </div>
  );
};

export { EmailItem, Tag };
