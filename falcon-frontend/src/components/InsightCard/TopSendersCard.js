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
  { sender: "alice.averylongemailaddress@example.com", count: 18 },
  { sender: "bob@example.com", count: 14 },
  { sender: "carol@example.com", count: 10 },
  { sender: "dave@example.com", count: 7 },
  { sender: "eve@example.com", count: 5 },
];

const BAR_COLORS = ["#8B5CF6", "#FBB6CE", "#C026D3", "#FB3E0E", "#F0ABFC"];

function getName(str) {
  if (typeof str !== "string") return "";
  return str.split("@")[0];
}

function truncate(str, n) {
  if (typeof str !== "string") return "";
  return str.length > n ? str.slice(0, n - 1) + "…" : str;
}

const CustomBarLabel = (props) => {
  const { x, y, width, height, value } = props;
  return (
    <text
      x={x + width + 8}
      y={y + height / 2 + 4}
      fill="#374151"
      fontSize={13}
      textAnchor="start"
    >
      {value}
    </text>
  );
};

const TopSendersCard = () => {
  return (
    <div className="insight-card top-senders-card vertical-flex">
      <div className="insight-title left-align">Top Senders (Last 7 Days)</div>
      <div className="top-senders-bar-container">
        <ResponsiveContainer width="100%" height={220}>
          <BarChart
            data={DUMMY_DATA}
            layout="vertical"
            margin={{ top: 10, right: 20, left: 10, bottom: 10 }}
            barCategoryGap={18}
          >
            <XAxis type="number" hide />
            <YAxis
              dataKey="sender"
              type="category"
              width={120}
              tick={{ fontSize: 13 }}
              tickFormatter={(name) => truncate(getName(name), 18)}
            />
            <Tooltip
              formatter={(value, name, props) => [`${value} emails`, "Count"]}
              labelFormatter={(label) => `Sender: ${getName(label)}`}
            />
            <Bar
              dataKey="count"
              radius={[8, 8, 8, 8]}
              label={<CustomBarLabel />}
            >
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

export default TopSendersCard;
