import React, { useEffect, useState } from "react";
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
import { API_BASE_URL } from "../../config/constants";
import { useAuth } from "../../context/AuthContext";
import Loader from "../Loader/Loader";

const BAR_COLORS = ["#8B5CF6", "#FBB6CE", "#C026D3", "#FB3E0E", "#F0ABFC"];

function getName(str) {
  if (typeof str !== "string") return "";
  return str.split("@")[0];
}

function truncate(str, n) {
  if (typeof str !== "string") return "";
  return str.length > n ? str.slice(0, n - 1) + " 026" : str;
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
  const { authToken } = useAuth();
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      setError(null);
      try {
        const response = await fetch(
          `${API_BASE_URL}/api/analytics/top-senders`,
          {
            headers: { Authorization: `Bearer ${authToken}` },
          }
        );
        if (!response.ok) throw new Error("Failed to fetch top senders");
        const result = await response.json();
        setData(result);
      } catch (err) {
        setError(err.message);
        setData([]);
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, [authToken]);

  if (loading)
    return (
      <div className="insight-card top-senders-card vertical-flex">
        <Loader />
      </div>
    );
  if (error)
    return (
      <div className="insight-card top-senders-card vertical-flex">
        <p className="error-message">{error}</p>
      </div>
    );
  if (!data || data.length === 0)
    return (
      <div className="insight-card top-senders-card vertical-flex">
        <p>No data available</p>
      </div>
    );

  return (
    <div className="insight-card top-senders-card vertical-flex">
      <div className="insight-title left-align">Top Senders (Last 7 Days)</div>
      <div className="top-senders-bar-container">
        <ResponsiveContainer width="100%" height={220}>
          <BarChart
            data={data}
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
              {data.map((entry, index) => (
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
