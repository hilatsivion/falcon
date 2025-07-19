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

const BAR_COLORS = [
  "#8B5CF6",
  "#FBB6CE",
  "#C026D3",
  "#FB3E0E",
  "#F0ABFC",
  "#60A5FA",
];

const EmailsByTimeOfDayCard = () => {
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
          `${API_BASE_URL}/api/analytics/emails-by-time-of-day`,
          {
            headers: { Authorization: `Bearer ${authToken}` },
          }
        );
        if (!response.ok)
          throw new Error("Failed to fetch emails by time of day");
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
      <div className="insight-card emails-by-time-card vertical-flex">
        <Loader />
      </div>
    );
  if (error)
    return (
      <div className="insight-card emails-by-time-card vertical-flex">
        <p className="error-message">{error}</p>
      </div>
    );
  if (!data || data.length === 0)
    return (
      <div className="insight-card emails-by-time-card vertical-flex">
        <p>No data available</p>
      </div>
    );

  return (
    <div className="insight-card emails-by-time-card vertical-flex">
      <div className="insight-title left-align">
        Emails Received by Time of Day (Weekly Average)
      </div>
      <div className="emails-by-time-bar-container">
        <ResponsiveContainer width="100%" height={180}>
          <BarChart
            data={data}
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
            <Bar dataKey="average" radius={[8, 8, 0, 0]}>
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

export default EmailsByTimeOfDayCard;
