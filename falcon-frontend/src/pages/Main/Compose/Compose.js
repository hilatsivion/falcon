import React, { useState } from "react";
import "./Compose.css";
import { ReactComponent as SendIcon } from "../../../assets/icons/black/send-white.svg";
import { ReactComponent as Paperclip } from "../../../assets/icons/black/paperclip.svg";
import { ReactComponent as GenerateIcon } from "../../../assets/icons/blue/magicpen-icon.svg";
import AiComposePanel from "./AiComposePanel";

const Compose = () => {
  const [from, setFrom] = useState("hilatsivion@gmail.com");
  const [subject, setSubject] = useState("");
  const [to, setTo] = useState([]);
  const [body, setBody] = useState("");
  const [isAiOpen, setIsAiOpen] = useState(false);

  const detectDirection = (text) => {
    const firstChar = text.trim().charAt(0);
    const isHebrew = /^[\u0590-\u05FF]/.test(firstChar);
    return isHebrew ? "rtl" : "ltr";
  };

  const availableFrom = [
    "hilatsivion@gmail.com",
    "hila.t@outlook.com",
    "hilatsivion222@gmail.com",
  ];

  return (
    <div className="compose-page page-container">
      {/* Compose Header */}
      <div className="compose-header space-between-full-wid">
        <button className="ai-button" onClick={() => setIsAiOpen(true)}>
          <GenerateIcon />
          <span className="gradient-text">Compose with AI</span>
        </button>
        <button className="send-btn">
          <SendIcon />
          Send
        </button>
      </div>

      <div className="flex-col inputs-compose">
        {/* Subject Input */}
        <input
          className="compose-input subject underline-grey"
          type="text"
          placeholder="Subject"
          value={subject}
          onChange={(e) => setSubject(e.target.value)}
        />

        {/* To Input */}
        <div className="input-with-label underline-grey">
          <span className="fixed-label">To:</span>
          <input
            className="compose-input"
            type="text"
            value={to}
            onChange={(e) => setTo([e.target.value])}
          />
        </div>

        {/* From Selector */}
        <div className="input-with-label underline-grey">
          <label className="fixed-label">From:</label>
          <select
            className="from-select margin-left-10"
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
        dir={detectDirection(body)}
        onChange={(e) => setBody(e.target.value)}
      />

      {/* AI Compose Panel */}
      {isAiOpen && <AiComposePanel onClose={() => setIsAiOpen(false)} />}

      {isAiOpen && (
        <>
          <div className="ai-overlay" onClick={() => setIsAiOpen(false)} />
          <AiComposePanel onClose={() => setIsAiOpen(false)} />
        </>
      )}
    </div>
  );
};

export default Compose;
