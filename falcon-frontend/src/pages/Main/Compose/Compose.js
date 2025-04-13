import React, { useState, useRef, useEffect } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { ReactComponent as SendIcon } from "../../../assets/icons/black/send-white.svg";
import { ReactComponent as Paperclip } from "../../../assets/icons/black/paperclip.svg";
import { ReactComponent as GenerateIcon } from "../../../assets/icons/blue/magicpen-icon.svg";
//import SuccessPopup from "../../../components/Popup/oneSentence_link";
import AiComposePanel from "./AiComposePanel";
import "./Compose.css";

// Import utilities and constants
import { getAuthToken } from "../../../utils/auth";
import { API_BASE_URL } from "../../../config/constants";
import { toast } from "react-toastify";

const Compose = () => {
  const location = useLocation();
  const navigate = useNavigate();

  // Initial values from navigation state
  const initialTo = location.state?.to || "";
  const initialSubject = location.state?.subject || "";
  const initialBody = location.state?.body || "";

  // --- State Declarations ---
  const [senderAccounts, setSenderAccounts] = useState([]);
  const [selectedAccountId, setSelectedAccountId] = useState("");
  const [subject, setSubject] = useState(initialSubject);
  const [to, setTo] = useState(initialTo);
  const [body, setBody] = useState(initialBody);
  const [files, setFiles] = useState([]);
  const [isAiOpen, setIsAiOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState(null);

  const fileInputRef = useRef(null);

  // --- Fetch Sender Accounts ---
  useEffect(() => {
    const fetchSenderAccounts = async () => {
      setIsLoading(true);
      setError(null);
      const token = getAuthToken(); // [cite: 3]
      if (!token) {
        setError("Authentication token not found.");
        setIsLoading(false);
        return;
      }

      try {
        const response = await fetch(`${API_BASE_URL}/api/user/mailaccounts`, {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        });

        if (!response.ok) {
          throw new Error(
            `Failed to fetch sender accounts: ${response.statusText}`
          );
        }

        const accounts = await response.json();
        setSenderAccounts(accounts);

        // Set default selection
        const defaultAccount = accounts.find((acc) => acc.isDefault);
        if (defaultAccount) {
          setSelectedAccountId(defaultAccount.mailAccountId);
        } else if (accounts.length > 0) {
          setSelectedAccountId(accounts[0].mailAccountId); // Select the first one if no default
        }
      } catch (err) {
        setError(
          err.message || "An error occurred while fetching sender accounts."
        );
        console.error("Fetch Sender Accounts Error:", err);
        toast.error(err.message || "Error fetching sender accounts."); // Show toast on fetch error
      } finally {
        setIsLoading(false);
      }
    };

    fetchSenderAccounts();
  }, []);

  // --- Event Handlers ---
  const handleSendClick = async () => {
    setError(null); // Clear previous errors

    // Simple validation (add more as needed)
    if (!isValidEmail(to)) {
      setError("Please enter a valid recipient email address.");
      toast.error("Please enter a valid recipient email address.");
      return;
    }
    if (!selectedAccountId) {
      setError("Please select a sender email address.");
      toast.error("Please select a sender email address.");
      return;
    }
    if (body.trim() === "") {
      setError("Email body cannot be empty.");
      toast.error("Email body cannot be empty.");
      return;
    }

    setIsLoading(true);
    const token = getAuthToken(); // [cite: 3]

    // Use FormData because we might send files and backend uses [FromForm]
    const formData = new FormData();
    formData.append("MailAccountId", selectedAccountId);
    formData.append("Subject", subject);
    formData.append("Body", body);
    formData.append("Recipients", to); // Backend expects List<string>, append single email
    // Append files
    files.forEach((file) => {
      formData.append("attachments", file); // Match the backend parameter name
    });

    try {
      const response = await fetch(`${API_BASE_URL}/api/mail/send`, {
        // [cite: 6]
        method: "POST",
        headers: {
          Authorization: `Bearer ${token}`,
          // Don't set 'Content-Type': 'multipart/form-data', fetch does it automatically for FormData
        },
        body: formData,
      });

      if (response.ok) {
        // Success! Navigate to Inbox and pass success message
        navigate("/inbox", {
          // [cite: 4] for route path context
          state: { message: "Email sent successfully!" },
          replace: true, // Optional: replace history entry
        });
        // No toast here, will be shown on Inbox page
      } else {
        // Handle specific errors
        const errorData = await response
          .json()
          .catch(() => ({ message: response.statusText })); // Try parsing JSON, fallback to statusText
        const errorMessage =
          errorData.message || `Failed to send email (${response.status})`;
        setError(errorMessage);
        console.error("Send Mail Error:", response.status, errorData);
        toast.error(errorMessage); // Show error toast
      }
    } catch (err) {
      setError(
        err.message || "An network error occurred while sending the email."
      );
      console.error("Send Mail Network/Fetch Error:", err);
      toast.error(err.message || "An network error occurred."); // Show network error toast
    } finally {
      setIsLoading(false);
    }
  };

  const triggerFileInput = () => {
    if (fileInputRef.current) {
      fileInputRef.current.click();
    }
  };

  const handleFileChange = (e) => {
    const selectedFiles = Array.from(e.target.files);
    setFiles((prev) => [...prev, ...selectedFiles]);
    // Optional: Add validation for file size/type here
  };

  // --- Utility Functions ---
  const detectDirection = (text) => {
    const firstChar = text.trim().charAt(0);
    const isHebrew = /^[\u0590-\u05FF]/.test(firstChar);
    return isHebrew ? "rtl" : "ltr";
  };

  const isValidEmail = (email) =>
    /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim());

  // --- Render Logic ---
  const isSendEnabled =
    to.trim() !== "" &&
    isValidEmail(to) &&
    selectedAccountId &&
    body.trim() !== "" &&
    !isLoading;

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
          disabled={!isSendEnabled || isLoading} // Disable while loading
          style={{ opacity: !isSendEnabled || isLoading ? 0.4 : 1 }}
        >
          {isLoading ? <span className="loader-small"></span> : <SendIcon />}{" "}
          {/* Show loader */}
          {isLoading ? "Sending..." : "Send"}
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
          disabled={isLoading} // Disable inputs while sending
        />

        {/* To Input - Changed to single string */}
        <div className="input-with-label underline-grey">
          <span className="fixed-label">To:</span>
          <input
            className="compose-input"
            type="email" // Use type="email" for basic browser validation
            placeholder="Recipient email"
            value={to}
            onChange={(e) => setTo(e.target.value)}
            disabled={isLoading}
          />
        </div>

        {/* From Selector - Populated from state */}
        <div className="input-with-label underline-grey">
          <label className="fixed-label">From:</label>
          <select
            className="from-select margin-left-10"
            value={selectedAccountId}
            onChange={(e) => setSelectedAccountId(e.target.value)}
            disabled={isLoading || senderAccounts.length === 0}
          >
            <option value="" disabled>
              {isLoading
                ? "Loading..."
                : senderAccounts.length === 0
                ? "No accounts found"
                : "Select account"}
            </option>
            {senderAccounts.map((account) => (
              <option key={account.mailAccountId} value={account.mailAccountId}>
                {account.emailAddress}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* File Attach */}
      <div className="file-upload-row">
        <button
          className="files-btn"
          onClick={triggerFileInput}
          disabled={isLoading}
        >
          <Paperclip />
          Add files
        </button>
        <input
          type="file"
          multiple
          style={{ display: "none" }}
          ref={fileInputRef}
          onChange={handleFileChange}
          disabled={isLoading}
        />

        <div className="file-pills">
          {files.map((file, index) => (
            <div key={index} className="file-pill">
              {file.name}
              <span
                className="remove-file"
                onClick={() => {
                  if (!isLoading) {
                    // Prevent removal during send
                    setFiles((prev) => prev.filter((_, i) => i !== index));
                  }
                }}
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
        disabled={isLoading}
      />

      {/* AI Compose Panel */}
      {isAiOpen && (
        <>
          <div className="ai-overlay" onClick={() => setIsAiOpen(false)} />
          <AiComposePanel
            onClose={() => setIsAiOpen(false)}
            onDone={({ subject: aiSubject, content: aiContent }) => {
              setSubject(aiSubject);
              setBody(aiContent);
            }}
          />
        </>
      )}
    </div>
  );
};

export default Compose;
