import React from "react";
import "./AiComposePanel.css";
import { ReactComponent as CloseIcon } from "../../../assets/icons/black/x.svg";

const AiComposePanel = ({ onClose }) => {
  return (
    <div className="ai-compose-panel">
      <div className="ai-header">
        <h2>
          <span className="gradient-text">Compose with AI</span>
        </h2>
        <button className="close-btn" onClick={onClose}>
          <CloseIcon />
        </button>
      </div>
      <p className="subtext">
        Write your email idea, and the AI will craft the content and subject for
        you.
      </p>

      <textarea className="ai-textarea" placeholder="Write here..." />

      <button className="btn-blue">Generate</button>
    </div>
  );
};

export default AiComposePanel;
