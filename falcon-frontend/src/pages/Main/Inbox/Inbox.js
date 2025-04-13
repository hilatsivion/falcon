import React, { useState, useEffect } from "react";
import { toast } from "react-toastify"; // Keep for success messages
import { useLocation } from "react-router-dom"; // Keep for success messages
import FilterFolderPage from "../FilterFolderPage/FilterFolderPage";
import InboxEmailList from "./InboxEmailList";
import listIcon from "../../../assets/icons/black/list.svg";
import folderIcon from "../../../assets/icons/black/folder.svg";
import "./Inbox.css";
// Remove: import { getAuthToken } from "../../../utils/auth"; // No longer needed directly here
import { API_BASE_URL } from "../../../config/constants";
import { useAuth } from "../../../context/AuthContext";

// Centralized tag colors
const tagColors = {
  Inbox: "#cbd5ff",
  Social: "#c8facc",
  School: "#f6d6b8",
  Work: "#b8ebf6",
  Personal: "#ffb3c6",
  Finance: "#ffd700",
  Promotions: "#ff9f43",
  Updates: "#6c757d",
  Forums: "#28a745",
  Travel: "#007bff",
};

// Reusable Tag component
export const Tag = ({ name }) => (
  <span
    className="email-tag"
    style={{ backgroundColor: tagColors[name] || "#ddd" }}
  >
    {name}
  </span>
);

const Inbox = () => {
  const { authToken, isAuthenticated } = useAuth(); // Get state from context
  const [isListView, setIsListView] = useState(true);
  const [selectedEmail, setSelectedEmail] = useState(null);
  const location = useLocation(); // For success message after redirect
  const [emails, setEmails] = useState([]); // Start with empty array for fetched data
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState(null);

  // --- Effect for Fetching Inbox Data ---
  useEffect(() => {
    // Define the async function to fetch data
    const fetchInboxData = async () => {
      // No need to check authToken here again if effect depends on it,
      // but it's safe to double-check or rely on isAuthenticated.
      if (!authToken) {
        console.log("Inbox fetch: No auth token available.");
        // Set loading false if it was true, clear emails
        setIsLoading(false);
        setEmails([]);
        return;
      }

      console.log("Inbox fetch: Auth token found, attempting fetch.");
      setIsLoading(true);
      setError(null);
      try {
        const response = await fetch(
          `${API_BASE_URL}/api/mail/received/preview`, // Correct endpoint for inbox preview
          {
            headers: {
              Authorization: `Bearer ${authToken}`, // Use token from context
            },
          }
        );
        if (!response.ok) {
          let errorMsg = `Failed to fetch inbox data: ${response.statusText} (${response.status})`;
          try {
            const errData = await response.json();
            errorMsg = errData.message || errorMsg;
          } catch (e) {
            /* Ignore if body isn't JSON */
          }
          throw new Error(errorMsg);
        }
        const data = await response.json();
        console.log("Inbox fetch: Data received", data);
        setEmails(data); // Update state with fetched emails
      } catch (err) {
        console.error("Inbox fetch error:", err);
        setError(err.message || "Failed to load inbox.");
        setEmails([]); // Clear emails on error
      } finally {
        setIsLoading(false);
      }
    };

    // --- Call fetchInboxData ONLY if authenticated ---
    if (isAuthenticated) {
      fetchInboxData();
    } else {
      // Clear data if user logs out or isn't authenticated
      console.log("Inbox: Not authenticated, clearing data.");
      setEmails([]);
      setIsLoading(false);
      setError(null);
    }

    // Dependency array: Re-fetch if authentication status or token changes
  }, [authToken, isAuthenticated]);

  // --- Effect for Showing Toast Message on Navigation ---
  useEffect(() => {
    if (location.state?.message) {
      const message = location.state.message;
      // Clear location state immediately to prevent re-triggering toast
      window.history.replaceState({}, document.title);
      // Show the toast
      toast.success(message);
    }
  }, [location.state]); // Depend only on location.state

  // --- Render Logic ---
  let content;
  if (isLoading) {
    content = <p>Loading emails...</p>; // Replace with your Loader component if desired
  } else if (error) {
    content = <p style={{ color: "red" }}>Error: {error}</p>;
  } else if (emails.length === 0) {
    content = <p>Your inbox is empty.</p>;
  } else {
    content = (
      <InboxEmailList
        emails={emails}
        setEmails={setEmails} // Pass if needed for updates like delete/read
        selectedEmail={selectedEmail}
        setSelectedEmail={setSelectedEmail}
      />
    );
  }

  return (
    <div className="page-container">
      <div className="space-between-full-wid bottom-line-grey">
        <h1>Inbox</h1>
        {/* View Switch Button */}
        <div
          className="switch-button"
          onClick={() => setIsListView(!isListView)}
        >
          <div
            className={`switch-circle ${isListView ? "left" : "right"}`}
          ></div>
          <img
            src={listIcon}
            alt="List View"
            className={`switch-icon ${isListView ? "active" : "inactive"}`}
          />
          <img
            src={folderIcon}
            alt="Folder View"
            className={`switch-icon ${isListView ? "inactive" : "active"}`}
          />
        </div>
      </div>

      {/* Render Content based on View */}
      {isListView ? content : <FilterFolderPage />}
    </div>
  );
};

export default Inbox;
