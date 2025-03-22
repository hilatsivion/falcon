import React, { useState } from "react";
import { ReactComponent as ArrowDownIcon } from "../../../assets/icons/black/arrow-down.svg";
import "./Analytics.css";

const Analytics = () => {
  const [isExpanded, setIsExpanded] = useState(true);

  const toggleSection = () => {
    setIsExpanded((prev) => !prev);
  };

  return (
    <div className="page-container">
      {/* Header */}
      <div className="space-between-full-wid">
        <h1>Analytics</h1>
        <p className="small-blue-btn">Edit</p>
      </div>

      {/* Section Header with Toggle */}
      <div
        className="space-between-full-wid activity-header"
        onClick={toggleSection}
      >
        <p className="bold">Activity Insights</p>
        <ArrowDownIcon
          className={`toggle-arrow ${isExpanded ? "rotated" : ""}`}
        />
      </div>

      {/* Collapsible Section */}
      <div
        className={`activity-content ${isExpanded ? "expanded" : "collapsed"}`}
      >
        {/* Here you'll later insert the metrics cards (Time Spent, Emails Sent, etc.) */}
        <p style={{ padding: "20px" }}>📊 Insight components will go here...</p>
      </div>
    </div>
  );
};

export default Analytics;
