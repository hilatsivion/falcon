import React from "react";
import { EmailItem } from "../../../components/EmailItem/EmailItem";
import "./Inbox.css";

const Inbox = () => {
  const emails = [
    {
      sender: "John Doe",
      subject: "Meeting Reminder",
      preview: "Don't forget about our meeting tomorrow at 10 AM...",
      tags: ["Work"],
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
  ];

  return (
    <div className="inbox-container">
      <h2 className="inbox-title">Inbox</h2>
      {emails.map((email, index) => (
        <EmailItem key={index} {...email} />
      ))}
    </div>
  );
};

export default Inbox;
