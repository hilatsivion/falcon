import React from "react";
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
  Cell,
} from "recharts";
import "./InsightCard.css";

const DUMMY_DATA = [
  { range: "00–06", avg: 2 },
  { range: "06–09", avg: 5 },
  { range: "09–12", avg: 12 },
  { range: "12–15", avg: 8 },
  { range: "15–18", avg: 6 },
  { range: "18–24", avg: 3 },
];

const BAR_COLORS = [
  "#8B5CF6",
  "#FBB6CE",
  "#C026D3",
  "#FB3E0E",
  "#F0ABFC",
  "#60A5FA",
];

const EmailsByTimeOfDayCard = () => {
  return (
    <div className="insight-card emails-by-time-card vertical-flex">
      <div className="insight-title left-align">
        Emails Received by Time of Day (Weekly Average)
      </div>
      <div className="emails-by-time-bar-container">
        <ResponsiveContainer width="100%" height={180}>
          <BarChart
            data={DUMMY_DATA}
            margin={{ top: 10, right: 10, left: 10, bottom: 10 }}
          >
            <XAxis
              dataKey="range"
              axisLine={false}
              tickLine={false}
              tick={{ fontSize: 13 }}
            />
            <YAxis hide />
            <Tooltip
              cursor={{ fill: "#f3f3f3" }}
              formatter={(value) => [`${value} emails`, "Avg"]}
            />
            <Bar dataKey="avg" radius={[8, 8, 0, 0]}>
              {DUMMY_DATA.map((entry, index) => (
                <Cell
                  key={`cell-${index}`}
                  fill={BAR_COLORS[index % BAR_COLORS.length]}
                />
              ))}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
};

export default EmailsByTimeOfDayCard;
