import React, { useEffect, useRef, useCallback } from "react";
import TopNav from "../components/TopNav/TopNav";
import Navbar from "../components/Navbar/Navbar";
import { useAuth } from "../context/AuthContext";
import { API_BASE_URL } from "../config/constants";

const HEARTBEAT_INTERVAL_MS = 60 * 1000; // 1 minute

const MainLayout = ({ children }) => {
  const { isAuthenticated, authToken, isValidating } = useAuth();
  const intervalRef = useRef(null);

  const sendHeartbeat = useCallback(async () => {
    if (!authToken) {
      return;
    }
    try {
      console.log("Sending heartbeat...");

      const response = await fetch(`${API_BASE_URL}/api/analytics/heartbeat`, {
        method: "POST",
        headers: {
          Authorization: `Bearer ${authToken}`,
        },
      });

      if (!response.ok) {
        console.error(`Heartbeat failed: ${response.status}`);
        if (response.status === 401) {
          console.error("Heartbeat received 401, token might be invalid.");
        }
      }
    } catch (error) {
      console.error("Error sending heartbeat:", error);
    }
  }, [authToken]);

  useEffect(() => {
    // CHANGE: Condition now includes !isValidating
    if (isAuthenticated && !isValidating) {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
      }
      console.log("Starting heartbeat interval");

      sendHeartbeat();
      intervalRef.current = setInterval(sendHeartbeat, HEARTBEAT_INTERVAL_MS);
    } else {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
        intervalRef.current = null;
      }
    }

    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
      }
    };
    // CHANGE: Dependencies updated
  }, [isAuthenticated, isValidating, sendHeartbeat]);

  return (
    <div>
      <TopNav />
      {children}
      <Navbar />
    </div>
  );
};

export default MainLayout;
