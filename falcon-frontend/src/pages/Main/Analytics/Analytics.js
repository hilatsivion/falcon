import React, { useState } from "react";
import { ReactComponent as ArrowDownIcon } from "../../../assets/icons/black/arrow-down.svg";
import "./Analytics.css";

import InsightCard from "../../../components/InsightCard/InsightCard";
import { ReactComponent as ClockIcon } from "../../../assets/icons/black/clock-icon.svg";
import { ReactComponent as SentIcon } from "../../../assets/icons/black/mail-sent.svg";
import { ReactComponent as ReceivedIcon } from "../../../assets/icons/black/mail-received.svg";
import { ReactComponent as SpamIcon } from "../../../assets/icons/black/spam.svg";
import { ReactComponent as TrashIcon } from "../../../assets/icons/black/trash-black.svg";

const initialInsights = [
  {
    id: "1",
    title: "Time Spent Today",
    value: "2h 39m",
    icon: ClockIcon,
    change: "-0.2%",
    isPositive: false,
    isActive: true,
  },
  {
    id: "2",
    title: "Avg Weekly Usage",
    value: "4h 5m",
    icon: ClockIcon,
    change: "+0.6%",
    isPositive: true,
    isActive: true,
  },
  {
    id: "3",
    title: "Emails Sent Weekly",
    value: "5",
    icon: SentIcon,
    change: "+0.6%",
    isPositive: true,
    isActive: true,
  },
  {
    id: "4",
    title: "Emails Received Weekly",
    value: "10",
    icon: ReceivedIcon,
    change: "-0.2%",
    isPositive: false,
    isActive: true,
  },
  {
    id: "5",
    title: "Spam Emails Weekly",
    value: "17",
    icon: SpamIcon,
    change: "0%",
    isPositive: null,
    isActive: true,
  },
  {
    id: "6",
    title: "Deleted Emails Weekly",
    value: "3",
    icon: TrashIcon,
    change: "2.5%",
    isPositive: false,
    isActive: false,
  },
];

const Analytics = () => {
  const [isEditMode, setIsEditMode] = useState(false);
  const [allInsights, setAllInsights] = useState(initialInsights);
  const [draftInsights, setDraftInsights] = useState([]);

  const [isActivityExpanded, setIsActivityExpanded] = useState(true);
  const [isGraphExpanded, setIsGraphExpanded] = useState(true);

  const handleEditToggle = () => {
    // Clone the current state for editing
    setDraftInsights([...allInsights]);
    setIsEditMode(true);
  };

  const handleCancel = () => {
    setIsEditMode(false);
    setDraftInsights([]); // Clean draft
  };

  const handleSave = () => {
    setAllInsights([...draftInsights]);
    setIsEditMode(false);
    setDraftInsights([]);
  };

  const handleSelectToggle = (id) => {
    const updated = draftInsights.map((item) =>
      item.id === id ? { ...item, isActive: !item.isActive } : item
    );
    setDraftInsights(updated);
  };

  const insightsToDisplay = isEditMode
    ? draftInsights
    : allInsights.filter((item) => item.isActive);

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

      {/* Activity Insights Header */}
      <div className="padding-sides">
        <div
          className="space-between-full-wid activity-header bottom-line-grey"
          onClick={() => setIsActivityExpanded((prev) => !prev)}
        >
          <p className="bold">Activity Insights</p>
          <ArrowDownIcon
            className={`toggle-arrow ${isActivityExpanded ? "" : "rotated"}`}
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
          {insightsToDisplay.map((insight) => (
            <InsightCard
              key={insight.id}
              {...insight}
              isEditMode={isEditMode}
              onToggle={() => handleSelectToggle(insight.id)}
            />
          ))}
        </div>
      </div>

      {/* Graph Overview Header */}
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
