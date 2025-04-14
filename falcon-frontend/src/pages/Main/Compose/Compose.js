import React, { useRef, useState, useEffect } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { ReactComponent as SendIcon } from "../../../assets/icons/black/send-white.svg";
import { ReactComponent as Paperclip } from "../../../assets/icons/black/paperclip.svg";
import { ReactComponent as GenerateIcon } from "../../../assets/icons/blue/magicpen-icon.svg";
import AiComposePanel from "./AiComposePanel";
import "./Compose.css";

import { getAuthToken } from "../../../utils/auth";
import { API_BASE_URL } from "../../../config/constants";
import { toast } from "react-toastify";

const Compose = () => {
  const location = useLocation();
  const navigate = useNavigate();

  const initialTo = location.state?.to || "";
  const initialSubject = location.state?.subject || "";
  const initialBody = location.state?.body || "";

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

  // Fetch sender email accounts on mount
  useEffect(() => {
    const fetchSenderAccounts = async () => {
      setIsLoading(true);
      setError(null);
      const token = getAuthToken();

      if (!token) {
        setError("Authentication token not found.");
        setIsLoading(false);
        return;
      }

      try {
        const response = await fetch(`${API_BASE_URL}/api/user/mailaccounts`, {
          headers: { Authorization: `Bearer ${token}` },
        });

        if (!response.ok) {
          throw new Error("Failed to fetch sender accounts.");
        }

        const accounts = await response.json();
        setSenderAccounts(accounts);

        const defaultAccount = accounts.find((acc) => acc.isDefault);
        setSelectedAccountId(
          defaultAccount?.mailAccountId || accounts[0]?.mailAccountId || ""
        );
      } catch (err) {
        setError(err.message);
        toast.error(err.message);
      } finally {
        setIsLoading(false);
      }
    };

    fetchSenderAccounts();
  }, []);

  const handleFileChange = (e) => {
    const selectedFiles = Array.from(e.target.files);
    setFiles((prev) => [...prev, ...selectedFiles]);
  };

  const isValidEmail = (email) =>
    /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim());

  const detectDirection = (text) => {
    const firstChar = text.trim().charAt(0);
    return /^[\u0590-\u05FF]/.test(firstChar) ? "rtl" : "ltr";
  };

  const handleSendClick = async () => {
    setError(null);

    if (!isValidEmail(to)) {
      const msg = "Please enter a valid recipient email address.";
      setError(msg);
      return toast.error(msg);
    }

    if (!selectedAccountId) {
      const msg = "Please select a sender email address.";
      setError(msg);
      return toast.error(msg);
    }

    if (body.trim() === "") {
      const msg = "Email body cannot be empty.";
      setError(msg);
      return toast.error(msg);
    }

    setIsLoading(true);
    const token = getAuthToken();

    const formData = new FormData();
    formData.append("MailAccountId", selectedAccountId);
    formData.append("Subject", subject);
    formData.append("Body", body);
    formData.append("Recipients", to);
    files.forEach((file) => {
      formData.append("attachments", file);
    });

    try {
      const response = await fetch(`${API_BASE_URL}/api/mail/send`, {
        method: "POST",
        headers: { Authorization: `Bearer ${token}` },
        body: formData,
      });

      if (response.ok) {
        navigate("/inbox", {
          state: { message: "Email sent successfully!" },
          replace: true,
        });
      } else {
        const errorData = await response.json().catch(() => ({}));
        const msg =
          errorData.message || `Failed to send email (${response.status})`;
        setError(msg);
        toast.error(msg);
      }
    } catch (err) {
      const msg = err.message || "A network error occurred.";
      setError(msg);
      toast.error(msg);
    } finally {
      setIsLoading(false);
    }
  };

  const isSendEnabled =
    to.trim() &&
    isValidEmail(to) &&
    selectedAccountId &&
    body.trim() &&
    !isLoading;

  return (
    <div className="compose-page page-container">
      {/* Header */}
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
          {isLoading ? <span className="loader-small" /> : <SendIcon />}
          {isLoading ? "Sending..." : "Send"}
        </button>
      </div>

      {/* Inputs */}
      <div className="flex-col inputs-compose">
        <input
          className="compose-input subject underline-grey"
          type="text"
          placeholder="Subject"
          value={subject}
          onChange={(e) => setSubject(e.target.value)}
          disabled={isLoading}
        />

        <div className="input-with-label underline-grey">
          <span className="fixed-label">To:</span>
          <input
            className="compose-input"
            type="email"
            placeholder="Recipient email"
            value={to}
            onChange={(e) => setTo(e.target.value)}
            disabled={isLoading}
          />
        </div>

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
          onClick={() => fileInputRef.current?.click()}
          disabled={isLoading}
        >
          <Paperclip />
          Add files
        </button>

        <input
          type="file"
          multiple
          accept="image/*" // אפשר להשאיר את זה כדי לאפשר בחירת תמונות או להסיר כדי לאפשר הכל
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

      {/* AI Panel */}
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
