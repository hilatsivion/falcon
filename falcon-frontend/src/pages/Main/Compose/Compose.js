import React, { useState } from "react";
import "./Compose.css";
import { ReactComponent as SendIcon } from "../../../assets/icons/black/send-white.svg";
import { ReactComponent as Paperclip } from "../../../assets/icons/black/paperclip.svg";
import { ReactComponent as GenerateIcon } from "../../../assets/icons/blue/magicpen-icon.svg";

const Compose = () => {
  const [from, setFrom] = useState("hilatsivion@gmail.com");
  const [subject, setSubject] = useState("");
  const [to, setTo] = useState([]);
  const [body, setBody] = useState("");

  const availableFrom = [
    "hilatsivion@gmail.com",
    "hila.t@outlook.com",
    "hilatsivion222@gmail.com",
  ];

  return (
    <div className="compose-page page-container">
      {/* Compose Header */}
      <div className="compose-header space-between-full-wid">
        <button className="ai-button">
          <GenerateIcon />
          <span className="gradient-text">Compose with AI</span>
        </button>{" "}
        <button className="send-btn">
          <SendIcon />
          Send
        </button>
      </div>

      <div className="flex-col">
        {/* Subject Input */}
        <input
          className="compose-input subject"
          type="text"
          placeholder="Subject"
          value={subject}
          onChange={(e) => setSubject(e.target.value)}
        />

        {/* To Input (simplified for now) */}
        <input
          className="compose-input"
          type="text"
          placeholder="To:"
          value={to}
          onChange={(e) => setTo([e.target.value])}
        />

        {/* From Selector */}
        <div className="from-container">
          <label className="from-label">From:</label>
          <select
            className="from-select"
            value={from}
            onChange={(e) => setFrom(e.target.value)}
          >
            {availableFrom.map((email) => (
              <option key={email} value={email}>
                {email}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* File Attach */}
      <button className="files-btn">
        <Paperclip />
        Add files
      </button>

      {/* Body */}
      <textarea
        className="compose-body"
        placeholder="Write here..."
        value={body}
        onChange={(e) => setBody(e.target.value)}
      />
    </div>
  );
};

export default Compose;
