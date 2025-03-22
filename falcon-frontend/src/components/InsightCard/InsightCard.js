import React from "react";
import "./InsightCard.css";
import { ReactComponent as CheckboxEmpty } from "../../assets/icons/blue/checkbox-empty.svg";
import { ReactComponent as CheckboxChecked } from "../../assets/icons/blue/checkbox-checked.svg";

const InsightCard = ({
  title,
  value,
  icon: Icon,
  change,
  isPositive,
  isActive,
  isEditMode,
  onToggle,
}) => {
  return (
    <div
      className={`insight-card ${isEditMode ? "edit-mode" : ""} ${
        isEditMode && !isActive ? "dimmed" : ""
      }`}
    >
      <p className="insight-title">{title}</p>

      <div className="space-between-full-wid no-padding">
        <p className="insight-value">{value}</p>
        <Icon className="insight-icon" />
      </div>

      <div className="space-between-full-wid no-padding">
        <p
          className={`insight-change ${
            isPositive === null
              ? "neutral"
              : isPositive
              ? "positive"
              : "negative"
          }`}
        >
          {change}
        </p>

        {isEditMode && (
          <div className="select-toggle" onClick={onToggle}>
            {isActive ? (
              <CheckboxChecked className="checkbox-icon" />
            ) : (
              <CheckboxEmpty className="checkbox-icon" />
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export default InsightCard;
