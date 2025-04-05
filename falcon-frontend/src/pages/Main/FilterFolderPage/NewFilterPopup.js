import React, { useState } from "react";
import "./NewFilterPopup.css";
import { ReactComponent as CloseIcon } from "../../../assets/icons/black/x.svg";

const allTags = ["Social", "School", "Work", "Music", "Digital", "All"];

const folderColors = [
  "var(--folder-green-1)",
  "var(--folder-green-2)",
  "var(--folder-orange)",
  "var(--folder-purple)",
  "var(--folder-blue)",
  "var(--folder-yellow)",
];

const NewFilterPopup = ({ onClose, onSave }) => {
  const [filterName, setFilterName] = useState("");
  const [emailInput, setEmailInput] = useState("");
  const [emails, setEmails] = useState([]);
  const [keywordInput, setKeywordInput] = useState("");
  const [keywords, setKeywords] = useState([]);
  const [selectedTags, setSelectedTags] = useState([]);
  const [selectedColor, setSelectedColor] = useState("");

  const isValidEmail = (email) =>
    /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim());

  const addEmail = () => {
    if (isValidEmail(emailInput)) {
      setEmails([...emails, emailInput]);
      setEmailInput("");
    }
  };

  const addKeyword = () => {
    if (keywordInput.trim()) {
      setKeywords([...keywords, keywordInput.trim()]);
      setKeywordInput("");
    }
  };

  const toggleTag = (tag) => {
    setSelectedTags((prev) =>
      prev.includes(tag) ? prev.filter((t) => t !== tag) : [...prev, tag]
    );
  };

  const saveFilter = () => {
    const data = {
      name: filterName,
      senders: emails,
      keywords,
      tags: selectedTags,
      color: selectedColor,
    };
    onSave(data);
    onClose();
  };

  return (
    <div className="popup-overlay">
      <div className="new-filter-popup">
        <div className="header-div">
          <h2>Create a new Filter</h2>
          <p>
            Create a custom filter to automatically organize emails based on
            your chosen criteria, like sender, tags, or keywords.
          </p>
          <button className="close-btn" onClick={onClose}>
            <CloseIcon />
          </button>
        </div>

        {/* Filter Name */}
        <div className="form-group">
          <label>
            Filter name <span className="required">*</span>
          </label>
          <input
            className="form-input"
            placeholder="Insert filter name for recognize it later"
            value={filterName}
            onChange={(e) => setFilterName(e.target.value)}
          />
        </div>

        {/* Sender Emails */}
        <div className="form-group">
          <label>Sender email address</label>
          <div className="input-row">
            <input
              className="form-input"
              placeholder="Insert email address"
              value={emailInput}
              onChange={(e) => setEmailInput(e.target.value)}
            />
            <button
              className="add-btn"
              disabled={!isValidEmail(emailInput)}
              onClick={addEmail}
            >
              +
            </button>
          </div>
          <div className="pills-container">
            {emails.map((email, i) => (
              <span className="pill" key={i}>
                {email}{" "}
                <span
                  onClick={() => setEmails(emails.filter((_, j) => j !== i))}
                >
                  ×
                </span>
              </span>
            ))}
          </div>
        </div>

        {/* Keywords */}
        <div className="form-group">
          <label>Keywords included in the email subject / content</label>
          <div className="input-row">
            <input
              className="form-input"
              placeholder="Insert keywords"
              value={keywordInput}
              onChange={(e) => setKeywordInput(e.target.value)}
            />
            <button
              className="add-btn"
              disabled={!keywordInput.trim()}
              onClick={addKeyword}
            >
              +
            </button>
          </div>
          <div className="pills-container">
            {keywords.map((kw, i) => (
              <span className="pill" key={i}>
                {kw}{" "}
                <span
                  onClick={() =>
                    setKeywords(keywords.filter((_, j) => j !== i))
                  }
                >
                  ×
                </span>
              </span>
            ))}
          </div>
        </div>

        {/* Tags */}
        <div className="form-group">
          <label>Filtered tags</label>
          <div className="tags-container">
            {allTags.map((tag) => (
              <span
                key={tag}
                className={`tag-filter ${
                  selectedTags.includes(tag) ? "selected" : ""
                }`}
                onClick={() => toggleTag(tag)}
              >
                {tag}
              </span>
            ))}
          </div>
        </div>

        {/* Folder Colors */}
        <div className="form-group">
          <label>
            Folder color <span className="required">*</span>
          </label>
          <div className="colors-container">
            {folderColors.map((color, index) => (
              <div
                key={index}
                className={`color-circle ${
                  selectedColor === color ? "selected" : ""
                }`}
                style={{ background: color }}
                onClick={() => setSelectedColor(color)}
              />
            ))}
          </div>
        </div>

        {/* Action Buttons */}
        <div className="center-col">
          <button className="btn-blue" onClick={saveFilter}>
            Create
          </button>
          <button className="cancel-link" onClick={onClose}>
            Cancel
          </button>
        </div>
      </div>
    </div>
  );
};

export default NewFilterPopup;
