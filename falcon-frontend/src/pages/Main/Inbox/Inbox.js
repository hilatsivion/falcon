import React, { useState, useEffect, useCallback } from "react";
import { toast } from "react-toastify";
import { useLocation, useNavigate } from "react-router-dom";
import FilterFolderPage from "../FilterFolderPage/FilterFolderPage";
import InboxEmailList from "./InboxEmailList";
import EmailView from "../../../components/EmailView/EmailView";
import Loader from "../../../components/Loader/Loader";
import listIcon from "../../../assets/icons/black/list.svg";
import folderIcon from "../../../assets/icons/black/folder.svg";
import "./Inbox.css";
import { API_BASE_URL } from "../../../config/constants";
import { useAuth } from "../../../context/AuthContext";

export const Tag = ({ name }) => {
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
  return (
    <span
      className="email-tag"
      style={{ backgroundColor: tagColors[name] || "#ddd" }}
    >
      {name}
    </span>
  );
};

const Inbox = () => {
  const { authToken, isAuthenticated } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();

  const [isListView, setIsListView] = useState(true);
  const [emails, setEmails] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState(null);
  const [selectedMailId, setSelectedMailId] = useState(null);
  const [fullEmailData, setFullEmailData] = useState(null);
  const [isEmailViewOpen, setIsEmailViewOpen] = useState(false);
  const [isFetchingFullEmail, setIsFetchingFullEmail] = useState(false);

  const fetchFullEmail = useCallback(
    async (mailId) => {
      if (!authToken || !mailId) return null;
      setIsFetchingFullEmail(true);
      setError(null);
      try {
        const response = await fetch(
          `${API_BASE_URL}/api/mail/received/full/${mailId}`,
          {
            headers: { Authorization: `Bearer ${authToken}` },
          }
        );
        if (!response.ok) {
          let errorMsg = `Failed to fetch email details (${response.status})`;
          try {
            const errData = await response.json();
            errorMsg = errData.message || errorMsg;
          } catch (e) {}
          throw new Error(errorMsg);
        }
        const data = await response.json();
        return data;
      } catch (err) {
        console.error("Fetch Full Email Error:", err);
        toast.error(`Could not load email details: ${err.message}`);
        return null;
      } finally {
        setIsFetchingFullEmail(false);
      }
    },
    [authToken]
  );

  const updateReadStatusAPI = useCallback(
    async (mailIds, isRead) => {
      if (!authToken || !mailIds || mailIds.length === 0) return false;
      const body = mailIds.map((id) => ({ mailId: id, isRead }));
      try {
        const response = await fetch(`${API_BASE_URL}/api/mail/read`, {
          method: "PUT",
          headers: {
            Authorization: `Bearer ${authToken}`,
            "Content-Type": "application/json",
          },
          body: JSON.stringify(body),
        });
        if (!response.ok) {
          throw new Error(`Failed to update read status (${response.status})`);
        }
        return true;
      } catch (err) {
        console.error("Update Read Status Error:", err);
        toast.error("Failed to update read status.");
        return false;
      }
    },
    [authToken]
  );

  const toggleFavoriteAPI = useCallback(
    async (mailId, newFavoriteStatus) => {
      if (!authToken || mailId === undefined) return false;
      try {
        const response = await fetch(
          `${API_BASE_URL}/api/mail/favorite/${mailId}/${newFavoriteStatus}`,
          {
            method: "PUT",
            headers: { Authorization: `Bearer ${authToken}` },
          }
        );
        if (!response.ok) {
          throw new Error(
            `Failed to update favorite status (${response.status})`
          );
        }
        return true;
      } catch (err) {
        console.error("Toggle Favorite Error:", err);
        return false;
      }
    },
    [authToken]
  );

  const deleteEmailAPI = useCallback(
    async (mailId, mailAccountId) => {
      if (!authToken || !mailId || !mailAccountId) return false;
      const body = [{ mailId, mailAccountId }];
      try {
        const response = await fetch(`${API_BASE_URL}/api/mail/delete`, {
          method: "DELETE",
          headers: {
            Authorization: `Bearer ${authToken}`,
            "Content-Type": "application/json",
          },
          body: JSON.stringify(body),
        });
        if (!response.ok && response.status !== 204) {
          throw new Error(`Failed to delete email (${response.status})`);
        }
        toast.success("Email deleted.");
        return true;
      } catch (err) {
        console.error("Delete Email Error:", err);
        toast.error("Failed to delete email.");
        return false;
      }
    },
    [authToken]
  );

  const fetchInboxPreviews = useCallback(async () => {
    if (!authToken) {
      setIsLoading(false);
      setEmails([]);
      return;
    }
    setIsLoading(true);
    setError(null);
    try {
      const response = await fetch(
        `${API_BASE_URL}/api/mail/received/preview`,
        { headers: { Authorization: `Bearer ${authToken}` } }
      );
      if (!response.ok) {
        let errorMsg = `Failed to fetch inbox (${response.status})`;
        try {
          const errData = await response.json();
          errorMsg = errData.message || errorMsg;
        } catch (e) {}
        throw new Error(errorMsg);
      }
      const data = await response.json();
      setEmails(data || []);
    } catch (err) {
      console.error("Inbox preview fetch error:", err);
      setError(err.message || "Failed to load inbox.");
      setEmails([]);
    } finally {
      setIsLoading(false);
    }
  }, [authToken]);

  useEffect(() => {
    if (isAuthenticated) {
      fetchInboxPreviews();
    } else {
      setEmails([]);
      setIsLoading(false);
      setError(null);
      setIsEmailViewOpen(false);
      setFullEmailData(null);
      setSelectedMailId(null);
    }
  }, [isAuthenticated, fetchInboxPreviews]);

  useEffect(() => {
    if (location.state?.message) {
      const message = location.state.message;
      window.history.replaceState({}, document.title);
      toast.success(message);
    }
  }, [location.state]);

  const handleEmailSelect = useCallback(
    async (mailId) => {
      const previewEmail = emails.find((e) => e.mailId === mailId);
      if (!previewEmail || isFetchingFullEmail) return;

      setSelectedMailId(mailId);
      setIsEmailViewOpen(false);
      setFullEmailData(null);

      let markedReadLocally = false;
      if (!previewEmail.isRead) {
        setEmails((prevEmails) =>
          prevEmails.map((e) =>
            e.mailId === mailId ? { ...e, isRead: true } : e
          )
        );
        markedReadLocally = true;
      }

      const fetchedData = await fetchFullEmail(mailId);

      if (fetchedData) {
        setFullEmailData(fetchedData);
        setIsEmailViewOpen(true);
        if (markedReadLocally) {
          updateReadStatusAPI([mailId], true);
        }
      } else {
        if (markedReadLocally) {
          setEmails((prevEmails) =>
            prevEmails.map((e) =>
              e.mailId === mailId ? { ...e, isRead: false } : e
            )
          );
        }
        setSelectedMailId(null);
      }
    },
    [emails, isFetchingFullEmail, fetchFullEmail, updateReadStatusAPI]
  );

  const handleToggleFavorite = useCallback(
    async (mailId, newFavoriteStatus) => {
      setEmails((prevEmails) =>
        prevEmails.map((e) =>
          e.mailId === mailId ? { ...e, isFavorite: newFavoriteStatus } : e
        )
      );
      if (isEmailViewOpen && fullEmailData?.mailId === mailId) {
        setFullEmailData((prev) =>
          prev ? { ...prev, isFavorite: newFavoriteStatus } : null
        );
      }

      const success = await toggleFavoriteAPI(mailId, newFavoriteStatus);

      if (!success) {
        toast.error("Failed to update favorite status");
        setEmails((prevEmails) =>
          prevEmails.map((e) =>
            e.mailId === mailId ? { ...e, isFavorite: !newFavoriteStatus } : e
          )
        );
        if (isEmailViewOpen && fullEmailData?.mailId === mailId) {
          setFullEmailData((prev) =>
            prev ? { ...prev, isFavorite: !newFavoriteStatus } : null
          );
        }
      }
    },
    [isEmailViewOpen, fullEmailData, toggleFavoriteAPI]
  );

  const handleDeleteEmail = useCallback(async () => {
    if (!fullEmailData) return;
    const { mailId, mailAccountId } = fullEmailData;
    const success = await deleteEmailAPI(mailId, mailAccountId);
    if (success) {
      setEmails((prevEmails) => prevEmails.filter((e) => e.mailId !== mailId));
      setIsEmailViewOpen(false);
      setFullEmailData(null);
      setSelectedMailId(null);
    }
  }, [fullEmailData, deleteEmailAPI]);

  const handleMarkUnread = useCallback(async () => {
    if (!fullEmailData) return;
    const { mailId } = fullEmailData;
    const success = await updateReadStatusAPI([mailId], false);
    if (success) {
      setEmails((prevEmails) =>
        prevEmails.map((e) =>
          e.mailId === mailId ? { ...e, isRead: false } : e
        )
      );
      setIsEmailViewOpen(false);
      setFullEmailData(null);
      setSelectedMailId(null);
    }
  }, [fullEmailData, updateReadStatusAPI]);

  const handleCloseEmailView = useCallback(() => {
    setIsEmailViewOpen(false);
    setFullEmailData(null);
    setSelectedMailId(null);
  }, []);

  const handleReply = useCallback(() => {
    if (!fullEmailData) return;
    let replyToAddress = fullEmailData.sender;
    const match = fullEmailData.sender?.match(/<(.+)>/);
    if (match && match[1]) replyToAddress = match[1];
    else if (fullEmailData.sender?.includes("@"))
      replyToAddress = fullEmailData.sender;
    navigate("/compose", {
      state: { to: replyToAddress, subject: `Re: ${fullEmailData.subject}` },
    });
  }, [navigate, fullEmailData]);

  const handleForward = useCallback(() => {
    if (!fullEmailData) return;
    const forwardBody = `\n\n---------- Forwarded message ----------\nFrom: ${
      fullEmailData.sender
    }\nDate: ${new Date(
      fullEmailData.timeReceived
    ).toLocaleString()}\nSubject: ${fullEmailData.subject}\n\n${
      fullEmailData.body || ""
    }`;
    navigate("/compose", {
      state: { subject: `Fwd: ${fullEmailData.subject}`, body: forwardBody },
    });
  }, [navigate, fullEmailData]);

  let listContent;
  if (isLoading) {
    listContent = (
      <div className="centered-loader">
        <Loader />
      </div>
    );
  } else if (error) {
    listContent = <p className="error-message padding-sides">{error}</p>;
  } else if (!isAuthenticated) {
    listContent = (
      <p className="padding-sides">Please log in to view your inbox.</p>
    );
  } else if (emails.length === 0) {
    listContent = <p className="padding-sides">Your inbox is empty.</p>;
  } else {
    listContent = (
      <InboxEmailList
        emails={emails}
        onEmailSelect={handleEmailSelect}
        onToggleFavorite={handleToggleFavorite}
      />
    );
  }

  return (
    <div className="page-container">
      <div className="space-between-full-wid bottom-line-grey">
        <h1>Inbox</h1>
        <div
          className="switch-button"
          onClick={() => setIsListView(!isListView)}
        >
          <div
            className={`switch-circle ${isListView ? "left" : "right"}`}
          ></div>
          <img
            src={listIcon}
            alt="List"
            className={`switch-icon ${isListView ? "active" : "inactive"}`}
          />
          <img
            src={folderIcon}
            alt="Folder"
            className={`switch-icon ${isListView ? "inactive" : "active"}`}
          />
        </div>
      </div>

      {isListView ? listContent : <FilterFolderPage />}

      {isFetchingFullEmail && (
        <div className="fullscreen-loader">
          <Loader />
        </div>
      )}

      {!isFetchingFullEmail && isEmailViewOpen && fullEmailData && (
        <EmailView
          email={fullEmailData}
          onClose={handleCloseEmailView}
          onDelete={handleDeleteEmail}
          onMarkUnread={handleMarkUnread}
          onToggleFavorite={() =>
            handleToggleFavorite(
              fullEmailData.mailId,
              !fullEmailData.isFavorite
            )
          }
          onReply={handleReply}
          onForward={handleForward}
        />
      )}
    </div>
  );
};

export default Inbox;
