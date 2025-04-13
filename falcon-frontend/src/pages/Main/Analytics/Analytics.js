import React, { useState, useEffect } from "react";
import { ReactComponent as ArrowDownIcon } from "../../../assets/icons/black/arrow-down.svg";
import "./Analytics.css";

// --- Import Components, Constants & Context ---
import InsightCard from "../../../components/InsightCard/InsightCard";
import Loader from "../../../components/Loader/Loader";
import { API_BASE_URL } from "../../../config/constants";
import { useAuth } from "../../../context/AuthContext"; // Use Auth context

// --- Import Icons ---
import { ReactComponent as ClockIcon } from "../../../assets/icons/black/clock-icon.svg";
import { ReactComponent as SentIcon } from "../../../assets/icons/black/mail-sent.svg";
import { ReactComponent as ReceivedIcon } from "../../../assets/icons/black/mail-received.svg";
import { ReactComponent as SpamIcon } from "../../../assets/icons/black/spam.svg";
import { ReactComponent as TrashIcon } from "../../../assets/icons/black/trash-black.svg";
import { ReactComponent as ReadEmailIcon } from "../../../assets/icons/black/glasses.svg";
import { ReactComponent as StreakIcon } from "../../../assets/icons/black/streak.svg";

// --- Helper function to format minutes ---
const formatMinutes = (totalMinutes) => {
  if (
    totalMinutes === null ||
    totalMinutes === undefined ||
    isNaN(totalMinutes) ||
    totalMinutes === 0
  )
    return "0m";
  const isNegative = totalMinutes < 0;
  const absMinutes = Math.abs(totalMinutes);
  const hours = Math.floor(absMinutes / 60);
  const minutes = Math.floor(absMinutes % 60);
  let result = isNegative ? "-" : "";
  if (hours > 0) {
    result += `${hours}h `;
  }
  if (hours === 0 || minutes > 0) {
    result += `${minutes}m`;
  }
  if (result === "-" || result === "") return "0m";
  return result.trim();
};

// --- Helper function for Absolute Change ---
const calculateAbsoluteChange = (current, previous) => {
  if (
    previous === null ||
    previous === undefined ||
    current === null ||
    current === undefined ||
    isNaN(current) ||
    isNaN(previous)
  ) {
    return { difference: null, isPositive: null }; // Cannot calculate
  }
  // Round difference for whole numbers like emails, keep float for time
  const difference =
    typeof current === "number" &&
    typeof previous === "number" &&
    (Number.isInteger(current) || Number.isInteger(previous))
      ? Math.round(current - previous)
      : current - previous;

  let isPositive = null;
  if (difference > 0) isPositive = true;
  else if (difference < 0) isPositive = false;

  return { difference, isPositive };
};

const Analytics = () => {
  // --- State ---
  const [isEditMode, setIsEditMode] = useState(false);
  const [isActivityExpanded, setIsActivityExpanded] = useState(true);
  const [isGraphExpanded, setIsGraphExpanded] = useState(true);
  const [isLoading, setIsLoading] = useState(true);
  const [analyticsData, setAnalyticsData] = useState(null);
  const [error, setError] = useState(null);
  const { authToken, isAuthenticated, logout } = useAuth(); // Get auth state

  // Load hidden IDs from localStorage
  const [hiddenInsightIds, setHiddenInsightIds] = useState(() => {
    const savedHiddenIds = localStorage.getItem("hiddenAnalyticsIds");
    try {
      return savedHiddenIds ? new Set(JSON.parse(savedHiddenIds)) : new Set();
    } catch (e) {
      console.error("Failed to parse hiddenAnalyticsIds from localStorage", e);
      return new Set();
    }
  });

  // --- Fetch Data Effect ---
  useEffect(() => {
    const fetchAnalytics = async () => {
      if (!isAuthenticated || !authToken) {
        setError("Authentication required to view analytics.");
        setIsLoading(false);
        setAnalyticsData(null);
        return;
      }

      setIsLoading(true);
      setError(null);
      try {
        const response = await fetch(`${API_BASE_URL}/api/analytics`, {
          method: "GET",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${authToken}`, // Use token from context
          },
        });

        if (!response.ok) {
          if (response.status === 401) {
            setError("Session expired or invalid. Please log in again.");
            logout(); // Use logout from context
            return;
          }
          let errorText = `Failed to fetch analytics (${response.status})`;
          try {
            const errData = await response.json();
            errorText = errData.message || errorText;
          } catch (e) {}
          throw new Error(errorText);
        }
        const data = await response.json();
        setAnalyticsData(data);
      } catch (err) {
        console.error("Error fetching analytics:", err);
        setError(
          err.message || "An error occurred while fetching analytics data."
        );
        setAnalyticsData(null);
      } finally {
        setIsLoading(false);
      }
    };

    fetchAnalytics();
  }, [authToken, isAuthenticated, logout]); // Dependency array includes context values

  // --- Prepare Insight Data ---
  const allAvailableInsights = analyticsData
    ? [
        // Time Spent Today
        (() => {
          const { difference, isPositive } = calculateAbsoluteChange(
            analyticsData.timeSpentToday,
            analyticsData.timeSpentYesterday
          );
          let changeString = "N/A";
          if (difference !== null) {
            const sign = difference > 0 ? "+" : "";
            changeString = `${sign}${formatMinutes(difference)}`;
          }
          return {
            id: "timeToday",
            title: "Time Spent Today",
            value: formatMinutes(analyticsData.timeSpentToday),
            icon: ClockIcon,
            change: changeString,
            isPositive: isPositive,
          };
        })(),
        // Time Spent This Week
        (() => {
          const { difference, isPositive } = calculateAbsoluteChange(
            analyticsData.timeSpentThisWeek,
            analyticsData.timeSpentLastWeek
          );
          let changeString = "N/A";
          if (difference !== null) {
            const sign = difference > 0 ? "+" : "";
            changeString = `${sign}${formatMinutes(difference)}`;
          }
          return {
            id: "timeWeekly",
            title: "Time Spent This Week",
            value: formatMinutes(analyticsData.timeSpentThisWeek),
            icon: ClockIcon,
            change: changeString,
            isPositive: isPositive,
          };
        })(),
        // Emails Sent Weekly
        (() => {
          const { difference, isPositive } = calculateAbsoluteChange(
            analyticsData.emailsSentWeekly,
            analyticsData.emailsSentLastWeek
          );
          let changeString = "N/A";
          if (difference !== null) {
            const sign = difference > 0 ? "+" : "";
            changeString = `${sign}${difference}`;
          }
          return {
            id: "emailsSentWeekly",
            title: "Emails Sent Weekly",
            value: analyticsData.emailsSentWeekly?.toString() ?? "0",
            icon: SentIcon,
            change: changeString,
            isPositive: isPositive,
          };
        })(),
        // Emails Received Weekly
        (() => {
          const { difference, isPositive } = calculateAbsoluteChange(
            analyticsData.emailsReceivedWeekly,
            analyticsData.emailsReceivedLastWeek
          );
          let changeString = "N/A";
          if (difference !== null) {
            const sign = difference > 0 ? "+" : "";
            changeString = `${sign}${difference}`;
          }
          return {
            id: "emailsReceivedWeekly",
            title: "Emails Received Weekly",
            value: analyticsData.emailsReceivedWeekly?.toString() ?? "0",
            icon: ReceivedIcon,
            change: changeString,
            isPositive: isPositive,
          };
        })(),
        // Emails Read Weekly
        (() => {
          const { difference, isPositive } = calculateAbsoluteChange(
            analyticsData.readEmailsWeekly,
            analyticsData.readEmailsLastWeek
          );
          let changeString = "N/A";
          if (difference !== null) {
            const sign = difference > 0 ? "+" : "";
            changeString = `${sign}${difference}`;
          }
          return {
            id: "emailsReadWeekly",
            title: "Emails Read Weekly",
            value: analyticsData.readEmailsWeekly?.toString() ?? "0",
            icon: ReadEmailIcon,
            change: changeString,
            isPositive: isPositive,
          };
        })(),
        // Spam Emails Weekly
        (() => {
          const { difference, isPositive } = calculateAbsoluteChange(
            analyticsData.spamEmailsWeekly,
            analyticsData.spamEmailsLastWeek
          );
          let changeString = "N/A";
          if (difference !== null) {
            const sign = difference > 0 ? "+" : "";
            changeString = `${sign}${difference}`;
          }
          return {
            id: "spamEmailsWeekly",
            title: "Spam Emails Weekly",
            value: analyticsData.spamEmailsWeekly?.toString() ?? "0",
            icon: SpamIcon,
            change: changeString,
            isPositive: isPositive,
          };
        })(),
        // Deleted Emails Weekly
        (() => {
          const { difference, isPositive } = calculateAbsoluteChange(
            analyticsData.deletedEmailsWeekly,
            analyticsData.deletedEmailsLastWeek
          );
          let changeString = "N/A";
          if (difference !== null) {
            const sign = difference > 0 ? "+" : "";
            changeString = `${sign}${difference}`;
          }
          return {
            id: "deletedEmailsWeekly",
            title: "Deleted Emails Weekly",
            value: analyticsData.deletedEmailsWeekly?.toString() ?? "0",
            icon: TrashIcon,
            change: changeString,
            isPositive: isPositive,
          };
        })(),
        // Current Streak
        {
          id: "currentStreak",
          title: "Current Daily Streak",
          value: `${analyticsData.currentStreak ?? 0} ${
            analyticsData.currentStreak === 1 ? "day" : "days"
          }`,
          icon: StreakIcon,
          change: `Longest: ${analyticsData.longestStreak ?? 0} days`,
          isPositive: null,
        },
      ]
    : [];

  // Filter insights based on edit mode and hidden IDs
  const insightsToDisplay = isEditMode
    ? allAvailableInsights
    : allAvailableInsights.filter(
        (insight) => !hiddenInsightIds.has(insight.id)
      );

  // --- Edit Mode Handlers ---
  const handleEditToggle = () => setIsEditMode(true);
  const handleCancel = () => setIsEditMode(false);
  const handleSave = () => {
    setIsEditMode(false);
    console.log("Visibility preferences saved.");
    // Persisting changes to backend is not implemented here
  };
  const handleSelectToggle = (id) => {
    const newHiddenIds = new Set(hiddenInsightIds);
    if (newHiddenIds.has(id)) {
      newHiddenIds.delete(id);
    } else {
      newHiddenIds.add(id);
    }
    setHiddenInsightIds(newHiddenIds);
    try {
      localStorage.setItem(
        "hiddenAnalyticsIds",
        JSON.stringify(Array.from(newHiddenIds))
      );
    } catch (e) {
      console.error("Failed to save hiddenAnalyticsIds", e);
    }
  };

  // --- Render ---
  return (
    <div className="page-container">
      {/* Header */}
      <div className="space-between-full-wid header-analytics">
        <h1>Analytics</h1>
        {isEditMode ? (
          <div className="edit-buttons">
            <button className="cancel-btn btn-border" onClick={handleCancel}>
              Cancel
            </button>
            <button className="save-btn btn-blue" onClick={handleSave}>
              Save
            </button>
          </div>
        ) : (
          <p className="small-blue-btn" onClick={handleEditToggle}>
            Edit
          </p>
        )}
      </div>

      {/* Loading / Error State */}
      {isLoading && <Loader />}
      {!isLoading &&
        error &&
        !analyticsData && ( // Show error only if not loading and no data
          <p className="error-message padding-sides">{error}</p>
        )}

      {/* Content Area (only if not loading and no critical error preventing data load) */}
      {!isLoading && analyticsData && (
        <>
          {/* Activity Insights Section */}
          <div className="padding-sides">
            <div
              className="space-between-full-wid activity-header bottom-line-grey"
              onClick={() => setIsActivityExpanded((prev) => !prev)}
            >
              <p className="bold">Activity Insights</p>
              <ArrowDownIcon
                className={`toggle-arrow ${
                  isActivityExpanded ? "" : "rotated"
                }`}
              />
            </div>
          </div>
          <div
            className={`activity-content ${
              isActivityExpanded ? "expanded" : "collapsed"
            }`}
          >
            <div className="insight-grid">
              {insightsToDisplay.map((insight) => (
                <InsightCard
                  key={insight.id}
                  title={insight.title}
                  value={insight.value}
                  icon={insight.icon}
                  change={insight.change} // Absolute difference string
                  isPositive={insight.isPositive} // Sign indicator
                  isEditMode={isEditMode}
                  isActive={!hiddenInsightIds.has(insight.id)}
                  onToggle={() => handleSelectToggle(insight.id)}
                />
              ))}
            </div>
          </div>

          {/* Graph Overview Section */}
          <div className="padding-sides">
            <div
              className="space-between-full-wid activity-header bottom-line-grey"
              onClick={() => setIsGraphExpanded((prev) => !prev)}
            >
              <p className="bold">Graphical Overview</p>
              <ArrowDownIcon
                className={`toggle-arrow ${isGraphExpanded ? "" : "rotated"}`}
              />
            </div>
          </div>
          <div
            className={`activity-content ${
              isGraphExpanded ? "expanded" : "collapsed"
            }`}
          >
            <p className="space-between-full-wid">Coming soon...</p>
          </div>
        </>
      )}
      {/* Render error message even if data load failed but wasn't an auth error */}
      {!isLoading &&
        error &&
        analyticsData === null &&
        !error.toLowerCase().includes("authentication") &&
        !error.toLowerCase().includes("session") && (
          <p className="error-message padding-sides">{error}</p>
        )}
    </div>
  );
};

export default Analytics;
