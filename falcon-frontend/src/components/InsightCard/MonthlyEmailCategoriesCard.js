import React, { useEffect, useState } from "react";
import { PieChart, Pie, Cell, ResponsiveContainer } from "recharts";
import "./InsightCard.css";
import { API_BASE_URL } from "../../config/constants";
import { useAuth } from "../../context/AuthContext";
import Loader from "../Loader/Loader";

const COLORS = [
  "#8B5CF6",
  "#FBB6CE",
  "#C026D3",
  "#FB3E0E",
  "#F0ABFC",
  "#60A5FA",
  "#FFD6E0",
  "#D1F0FF",
  "#FFF6B2",
];

const MonthlyEmailCategoriesCard = () => {
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
          `${API_BASE_URL}/api/analytics/email-category-breakdown`,
          {
            headers: { Authorization: `Bearer ${authToken}` },
          }
        );
        if (!response.ok) throw new Error("Failed to fetch category breakdown");
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
      <div className="insight-card monthly-categories-card vertical-flex">
        <Loader />
      </div>
    );
  if (error)
    return (
      <div className="insight-card monthly-categories-card vertical-flex">
        <p className="error-message">{error}</p>
      </div>
    );
  if (!data || data.length === 0)
    return (
      <div className="insight-card monthly-categories-card vertical-flex">
        <p>No data available</p>
      </div>
    );

  return (
    <div className="insight-card monthly-categories-card vertical-flex">
      <div className="insight-title left-align">Monthly Email Categories</div>
      <div className="monthly-categories-center-content">
        <ResponsiveContainer width={200} height={200}>
          <PieChart>
            <Pie
              data={data}
              dataKey="percentage"
              nameKey="category"
              cx="50%"
              cy="50%"
              innerRadius={60}
              outerRadius={90}
              paddingAngle={2}
              label={false}
            >
              {data.map((entry, index) => (
                <Cell
                  key={`cell-${index}`}
                  fill={COLORS[index % COLORS.length]}
                />
              ))}
            </Pie>
          </PieChart>
        </ResponsiveContainer>
        <ul className="monthly-categories-legend vertical-legend">
          {data.map((entry, index) => (
            <li key={`item-${index}`}>
              <span
                className="legend-dot"
                style={{ background: COLORS[index % COLORS.length] }}
              />
              <span className="legend-label">{entry.category}</span>
              <span className="legend-value">
                {entry.percentage.toFixed(1)}%
              </span>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
};

export default MonthlyEmailCategoriesCard;
