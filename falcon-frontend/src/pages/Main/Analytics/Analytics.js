import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { ReactComponent as ArrowDownIcon } from "../../../assets/icons/black/arrow-down.svg";
import "./Analytics.css";

// --- Import Components & Constants ---
import InsightCard from "../../../components/InsightCard/InsightCard";
import Loader from "../../../components/Loader/Loader";
import { API_BASE_URL } from "../../../config/constants";

// --- Import AVAILABLE Icons ---
import { ReactComponent as ClockIcon } from "../../../assets/icons/black/clock-icon.svg";
import { ReactComponent as SentIcon } from "../../../assets/icons/black/mail-sent.svg";
import { ReactComponent as ReceivedIcon } from "../../../assets/icons/black/mail-received.svg";
import { ReactComponent as SpamIcon } from "../../../assets/icons/black/spam.svg";
import { ReactComponent as TrashIcon } from "../../../assets/icons/black/trash-black.svg";
import { ReactComponent as ReadEmailIcon } from "../../../assets/icons/black/glasses.svg";
import { ReactComponent as StreakIcon } from "../../../assets/icons/black/streak.svg";

// --- Helper function to calculate percentage change ---
const calculatePercentageChange = (current, previous) => {
  if (previous === null || previous === undefined)
    return { change: "N/A", isPositive: null };
  if (previous === 0) {
    return {
      change: current > 0 ? "+100%" : "0%",
      isPositive: current > 0 ? true : null,
    };
  }
  const changeValue = ((current - previous) / previous) * 100;
  const isPositive = changeValue > 0;
  const changeString = `${changeValue >= 0 ? "+" : ""}${changeValue.toFixed(
    1
  )}%`;
  return {
    change: changeString,
    isPositive: changeValue === 0 ? null : isPositive,
  };
};

// --- Helper function to format minutes into hours/minutes string ---
const formatMinutes = (totalMinutes) => {
  if (totalMinutes === null || totalMinutes === undefined || totalMinutes < 0)
    return "0m";
  const hours = Math.floor(totalMinutes / 60);
  const minutes = Math.floor(totalMinutes % 60);
  let result = "";
  if (hours > 0) {
    result += `${hours}h `;
  }
  result += `${minutes}m`;
  return result.trim() || "0m";
};

const Analytics = () => {
  // Edit mode state (functionality needs review)
  const [isEditMode, setIsEditMode] = useState(false);

  // Accordion expansion state
  const [isActivityExpanded, setIsActivityExpanded] = useState(true);
  const [isGraphExpanded, setIsGraphExpanded] = useState(true);

  // State for loading, fetched data, and errors
  const [isLoading, setIsLoading] = useState(true);
  const [analyticsData, setAnalyticsData] = useState(null);
  const [error, setError] = useState(null);
  const navigate = useNavigate();

  // --- State for user's visibility preferences, loaded from localStorage ---
  const [hiddenInsightIds, setHiddenInsightIds] = useState(() => {
    const savedHiddenIds = localStorage.getItem("hiddenAnalyticsIds");
    try {
      return savedHiddenIds ? new Set(JSON.parse(savedHiddenIds)) : new Set();
    } catch (e) {
      console.error("Failed to parse hiddenAnalyticsIds from localStorage", e);
      return new Set(); // Fallback to empty set on parse error
    }
  });

  // Fetch data on component mount
  useEffect(() => {
    const fetchAnalytics = async () => {
      setIsLoading(true);
      setError(null);
      const token = localStorage.getItem("token");

      if (!token) {
        setError("Authentication token not found. Please log in.");
        setIsLoading(false);
        return;
      }

      try {
        const response = await fetch(`${API_BASE_URL}/api/analytics`, {
          method: "GET",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`,
          },
        });

        if (!response.ok) {
          if (response.status === 401) {
            localStorage.removeItem("token");
            localStorage.removeItem("isAuthenticated");
            setError("Session expired or invalid. Please log in again.");
            navigate("/login");
            return;
          }
          const errorText = await response.text();
          throw new Error(
            `Failed to fetch analytics (${response.status}): ${errorText}`
          );
        }

        const data = await response.json();
        setAnalyticsData(data);
      } catch (err) {
        console.error("Error fetching analytics:", err);
        setError(
          err.message || "An error occurred while fetching analytics data."
        );
      } finally {
        setIsLoading(false);
      }
    };

    fetchAnalytics();
  }, [navigate]);

  // Prepare FULL insight data array based on fetched analyticsData
  const allAvailableInsights = analyticsData
    ? [
        {
          id: "timeToday",
          title: "Time Spent Today",
          value: formatMinutes(analyticsData.timeSpentToday),
          icon: ClockIcon,
          ...calculatePercentageChange(
            analyticsData.timeSpentToday,
            analyticsData.timeSpentYesterday
          ),
        },
        {
          id: "timeWeekly",
          title: "Time Spent This Week",
          value: formatMinutes(analyticsData.timeSpentThisWeek),
          icon: ClockIcon,
          ...calculatePercentageChange(
            analyticsData.timeSpentThisWeek,
            analyticsData.timeSpentLastWeek
          ),
        },
        {
          id: "emailsSentWeekly",
          title: "Emails Sent Weekly",
          value: analyticsData.emailsSentWeekly?.toString() ?? "0",
          icon: SentIcon,
          ...calculatePercentageChange(
            analyticsData.emailsSentWeekly,
            analyticsData.emailsSentLastWeek
          ),
        },
        {
          id: "emailsReceivedWeekly",
          title: "Emails Received Weekly",
          value: analyticsData.emailsReceivedWeekly?.toString() ?? "0",
          icon: ReceivedIcon,
          ...calculatePercentageChange(
            analyticsData.emailsReceivedWeekly,
            analyticsData.emailsReceivedLastWeek
          ),
        },
        {
          id: "emailsReadWeekly",
          title: "Emails Read Weekly",
          value: analyticsData.readEmailsWeekly?.toString() ?? "0",
          icon: ReadEmailIcon,
          ...calculatePercentageChange(
            analyticsData.readEmailsWeekly,
            analyticsData.readEmailsLastWeek
          ),
        },
        {
          id: "spamEmailsWeekly",
          title: "Spam Emails Weekly",
          value: analyticsData.spamEmailsWeekly?.toString() ?? "0",
          icon: SpamIcon,
          ...calculatePercentageChange(
            analyticsData.spamEmailsWeekly,
            analyticsData.spamEmailsLastWeek
          ),
          isPositive:
            calculatePercentageChange(
              analyticsData.spamEmailsWeekly,
              analyticsData.spamEmailsLastWeek
            ).change !== "N/A"
              ? analyticsData.spamEmailsWeekly <
                analyticsData.spamEmailsLastWeek
              : null,
        },
        {
          id: "deletedEmailsWeekly",
          title: "Deleted Emails Weekly",
          value: analyticsData.deletedEmailsWeekly?.toString() ?? "0",
          icon: TrashIcon,
          ...calculatePercentageChange(
            analyticsData.deletedEmailsWeekly,
            analyticsData.deletedEmailsLastWeek
          ),
        }, // Shows placeholder "0" / "N/A"
        {
          id: "currentStreak",
          title: "Current Daily Streak",
          value: `${analyticsData.currentStreak ?? 0} ${
            analyticsData.currentStreak === 1 ? "day" : "days"
          }`,
          icon: StreakIcon,
          change: `Longest: ${analyticsData.longestStreak ?? 0}`,
          isPositive: null,
        },
      ]
    : [];

  const insightsToDisplay = isEditMode
    ? allAvailableInsights
    : allAvailableInsights.filter(
        (insight) => !hiddenInsightIds.has(insight.id)
      );
  // --- Edit Mode Handlers ---
  const handleEditToggle = () => {
    setIsEditMode(true);
  };

  const handleCancel = () => {
    setIsEditMode(false);
  };

  const handleSave = () => {
    setIsEditMode(false);
    console.log("Visibility preferences saved.");
  };

  // Toggle Visibility Handler (updates state and localStorage)
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
      console.error("Failed to save hiddenAnalyticsIds to localStorage", e);
    }
  };

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

      {/* Display Loading or Error */}
      {isLoading && <Loader />}
      {!isLoading && error && (
        <p className="error-message padding-sides">{error}</p>
      )}

      {/* Activity Insights Section */}
      {!isLoading && !error && analyticsData && (
        <>
          {/* Activity Insights Header */}
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

          {/* Activity Content */}
          <div
            className={`activity-content ${
              isActivityExpanded ? "expanded" : "collapsed"
            }`}
          >
            <div className="insight-grid">
              {/* Map over the correct array based on edit mode */}
              {(isEditMode ? allAvailableInsights : insightsToDisplay).map(
                (insight) => (
                  <InsightCard
                    key={insight.id}
                    title={insight.title}
                    value={insight.value}
                    icon={insight.icon}
                    change={insight.change}
                    isPositive={insight.isPositive}
                    isEditMode={isEditMode}
                    isActive={!hiddenInsightIds.has(insight.id)}
                    onToggle={() => handleSelectToggle(insight.id)}
                  />
                )
              )}
            </div>
          </div>
        </>
      )}

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
    </div>
  );
};

export default Analytics;
