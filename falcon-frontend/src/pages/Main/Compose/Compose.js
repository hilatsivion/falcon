import React, { useState, useRef } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { ReactComponent as SendIcon } from "../../../assets/icons/black/send-white.svg";
import { ReactComponent as Paperclip } from "../../../assets/icons/black/paperclip.svg";
import { ReactComponent as GenerateIcon } from "../../../assets/icons/blue/magicpen-icon.svg";
import SuccessPopup from "../../../components/Popup/oneSentence_link";
import AiComposePanel from "./AiComposePanel";
import "./Compose.css";

const Compose = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const initialTo = location.state?.to || "";
  const initialSubject = location.state?.subject || "";
  const initialBody = location.state?.body || "";

  const [from, setFrom] = useState("hilatsivion@gmail.com"); //לשמור את כתובת המייל בלוקל סטורג ואז לשים פה את הערך הזה כדיפולט
  const [subject, setSubject] = useState(initialSubject);
  const [to, setTo] = useState(initialTo ? [initialTo] : []);
  const [body, setBody] = useState(initialBody);
  const [isAiOpen, setIsAiOpen] = useState(false);
  const [showSuccess, setShowSuccess] = useState(false);

  const [files, setFiles] = useState([]);
  const fileInputRef = useRef(null);

  const handleSendClick = () => {
    if (isSendEnabled) {
      // TODO: send logic here
      console.log("Sending email:", { to, from, subject, body });
      setShowSuccess(true); // show popup after 'send'
    }
  };

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

  const handleAiDone = ({ subject, content }) => {
    setSubject(subject);
    setBody(content);
  };

  const isValidEmail = (email) =>
    /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim());

  const isSendEnabled =
    to.length > 0 &&
    to.every((email) => isValidEmail(email)) &&
    from &&
    body.trim() !== "";

  const triggerFileInput = () => {
    if (fileInputRef.current) {
      fileInputRef.current.click();
    }
  };

  const handleFileChange = (e) => {
    const selectedFiles = Array.from(e.target.files);
    setFiles((prev) => [...prev, ...selectedFiles]);
  };

  return (
    <div className="compose-page page-container">
      {/* Compose Header */}
      <div className="compose-header space-between-full-wid">
        <button className="ai-button" onClick={() => setIsAiOpen(true)}>
          <GenerateIcon />
          <span className="gradient-text">Compose with AI</span>
        </button>

        <button
          className="send-btn"
          onClick={handleSendClick}
          disabled={!isSendEnabled}
          style={{ opacity: isSendEnabled ? 1 : 0.4 }}
        >
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
      <div className="file-upload-row">
        <button className="files-btn" onClick={triggerFileInput}>
          <Paperclip />
          Add files
        </button>
        <input
          type="file"
          multiple
          style={{ display: "none" }}
          ref={fileInputRef}
          onChange={handleFileChange}
        />

        <div className="file-pills">
          {files.map((file, index) => (
            <div key={index} className="file-pill">
              {file.name}
              <span
                className="remove-file"
                onClick={() =>
                  setFiles((prev) => prev.filter((_, i) => i !== index))
                }
              >
                ×
              </span>
            </div>
          ))}
        </div>
      </div>

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
          <AiComposePanel
            onClose={() => setIsAiOpen(false)}
            onDone={handleAiDone}
          />
        </>
      )}

      {showSuccess && (
        <SuccessPopup
          message="Sent Successfully!"
          linkText="Back to Inbox"
          onLinkClick={() => {
            setShowSuccess(false);
            navigate("/inbox"); // ← adjust the route if needed
          }}
        />
      )}
    </div>
  );
};

export default Compose;
