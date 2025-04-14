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
  "/unread": { title: "Unread", api: "/api/mail/unread" },
  "/favorite": { title: "Favorite", api: "/api/mail/favorite" },
  "/sent": { title: "Sent", api: "/api/mail/sent" },
  "/search-results": {
    title: "Results",
    api: null,
  },
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
          { headers: { Authorization: `Bearer ${authToken}` } }
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

  const fetchEmails = useCallback(async () => {
    if (!authToken || !apiPath) return;

    if (!authToken) return;
    setIsLoading(true);
    try {
      const response = await fetch(`${API_BASE_URL}${apiPath}`, {
        headers: { Authorization: `Bearer ${authToken}` },
      });
      if (!response.ok) throw new Error("Failed to fetch emails");
      setEmails(await response.json());
    } catch (err) {
      setError(err.message);
      toast.error(err.message);
    } finally {
      setIsLoading(false);
    }
  }, [authToken, apiPath]);

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
      console.log("🧪 Search results from location.state:", resultsFromSearch);

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
      const preview = emails.find((e) => e.mailId === mailId);
      if (!preview || isFetchingFullEmail) return;

      setSelectedMailId(mailId);
      setFullEmailData(null);
      setIsEmailViewOpen(false);

      const data = await fetchFullEmail(mailId);
      if (data) {
        setFullEmailData(data);
        setIsEmailViewOpen(true);
      }
    },
    [emails, isFetchingFullEmail, fetchFullEmail]
  );

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
    listContent = <p>No emails to show.</p>;
  } else {
    listContent = (
      <InboxEmailList
        emails={emails}
        onEmailSelect={handleEmailSelect}
        onToggleFavorite={() => {}}
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
        onBack={() => navigate("/search")} // 👈 Back to the search page
      />
      {isListView ? listContent : <FilterFolderPage />}
      {isEmailViewOpen && fullEmailData && (
        <EmailView
          email={fullEmailData}
          onClose={() => {
            setIsEmailViewOpen(false);
            setFullEmailData(null);
            setSelectedMailId(null);
          }}
          onDelete={() => {}}
          onMarkUnread={() => {}}
          onToggleFavorite={() => {}}
          onReply={() => {}}
          onForward={() => {}}
        />
      )}
    </div>
  );
};

export default GenericEmailPage;
