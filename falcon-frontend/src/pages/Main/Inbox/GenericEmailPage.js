import React, { useState, useEffect, useCallback, useRef } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../../../context/AuthContext";
import HeaderWithSwitch from "../../../components/HeaderWithSwitch/HeaderWithSwitch";
import EmailView from "../../../components/EmailView/EmailView";
import InboxEmailList from "./InboxEmailList";
import FilterFolderPage from "../FilterFolderPage/FilterFolderPage";
import Loader from "../../../components/Loader/Loader";
import { API_BASE_URL } from "../../../config/constants";
import { toast } from "react-toastify";

const pageMap = {
  "/inbox": { title: "Inbox", api: "/api/mail/received/preview" },
  "/unread": {
    title: "Unread",
    api: "/api/mail/unread/preview?page=1&pageSize=100",
  },
  "/favorite": {
    title: "Favorite",
    api: "/api/mail/favorites/preview?page=1&pageSize=50",
    expectsCombinedFavorites: true,
  },
  "/sent": {
    title: "Sent",
    api: "/api/mail/sent/preview?page=1&pageSize=100",
  },
  "/search-results": { title: "Results", api: null },
  "/filter-results": {
    title: "Filtered Results",
    api: "/api/mail/filter/results",
  },
};

const GenericEmailPage = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const pathname = location.pathname;
  const { authToken, isAuthenticated } = useAuth();
  const toastShownRef = useRef(false);

  const [isListView, setIsListView] = useState(true);
  const [emails, setEmails] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState(null);
  const [selectedMailId, setSelectedMailId] = useState(null);
  const [fullEmailData, setFullEmailData] = useState(null);
  const [isEmailViewOpen, setIsEmailViewOpen] = useState(false);
  const [isFetchingFullEmail, setIsFetchingFullEmail] = useState(false);

  const { title, api: apiPath } = pageMap[pathname] || {
    title: "Inbox",
    api: "/api/mail/received/preview",
  };

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
        if (!response.ok) throw new Error("Failed to fetch full email");
        return await response.json();
      } catch (err) {
        toast.error(`Could not load email: ${err.message}`);
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
        if (!response.ok) throw new Error("Failed to update read status");
        return true;
      } catch (err) {
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
        if (!response.ok) throw new Error("Failed to update favorite status");
        return true;
      } catch (err) {
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
        if (!response.ok && response.status !== 204)
          throw new Error("Failed to delete email");
        toast.success("Email deleted.");
        return true;
      } catch (err) {
        toast.error("Failed to delete email.");
        return false;
      }
    },
    [authToken]
  );

  const fetchEmails = useCallback(async () => {
    if (!authToken || !apiPath) return;

    setIsLoading(true);
    try {
      const response = await fetch(`${API_BASE_URL}${apiPath}`, {
        headers: { Authorization: `Bearer ${authToken}` },
      });
      if (!response.ok) throw new Error("Failed to fetch emails");

      const data = await response.json();

      // 👇 Handle the special case of Favorite page
      if (pageMap[pathname]?.expectsCombinedFavorites) {
        const { receivedFavorites = [], sentFavorites = [] } = data;
        setEmails([...receivedFavorites, ...sentFavorites]);
      } else {
        setEmails(data);
      }
    } catch (err) {
      setError(err.message);
      toast.error(err.message);
    } finally {
      setIsLoading(false);
    }
  }, [authToken, apiPath, pathname]);

  useEffect(() => {
    if (isAuthenticated) fetchEmails();
  }, [isAuthenticated, fetchEmails]);

  useEffect(() => {
    const message = location.state?.message;
    if (message && !toastShownRef.current) {
      toastShownRef.current = true;
      toast.success(message);
      window.history.replaceState({}, document.title);
    }
  }, [location.state]);

  useEffect(() => {
    if (!isAuthenticated) return;
    if (pathname === "/search-results") {
      const resultsFromSearch = location.state?.results;
      if (resultsFromSearch && Array.isArray(resultsFromSearch)) {
        setEmails(resultsFromSearch);
      } else {
        setEmails([]);
      }
    } else {
      fetchEmails();
    }
  }, [isAuthenticated, fetchEmails, pathname, location.state]);

  const handleEmailSelect = useCallback(
    async (mailId) => {
      const previewEmail = emails.find((e) => e.mailId === mailId);
      if (!previewEmail || isFetchingFullEmail) return;
      setSelectedMailId(mailId);
      setIsEmailViewOpen(false);
      setFullEmailData(null);

      let markedReadLocally = false;
      if (!previewEmail.isRead) {
        setEmails((prev) =>
          prev.map((e) => (e.mailId === mailId ? { ...e, isRead: true } : e))
        );
        markedReadLocally = true;
      }

      const fetchedData = await fetchFullEmail(mailId);
      if (fetchedData) {
        setFullEmailData(fetchedData);
        setIsEmailViewOpen(true);
        if (markedReadLocally) updateReadStatusAPI([mailId], true);
      } else {
        if (markedReadLocally) {
          setEmails((prev) =>
            prev.map((e) => (e.mailId === mailId ? { ...e, isRead: false } : e))
          );
        }
        setSelectedMailId(null);
      }
    },
    [emails, isFetchingFullEmail, fetchFullEmail, updateReadStatusAPI]
  );

  const handleToggleFavorite = useCallback(
    async (mailId, newFavoriteStatus) => {
      setEmails((prev) =>
        prev.map((e) =>
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
        setEmails((prev) =>
          prev.map((e) =>
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
      setEmails((prev) => prev.filter((e) => e.mailId !== mailId));
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
      setEmails((prev) =>
        prev.map((e) => (e.mailId === mailId ? { ...e, isRead: false } : e))
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
    listContent = <p className="error-message">{error}</p>;
  } else if (!isAuthenticated) {
    listContent = <p>Please log in to view emails.</p>;
  } else if (emails.length === 0) {
    listContent = <p className="error-message">No emails to show.</p>;
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
      <HeaderWithSwitch
        title={title}
        isListView={isListView}
        onToggleView={() => setIsListView(!isListView)}
        showBackButton={pathname === "/search-results"}
        onBack={() => navigate("/search")}
      />
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

export default GenericEmailPage;
