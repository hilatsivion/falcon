import React, { useState, useEffect, useCallback, useRef } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { useAuth } from "../../../context/AuthContext";
import HeaderWithSwitch from "../../../components/HeaderWithSwitch/HeaderWithSwitch";
import EmailView from "../../../components/EmailView/EmailView";
import InboxEmailList from "./InboxEmailList";
import FilterFolderPage from "../FilterFolderPage/FilterFolderPage";
import Loader from "../../../components/Loader/Loader";
import { API_BASE_URL } from "../../../config/constants";
import { toast } from "react-toastify";

// Define mappings for different page types to their API endpoints and titles
const pageMap = {
  "/inbox": {
    title: "Inbox",
    api: "/api/mail/received/preview",
    dtoType: "MailReceivedPreviewDto",
    detailApiBase: "/api/mail/received/full/",
  },
  "/unread": {
    title: "Unread",
    api: "/api/mail/unread/preview?page=1&pageSize=100",
    dtoType: "MailReceivedPreviewDto",
    detailApiBase: "/api/mail/received/full/",
  },
  "/favorite": {
    title: "Favorite",
    api: "/api/mail/favorites/preview?page=1&pageSize=50",
    expectsCombinedFavorites: true,
    dtoType: "FavoriteEmailsDto",
    // Detail view needs logic to determine type for favorites/search
    detailApiBase: null, // Requires special handling
  },
  "/sent": {
    title: "Sent",
    api: "/api/mail/sent/preview?page=1&pageSize=100",
    dtoType: "MailSentPreviewDto",
    detailApiBase: "/api/mail/sent/full/", // Correct endpoint for sent details
  },
  "/search-results": {
    title: "Results",
    api: null,
    dtoType: "MailSearchResultDto",
    detailApiBase: null, // Requires special handling
  },
};

// Helper to map various DTOs to the props expected by EmailItem
const mapDtoToEmailItemProps = (dto, dtoType) => {
  switch (dtoType) {
    case "MailReceivedPreviewDto":
      return {
        mailId: dto.mailId,
        sender: dto.sender,
        subject: dto.subject,
        bodySnippet: dto.bodySnippet,
        tags: dto.tags || [],
        timeReceived: dto.timeReceived,
        isRead: dto.isRead,
        isFavorite: dto.isFavorite,
        mailAccountId: dto.mailAccountId,
        emailType: "received", // Add type hint
      };
    case "MailSentPreviewDto":
      return {
        mailId: dto.mailId,
        sender: "You",
        recipients: dto.recipients || [], // Keep recipients info
        subject: dto.subject,
        bodySnippet: dto.bodySnippet,
        tags: [],
        timeReceived: dto.timeSent, // Use timeSent
        isRead: true,
        isFavorite: dto.isFavorite,
        mailAccountId: dto.mailAccountId,
        emailType: "sent", // Add type hint
      };
    case "EmailSummaryDto": // DTO from /api/mail/filters/{id}/emails (assumed received)
      return {
        mailId: dto.mailId,
        sender: dto.senderEmail,
        subject: dto.subject,
        bodySnippet: dto.bodyPreview,
        tags: dto.tags ? dto.tags.map((tag) => tag.name) : [],
        timeReceived: dto.timeReceived,
        isRead: dto.isRead,
        isFavorite: false, // Not available
        mailAccountId: dto.mailAccountId || null,
        emailType: "received", // Assume filtered emails are received
      };
    case "MailSearchResultDto":
      // Search results can be mixed, include 'type' field from DTO
      const isSent = dto.type === "sent";
      return {
        mailId: dto.mailId,
        // Display sender if received, or first recipient if sent
        sender: isSent
          ? dto.recipients && dto.recipients.length > 0
            ? `To: ${dto.recipients[0]}`
            : "You"
          : dto.sender,
        recipients: dto.recipients || [],
        subject: dto.subject,
        bodySnippet: dto.bodySnippet,
        tags: dto.tags || [],
        timeReceived: dto.date,
        isRead: dto.isRead !== undefined ? dto.isRead : true,
        isFavorite: dto.isFavorite || false,
        mailAccountId: dto.mailAccountId,
        emailType: dto.type, // Crucial: pass the type from search results
      };
    default:
      console.warn("Unknown DTO type for mapping:", dtoType);
      return { ...dto, emailType: "unknown" }; // Add default type hint
  }
};

// Helper to map detail DTOs to the props expected by EmailView
const mapDetailDtoToEmailViewProps = (detailDto, emailType) => {
  if (!detailDto) return null;

  if (emailType === "received") {
    // Map MailReceivedDto to EmailView props
    return {
      mailId: detailDto.mailId,
      sender: detailDto.sender,
      recipients: detailDto.recipients || [],
      subject: detailDto.subject,
      body: detailDto.body,
      timeReceived: detailDto.timeReceived, // Use timeReceived
      isRead: detailDto.isRead,
      isFavorite: detailDto.isFavorite,
      tags: detailDto.tags || [], // Assuming TagDto structure
      attachments: detailDto.attachments || [],
      mailAccountId: detailDto.mailAccountId,
      emailType: "received",
    };
  } else if (emailType === "sent") {
    // Map MailSentDto to EmailView props
    return {
      mailId: detailDto.mailId,
      sender: "You", // Sender is always 'You' for sent items view
      recipients: detailDto.recipients || [],
      subject: detailDto.subject,
      body: detailDto.body,
      timeReceived: detailDto.timeSent, // Use timeSent for display consistency
      isRead: true, // Sent items are conceptually 'read' by the sender
      isFavorite: detailDto.isFavorite,
      tags: [], // Sent items don't have tags in this structure
      attachments: detailDto.attachments || [],
      mailAccountId: detailDto.mailAccountId,
      emailType: "sent",
    };
  } else {
    console.warn("Unknown email type for detail mapping:", emailType);
    // Fallback or default mapping if needed
    return { ...detailDto, emailType };
  }
};

const GenericEmailPage = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const { id: filterIdFromParams } = useParams();
  const pathname = location.pathname;
  const { authToken, isAuthenticated } = useAuth();
  const toastShownRef = useRef(false);

  const [isListView, setIsListView] = useState(true);
  const [emails, setEmails] = useState([]); // Holds mapped data for EmailItem
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState(null);
  const [selectedMailId, setSelectedMailId] = useState(null);
  const [selectedEmailType, setSelectedEmailType] = useState(null); // Store type of selected email
  const [fullEmailData, setFullEmailData] = useState(null); // Holds mapped data for EmailView
  const [isEmailViewOpen, setIsEmailViewOpen] = useState(false);
  const [isFetchingFullEmail, setIsFetchingFullEmail] = useState(false);

  const pathConfigRef = useRef({
    apiPath: null,
    expectedDtoType: null,
    title: "Inbox",
    detailApiBase: null,
  });

  useEffect(() => {
    let currentTitle = "Inbox";
    let currentApiPath = pageMap["/inbox"]?.api;
    let currentDtoType = pageMap["/inbox"]?.dtoType;
    let currentDetailApiBase = pageMap["/inbox"]?.detailApiBase;
    let currentIsListView = true;

    if (pageMap[pathname]) {
      currentTitle = pageMap[pathname].title;
      currentApiPath = pageMap[pathname].api;
      currentDtoType = pageMap[pathname].dtoType;
      currentDetailApiBase = pageMap[pathname].detailApiBase;
    } else if (pathname.startsWith("/filters/")) {
      currentTitle = location.state?.name || "Filter Folder";
      currentApiPath = `/api/mail/filters/${filterIdFromParams}/emails`;
      currentDtoType = "EmailSummaryDto";
      // Assume filtered emails are received for detail view for now
      currentDetailApiBase = "/api/mail/received/full/";
      currentIsListView = true;
    } else if (pathname === "/search-results") {
      currentTitle = "Results";
      currentApiPath = null;
      currentDtoType = "MailSearchResultDto";
      // Detail view needs logic based on the *specific* search result type
      currentDetailApiBase = null;
      currentIsListView = true;
    }

    pathConfigRef.current = {
      apiPath: currentApiPath,
      expectedDtoType: currentDtoType,
      title: currentTitle,
      detailApiBase: currentDetailApiBase,
    };

    setIsListView((prev) => {
      if (pathname.startsWith("/filters/") || pathname === "/search-results") {
        return true;
      }
      if (pathname === "/inbox" && location.state?.view === "filters") {
        return false;
      }
      return prev;
    });
  }, [pathname, filterIdFromParams, location.state]);

  // --- API Callbacks ---
  const fetchFullEmail = useCallback(
    async (mailId, emailType) => {
      // Accept emailType as argument
      if (!authToken || !mailId) return null;

      let detailEndpointBase = pathConfigRef.current.detailApiBase;

      // Special handling for search/favorites where type is determined per item
      if (!detailEndpointBase && emailType) {
        detailEndpointBase =
          emailType === "sent"
            ? "/api/mail/sent/full/"
            : "/api/mail/received/full/";
      } else if (!detailEndpointBase) {
        console.error(
          "Cannot determine detail API endpoint for this view/email type."
        );
        toast.error("Could not load email details: Configuration error.");
        return null;
      }

      const endpoint = `${API_BASE_URL}${detailEndpointBase}${mailId}`;

      setIsFetchingFullEmail(true);
      setError(null);
      try {
        const response = await fetch(endpoint, {
          headers: { Authorization: `Bearer ${authToken}` },
        });
        if (!response.ok) {
          const errorText = await response.text();
          throw new Error(
            `Failed to fetch full email (${emailType}) from ${endpoint}: ${errorText} (Status: ${response.status})`
          );
        }
        // Map the raw DTO to the structure EmailView expects
        const rawDetailDto = await response.json();
        return mapDetailDtoToEmailViewProps(rawDetailDto, emailType); // Map using the determined type
      } catch (err) {
        console.error("Fetch Full Email Error:", err);
        toast.error(`Could not load email details: ${err.message}`);
        return null;
      } finally {
        setIsFetchingFullEmail(false);
      }
    },
    [authToken] // Depends only on authToken
  );

  const updateReadStatusAPI = useCallback(
    async (mailIds, isRead) => {
      // (Implementation unchanged)
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
        if (!response.ok)
          throw new Error(
            `Failed to update read status (Status: ${response.status})`
          );
        return true;
      } catch (err) {
        console.error("Update Read Status Error:", err);
        toast.error(`Failed to update read status: ${err.message}`);
        return false;
      }
    },
    [authToken]
  );

  const toggleFavoriteAPI = useCallback(
    async (mailId, newFavoriteStatus) => {
      // (Implementation unchanged)
      if (!authToken || mailId === undefined) return false;
      try {
        const response = await fetch(
          `${API_BASE_URL}/api/mail/favorite/${mailId}/${newFavoriteStatus}`,
          {
            method: "PUT",
            headers: { Authorization: `Bearer ${authToken}` },
          }
        );
        if (!response.ok)
          throw new Error(
            `Failed to update favorite status (Status: ${response.status})`
          );
        return true;
      } catch (err) {
        console.error("Toggle Favorite Error:", err);
        // Let calling function handle UI reversion/toast
        return false;
      }
    },
    [authToken]
  );

  const deleteEmailAPI = useCallback(
    async (mailId, mailAccountId) => {
      // (Implementation unchanged, relies on mailAccountId being passed correctly)
      if (!authToken || !mailId || !mailAccountId) {
        console.warn(
          "Delete failed: Missing mailId or mailAccountId. MailAccountId:",
          mailAccountId
        );
        toast.error("Cannot delete email: Missing necessary information.");
        return false;
      }
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
          throw new Error(
            `Failed to delete email (Status: ${response.status})`
          );
        toast.success("Email deleted.");
        return true;
      } catch (err) {
        console.error("Delete Email Error:", err);
        toast.error(`Failed to delete email: ${err.message}`);
        return false;
      }
    },
    [authToken]
  );

  // --- Fetch Logic ---
  const fetchData = useCallback(async () => {
    const { apiPath, expectedDtoType } = pathConfigRef.current;

    if (!isAuthenticated || (!apiPath && pathname !== "/search-results")) {
      setEmails([]);
      setIsLoading(false);
      setError(null);
      return;
    }

    if (pathname === "/search-results") {
      const resultsFromSearch = location.state?.results || [];
      const mappedResults = resultsFromSearch.map((dto) =>
        mapDtoToEmailItemProps(dto, "MailSearchResultDto")
      );
      setEmails(mappedResults);
      setIsLoading(false);
      setError(null);
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const response = await fetch(`${API_BASE_URL}${apiPath}`, {
        headers: { Authorization: `Bearer ${authToken}` },
      });

      if (!response.ok) {
        throw new Error(
          `Failed to fetch data from ${apiPath} (Status: ${response.status})`
        );
      }
      const data = await response.json();

      let mappedEmails = [];
      if (pageMap[pathname]?.expectsCombinedFavorites) {
        const received = data.receivedFavorites || [];
        const sent = data.sentFavorites || [];
        mappedEmails = [
          ...received.map((dto) =>
            mapDtoToEmailItemProps(dto, "MailReceivedPreviewDto")
          ),
          ...sent.map((dto) =>
            mapDtoToEmailItemProps(dto, "MailSentPreviewDto")
          ),
        ];
        mappedEmails.sort(
          (a, b) => new Date(b.timeReceived) - new Date(a.timeReceived)
        );
      } else if (Array.isArray(data)) {
        mappedEmails = data.map((dto) =>
          mapDtoToEmailItemProps(dto, expectedDtoType)
        );
      } else {
        console.warn("Received non-array data from API:", data);
      }

      setEmails(mappedEmails);
    } catch (err) {
      console.error(`FetchData Error (${apiPath}):`, err);
      setError(err.message);
      toast.error(`Failed to load emails: ${err.message}`);
      setEmails([]);
    } finally {
      setIsLoading(false);
    }
  }, [authToken, isAuthenticated, pathname, location.state?.results]); // Dependencies are stable values or from React Router

  // --- Effects ---
  useEffect(() => {
    const message = location.state?.message;
    if (message && !toastShownRef.current) {
      toastShownRef.current = true;
      toast.success(message);
      window.history.replaceState(
        { ...location.state, message: undefined },
        document.title
      );
    }
  }, [location.state]);

  useEffect(() => {
    if (isAuthenticated) {
      fetchData();
    } else {
      setEmails([]);
      setError(null);
      setIsLoading(false);
      setIsEmailViewOpen(false);
      setFullEmailData(null);
      setSelectedMailId(null);
    }
    toastShownRef.current = false;
  }, [isAuthenticated, fetchData, pathname, filterIdFromParams]); // Effect runs when auth, path, or filter changes

  // --- Event Handlers ---
  const handleEmailSelect = useCallback(
    async (mailId) => {
      // Find the mapped email item to get its type hint
      const previewEmail = emails.find((e) => e.mailId === mailId);
      if (!previewEmail || isFetchingFullEmail) return;

      // Store the type for fetching the correct detail DTO
      setSelectedEmailType(previewEmail.emailType);
      setSelectedMailId(mailId);
      setIsEmailViewOpen(false);
      setFullEmailData(null);

      let markedReadLocally = false;
      // Only perform optimistic UI update if the email type supports 'isRead'
      if (previewEmail.isRead !== undefined && !previewEmail.isRead) {
        setEmails((prev) =>
          prev.map((e) => (e.mailId === mailId ? { ...e, isRead: true } : e))
        );
        markedReadLocally = true;
      }

      // Fetch using the determined email type
      const fetchedMappedData = await fetchFullEmail(
        mailId,
        previewEmail.emailType
      );
      if (fetchedMappedData) {
        setFullEmailData(fetchedMappedData); // Set the *mapped* data for EmailView
        setIsEmailViewOpen(true);
        if (markedReadLocally && previewEmail.isRead !== undefined) {
          updateReadStatusAPI([mailId], true); // Confirm read status with API
        }
      } else {
        // Revert optimistic update if fetch failed
        if (markedReadLocally && previewEmail.isRead !== undefined) {
          setEmails((prev) =>
            prev.map((e) => (e.mailId === mailId ? { ...e, isRead: false } : e))
          );
        }
        setSelectedMailId(null);
        setSelectedEmailType(null);
      }
    },
    // Dependencies: Need emails list, isFetching, and the stable API callbacks
    [emails, isFetchingFullEmail, fetchFullEmail, updateReadStatusAPI]
  );

  const handleToggleFavorite = useCallback(
    async (mailId, newFavoriteStatus) => {
      const emailToToggle = emails.find((e) => e.mailId === mailId);
      if (!emailToToggle || emailToToggle.isFavorite === undefined) {
        return;
      }
      const originalStatus = emailToToggle.isFavorite;

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
            e.mailId === mailId ? { ...e, isFavorite: originalStatus } : e
          )
        );
        if (isEmailViewOpen && fullEmailData?.mailId === mailId) {
          setFullEmailData((prev) =>
            prev ? { ...prev, isFavorite: originalStatus } : null
          );
        }
      }
    },
    [emails, isEmailViewOpen, fullEmailData, toggleFavoriteAPI]
  );

  const handleDeleteEmail = useCallback(async () => {
    if (!fullEmailData) return;
    // Use mailAccountId from the mapped fullEmailData
    const { mailId, mailAccountId } = fullEmailData;
    if (!mailAccountId) {
      toast.error("Cannot delete: Mail Account ID is missing.");
      return;
    }
    const success = await deleteEmailAPI(mailId, mailAccountId);
    if (success) {
      // Remove from the *mapped* emails list
      setEmails((prev) => prev.filter((e) => e.mailId !== mailId));
      setIsEmailViewOpen(false);
      setFullEmailData(null);
      setSelectedMailId(null);
      setSelectedEmailType(null);
    }
  }, [fullEmailData, deleteEmailAPI]); // Depends on fullEmailData and deleteEmailAPI

  const handleMarkUnread = useCallback(async () => {
    // (Implementation unchanged, relies on fullEmailData having isRead)
    if (!fullEmailData || fullEmailData.isRead === undefined) return;
    const { mailId } = fullEmailData;
    const success = await updateReadStatusAPI([mailId], false);
    if (success) {
      setEmails((prev) =>
        prev.map((e) => (e.mailId === mailId ? { ...e, isRead: false } : e))
      );
      setIsEmailViewOpen(false);
      setFullEmailData(null);
      setSelectedMailId(null);
      setSelectedEmailType(null);
    }
  }, [fullEmailData, updateReadStatusAPI]); // Depends on fullEmailData and updateReadStatusAPI

  const handleCloseEmailView = useCallback(() => {
    // (Implementation unchanged)
    setIsEmailViewOpen(false);
    setFullEmailData(null);
    setSelectedMailId(null);
    setSelectedEmailType(null);
  }, []);

  const handleReply = useCallback(() => {
    // (Implementation unchanged, relies on fullEmailData structure)
    if (!fullEmailData || fullEmailData.sender === undefined) return;
    let replyToAddress = fullEmailData.sender;
    // Handle "You" as sender for sent items - can't reply to yourself easily here
    if (replyToAddress === "You") {
      toast.info("Replying to yourself? Try forwarding instead.");
      return;
    }
    const match = fullEmailData.sender?.match(/<(.+)>/);
    if (match && match[1]) replyToAddress = match[1];
    else if (fullEmailData.sender?.includes("@"))
      replyToAddress = fullEmailData.sender;
    navigate("/compose", {
      state: { to: replyToAddress, subject: `Re: ${fullEmailData.subject}` },
    });
  }, [navigate, fullEmailData]); // Depends on navigate and fullEmailData

  const handleForward = useCallback(() => {
    // (Implementation unchanged, relies on fullEmailData structure)
    if (!fullEmailData) return;
    const time =
      fullEmailData.timeReceived || fullEmailData.timeSent || new Date();
    const forwardBody = `\n\n---------- Forwarded message ----------\nFrom: ${
      fullEmailData.sender || "Unknown Sender"
    }\nDate: ${new Date(time).toLocaleString()}\nSubject: ${
      fullEmailData.subject
    }\n\n${fullEmailData.body || ""}`;
    navigate("/compose", {
      state: { subject: `Fwd: ${fullEmailData.subject}`, body: forwardBody },
    });
  }, [navigate, fullEmailData]); // Depends on navigate and fullEmailData

  // --- Render Logic ---
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
      <p className="padding-sides">Please log in to view emails.</p>
    );
  } else if (emails.length === 0) {
    listContent = (
      <p className="padding-sides inbox-empty">No emails to show.</p>
    );
  } else {
    listContent = (
      <InboxEmailList
        emails={emails} // Pass the mapped emails
        onEmailSelect={handleEmailSelect}
        onToggleFavorite={handleToggleFavorite}
      />
    );
  }

  const shouldShowToggle =
    !pathname.startsWith("/filters/") && pathname !== "/search-results";

  let currentHeaderTitle = pathConfigRef.current.title; // Get the default title for the path
  // Check if we are on the Inbox path AND the view is toggled to show FilterFolderPage
  if (pathname === "/inbox" && !isListView) {
    currentHeaderTitle = "Filters"; // Or "Manage Filters", "Filter Folders" - choose the title you prefer
  }

  return (
    <div className="page-container">
      <HeaderWithSwitch
        title={currentHeaderTitle}
        isListView={isListView}
        onToggleView={
          shouldShowToggle ? () => setIsListView(!isListView) : undefined
        }
        showBackButton={
          pathname.startsWith("/filters/") || pathname === "/search-results"
        }
        onBack={() => {
          if (pathname.startsWith("/filters/")) {
            navigate("/inbox");
          } else if (pathname === "/search-results") {
            navigate("/search");
          } else {
            navigate("/inbox");
          }
        }}
        colorBar={
          pathname.startsWith("/filters/") ? location.state?.color : null
        }
      />

      {isListView
        ? listContent
        : pathname === "/inbox" && (
            <FilterFolderPage setIsListView={setIsListView} />
          )}

      {isFetchingFullEmail && (
        <div className="fullscreen-loader">
          <Loader />
        </div>
      )}

      {!isFetchingFullEmail && isEmailViewOpen && fullEmailData && (
        <EmailView
          email={fullEmailData} // Pass the *mapped* full data
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
