import React, { useState } from "react";
import { EmailItem } from "../../../components/EmailItem/EmailItem";
import listIcon from "../../../assets/icons/black/list.svg";
import folderIcon from "../../../assets/icons/black/folder.svg";
import "./Inbox.css";

const Inbox = () => {
  const [isListView, setIsListView] = useState(true);

  const emails = [
    {
      sender: "John Doe",
      subject: "Meeting Reminder",
      preview: "Don't forget about our meeting tomorrow at 10 AM...",
      tags: ["Work", "Social", "School"],
      time: "10:30",
      avatarColor: "#ff5733",
    },
    {
      sender: "Jane Smith",
      subject: "New Social Event!",
      preview: "Join us this weekend for a fun gathering...",
      tags: ["Social"],
      time: "2:45",
      avatarColor: "#33aaff",
    },
    {
      sender: "Banking Services",
      subject: "Your Monthly Statement",
      preview: "Your latest bank statement is now available...",
      tags: ["Finance"],
      time: "18:00",
      avatarColor: "#ffd700",
    },
    {
      sender: "Hila Tsivion",
      subject: "Meeting Reminder",
      preview: "Don't forget about our meeting tomorrow at 10 AM...",
      tags: ["Work", "Social", "School"],
      time: "10:30",
      avatarColor: "#f23858",
    },
  ];

  return (
    <div className="inbox-container">
      <div className="inbox-header">
        <h2 className="inbox-title">Inbox</h2>

        {/* Switch Button */}
        <div
          className="switch-button"
          onClick={() => setIsListView(!isListView)}
        >
          {/* White Circle */}
          <div
            className={`switch-circle ${isListView ? "left" : "right"}`}
          ></div>
          {/* Icons */}
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
          <EmailItem key={index} {...email} />
        ))}
      </div>
    </div>
  );
};

export default Inbox;
