import React, { useState } from "react"; // Import useEffect
import "./NewFilterPopup.css";
import { ReactComponent as CloseIcon } from "../../../assets/icons/black/x.svg";
import { toast } from "react-toastify"; // Import toast

// Remove const allTags = [...]

const folderColors = [
  "var(--folder-green-1)",
  "var(--folder-green-2)",
  "var(--folder-orange)",
  "var(--folder-purple)",
  "var(--folder-blue)",
  "var(--folder-yellow)",
];

// Add availableTags prop
const NewFilterPopup = ({ onClose, onSave, availableTags = [] }) => {
  // Use availableTags prop
  const [filterName, setFilterName] = useState("");
  const [emailInput, setEmailInput] = useState("");
  const [emails, setEmails] = useState([]);
  const [keywordInput, setKeywordInput] = useState("");
  const [keywords, setKeywords] = useState([]);
  const [selectedTagIds, setSelectedTagIds] = useState([]); // Store IDs now
  const [selectedColor, setSelectedColor] = useState(folderColors[0]); // Default color

  // No need to fetch tags here anymore, they are passed as props

  const isValidEmail = (email) =>
    /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim());

  const addEmail = () => {
    if (isValidEmail(emailInput) && !emails.includes(emailInput.trim())) {
      // Prevent duplicates
      setEmails([...emails, emailInput.trim()]);
      setEmailInput("");
    }
  };

  const addKeyword = () => {
    if (keywordInput.trim() && !keywords.includes(keywordInput.trim())) {
      // Prevent duplicates
      setKeywords([...keywords, keywordInput.trim()]);
      setKeywordInput("");
    }
  };

  // CHANGE: Toggle based on Tag ID
  const toggleTag = (tagId) => {
    setSelectedTagIds((prev) =>
      prev.includes(tagId)
        ? prev.filter((id) => id !== tagId)
        : [...prev, tagId]
    );
  };

  // CHANGE: Call onSave prop with data formatted for the backend DTO
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
      FolderColor: selectedColor, // Send the CSS variable name or map it if needed
      Keywords: keywords,
      SenderEmails: emails,
      TagIds: selectedTagIds, // Send the array of selected tag IDs
    };
    // Call the onSave function passed from the parent
    onSave(filterDataForBackend);
    // onClose(); // Let the parent handle closing after successful save
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
              onKeyDown={(e) => e.key === "Enter" && addEmail()} // Add on Enter
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
                  className="remove-pill" // Add class for styling removal 'x'
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
              onKeyDown={(e) => e.key === "Enter" && addKeyword()} // Add on Enter
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
                  className="remove-pill" // Add class for styling removal 'x'
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
          <label>Filtered tags (Optional)</label>
          {/* CHANGE: Use availableTags prop */}
          {availableTags.length > 0 ? (
            <div className="tags-container">
              {availableTags.map((tag) => (
                <span
                  key={tag.tagId} // Use unique tagId
                  className={`tag-filter ${
                    selectedTagIds.includes(tag.tagId) ? "selected" : ""
                  }`}
                  onClick={() => toggleTag(tag.tagId)} // Toggle based on ID
                >
                  {tag.tagName} {/* Display name */}
                </span>
              ))}
            </div>
          ) : (
            <p className="info-message">No tags available to select.</p>
          )}
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
          {/* CHANGE: Call handleSaveFolder */}
          <button className="btn-blue" onClick={handleSaveFolder}>
            Create Filter
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
