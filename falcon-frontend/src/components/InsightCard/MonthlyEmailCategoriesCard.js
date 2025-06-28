import React from "react";
import { PieChart, Pie, Cell, ResponsiveContainer } from "recharts";
import "./InsightCard.css";

const DUMMY_DATA = [
  { name: "Work", value: 10.33, color: "#8B5CF6" },
  { name: "School", value: 4.19, color: "#FBB6CE" },
  { name: "Finance", value: 25.33, color: "#C026D3" },
  { name: "Marketing", value: 10.33, color: "#FB3E0E" },
  { name: "Design", value: 4.19, color: "#F0ABFC" },
];

const MonthlyEmailCategoriesCard = () => {
  return (
    <div className="insight-card monthly-categories-card vertical-flex">
      <div className="insight-title left-align">Monthly Email Categories</div>
      <div className="monthly-categories-center-content">
        <ResponsiveContainer width={200} height={200}>
          <PieChart>
            <Pie
              data={DUMMY_DATA}
              dataKey="value"
              nameKey="name"
              cx="50%"
              cy="50%"
              innerRadius={60}
              outerRadius={90}
              paddingAngle={2}
              label={false}
            >
              {DUMMY_DATA.map((entry, index) => (
                <Cell key={`cell-${index}`} fill={entry.color} />
              ))}
            </Pie>
          </PieChart>
        </ResponsiveContainer>
        <ul className="monthly-categories-legend vertical-legend">
          {DUMMY_DATA.map((entry, index) => (
            <li key={`item-${index}`}>
              <span
                className="legend-dot"
                style={{ background: entry.color }}
              />
              <span className="legend-label">{entry.name}</span>
              <span className="legend-value">{entry.value.toFixed(1)}%</span>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
};

export default MonthlyEmailCategoriesCard;
