import React, { useState, useEffect } from "react";
import "./NewFilterPopup.css";
import { ReactComponent as CloseIcon } from "../../../assets/icons/black/x.svg";
import { toast } from "react-toastify";

const folderColors = [
  "var(--folder-green-1)",
  "var(--folder-green-2)",
  "var(--folder-orange)",
  "var(--folder-purple)",
  "var(--folder-blue)",
  "var(--folder-yellow)",
];

const NewFilterPopup = ({
  onClose,
  onSave,
  availableTags = [],
  isEditing = false,
  editingFilter = null,
}) => {
  const [filterName, setFilterName] = useState("");
  const [emailInput, setEmailInput] = useState("");
  const [emails, setEmails] = useState([]);
  const [keywordInput, setKeywordInput] = useState("");
  const [keywords, setKeywords] = useState([]);
  const [selectedTagIds, setSelectedTagIds] = useState([]);
  const [selectedColor, setSelectedColor] = useState(folderColors[0]);

  useEffect(() => {
    if (isEditing && editingFilter) {
      setFilterName(editingFilter.name || "");
      setEmails(editingFilter.senderEmails || []);
      setKeywords(editingFilter.keywords || []);
      setSelectedTagIds(editingFilter.tagIds || []);
      setSelectedColor(editingFilter.folderColor || folderColors[0]);
    }
  }, [isEditing, editingFilter]);

  const isValidEmail = (email) =>
    /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim());

  const addEmail = () => {
    if (isValidEmail(emailInput) && !emails.includes(emailInput.trim())) {
      setEmails([...emails, emailInput.trim()]);
      setEmailInput("");
    }
  };

  const addKeyword = () => {
    if (keywordInput.trim() && !keywords.includes(keywordInput.trim())) {
      setKeywords([...keywords, keywordInput.trim()]);
      setKeywordInput("");
    }
  };

  const toggleTag = (tagId) => {
    setSelectedTagIds((prev) =>
      prev.includes(tagId)
        ? prev.filter((id) => id !== tagId)
        : [...prev, tagId]
    );
  };

  const handleSaveFolder = () => {
    if (!filterName.trim()) {
      toast.error("Filter name is required.");
      return;
    }
    if (!selectedColor) {
      toast.error("Please select a folder color.");
      return;
    }

    const filterDataForBackend = {
      Name: filterName.trim(),
      FolderColor: selectedColor,
      Keywords: keywords,
      SenderEmails: emails,
      TagIds: selectedTagIds,
    };

    if (isEditing && editingFilter) {
      onSave(editingFilter.filterFolderId, filterDataForBackend);
    } else {
      onSave(filterDataForBackend);
    }
  };

  return (
    <div className="popup-overlay">
      <div className="new-filter-popup">
        <div className="header-div">
          <h2>{isEditing ? "Edit Filter" : "Create a new Filter"}</h2>
          <p>
            {isEditing
              ? "Modify the criteria for this filter."
              : "Create a custom filter to automatically organize emails based on your chosen criteria, like sender, tags, or keywords."}
          </p>
          <button className="close-btn" onClick={onClose}>
            <CloseIcon />
          </button>
        </div>

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

        <div className="form-group">
          <label>Sender email address</label>
          <div className="input-row">
            <input
              className="form-input"
              placeholder="Insert email address"
              value={emailInput}
              onChange={(e) => setEmailInput(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && addEmail()}
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
                  className="remove-pill"
                  onClick={() => setEmails(emails.filter((_, j) => j !== i))}
                >
                  ×
                </span>
              </span>
            ))}
          </div>
        </div>

        <div className="form-group">
          <label>Keywords included in the email subject / content</label>
          <div className="input-row">
            <input
              className="form-input"
              placeholder="Insert keywords"
              value={keywordInput}
              onChange={(e) => setKeywordInput(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && addKeyword()}
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
                  className="remove-pill"
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

        <div className="form-group">
          <label>Filtered tags (Optional)</label>
          {availableTags.length > 0 ? (
            <div className="tags-container">
              {availableTags.map((tag) => (
                <span
                  key={tag.tagId}
                  className={`tag-filter ${
                    selectedTagIds.includes(tag.tagId) ? "selected" : ""
                  }`}
                  onClick={() => toggleTag(tag.tagId)}
                >
                  {tag.tagName}
                </span>
              ))}
            </div>
          ) : (
            <p className="info-message">No tags available to select.</p>
          )}
        </div>

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

        <div className="center-col">
          <button className="btn-blue" onClick={handleSaveFolder}>
            {isEditing ? "Save Changes" : "Create Filter"}
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
