import React, { useState } from "react";
import { EmailItem } from "../../../components/EmailItem/EmailItem";
import EmailView from "../../../components/EmailView/EmailView";
import listIcon from "../../../assets/icons/black/list.svg";
import folderIcon from "../../../assets/icons/black/folder.svg";
import "./Inbox.css";

// Centralized tag colors
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

// Reusable Tag component
export const Tag = ({ name }) => (
  <span
    className="email-tag"
    style={{ backgroundColor: tagColors[name] || "#ddd" }}
  >
    {name}
  </span>
);

const Inbox = () => {
  const [isListView, setIsListView] = useState(true);
  const [selectedEmail, setSelectedEmail] = useState(null);

  const [emails, setEmails] = useState([
    {
      sender: "John Doe",
      subject: "Meeting Reminder",
      preview: "Don't forget about our meeting tomorrow at 10 AM...",
      tags: ["Work", "Social", "School"],
      time: "10:30",
      avatarColor: "#ff5733",
      isRead: false,
      isStarred: true,
    },
    {
      sender: "Jane Smith",
      subject: "New Social Event!",
      preview: "Join us this weekend for a fun gathering...",
      tags: ["Social"],
      time: "14:45",
      avatarColor: "#33aaff",
      isRead: false,
      isStarred: false,
    },
    {
      sender: "Banking Services",
      subject: "Your Monthly Statement",
      preview: "Your latest bank statement is now available...",
      tags: ["Finance"],
      time: "18:00",
      avatarColor: "#ffd700",
      isRead: true,
      isStarred: false,
    },
    {
      sender: "Hila Tsivion",
      subject: "Meeting Reminder",
      preview: "Don't forget about our meeting tomorrow at 10 AM...",
      tags: ["Work", "Social", "School"],
      time: "10:30",
      avatarColor: "#f23858",
      isRead: false,
      isStarred: true,
    },
  ]);

  return (
    <div className="inbox-container">
      <div className="inbox-header">
        <h2 className="inbox-title">Inbox</h2>

        <div
          className="switch-button"
          onClick={() => setIsListView(!isListView)}
        >
          <div
            className={`switch-circle ${isListView ? "left" : "right"}`}
          ></div>
          <img
            src={listIcon}
            alt="List View"
            className={`switch-icon ${isListView ? "active" : "inactive"}`}
          />
          <img
            src={folderIcon}
            alt="Folder View"
            className={`switch-icon ${isListView ? "inactive" : "active"}`}
          />
        </div>
      </div>

      <div className="emails-container">
        {emails.map((email, index) => (
          <div
            key={index}
            onClick={(e) => {
              if (!e.target.closest(".email-star")) {
                setSelectedEmail(email);
              }
            }}
          >
            <EmailItem
              {...email}
              onClick={() => setSelectedEmail(email)} // Pass onClick correctly
              onStarToggle={() => {
                const updatedEmails = [...emails];
                updatedEmails[index].isStarred =
                  !updatedEmails[index].isStarred;
                setEmails(updatedEmails);
              }}
            />
          </div>
        ))}
      </div>

      <EmailView email={selectedEmail} onClose={() => setSelectedEmail(null)} />
    </div>
  );
};

export default Inbox;
