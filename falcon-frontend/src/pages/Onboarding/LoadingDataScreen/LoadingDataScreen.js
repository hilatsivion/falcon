import React, { useState, useEffect } from "react"; // Import hooks
import { useNavigate } from "react-router-dom"; // Import navigate
import { useAuth } from "../../../context/AuthContext"; // Import Auth context hook
import { API_BASE_URL } from "../../../config/constants"; // Import API Base URL
import { toast } from "react-toastify"; // For error messages

import "./loadingData.css";
import "../../../styles/global.css";

import logo from "../../../assets/images/falcon-logo-full-blue.svg";
import spinnerIcon from "../../../assets/icons/black/spinner.svg";

const LoadingDataPage = () => {
  const [isDataLoadingComplete, setIsDataLoadingComplete] = useState(false); // Tracks API call completion
  const [isMinTimeElapsed, setIsMinTimeElapsed] = useState(false); // Tracks 3-second timer
  const { authToken } = useAuth(); // Get token from context
  const navigate = useNavigate();

  // Effect 1: Minimum Display Time (3 seconds)
  useEffect(() => {
    console.log("LoadingDataPage: Starting min time timer...");
    const timer = setTimeout(() => {
      console.log("LoadingDataPage: Minimum time elapsed.");
      setIsMinTimeElapsed(true);
    }, 3000); // 3 seconds

    return () => clearTimeout(timer); // Cleanup timer on unmount
  }, []); // Run only once on mount

  // Effect 2: Trigger Data Initialization API Call
  useEffect(() => {
    const initializeData = async () => {
      if (!authToken) {
        console.error(
          "LoadingDataPage: No auth token found! Redirecting to login."
        );
        toast.error("Authentication error. Please log in.");
        navigate("/login", { replace: true }); // Redirect if no token
        return;
      }

      console.log("LoadingDataPage: Starting data initialization API call...");
      // We assume loading starts true conceptually until the API call finishes
      setIsDataLoadingComplete(false); // Explicitly set loading incomplete

      try {
        const response = await fetch(
          `${API_BASE_URL}/api/user/initialize-account`,
          {
            method: "POST",
            headers: {
              Authorization: `Bearer ${authToken}`,
              // No 'Content-Type' needed for POST without body
            },
          }
        );

        if (!response.ok) {
          let errorMsg = "Failed to initialize account data";
          try {
            const errData = await response.json();
            errorMsg = errData.message || errorMsg;
          } catch (e) {
            /* ignore */
          }
          throw new Error(errorMsg);
        }

        console.log(
          "LoadingDataPage: Data initialization API call successful."
        );
        // Mark data loading as complete
        setIsDataLoadingComplete(true);
      } catch (error) {
        console.error("LoadingDataPage: Data initialization failed:", error);
        toast.error(
          `Failed to load initial data: ${error.message}. Proceeding anyway...`
        );
        // Even if initialization fails, we might want to proceed to inbox
        setIsDataLoadingComplete(true); // Mark as complete even on error to allow navigation
      }
    };

    initializeData();
  }, [authToken, navigate]); // Re-run if token changes (e.g., on quick re-render)

  // Effect 3: Navigate when both conditions are met
  useEffect(() => {
    console.log(
      `LoadingDataPage: Checking navigation conditions - MinTime: ${isMinTimeElapsed}, DataLoaded: ${isDataLoadingComplete}`
    );
    if (isMinTimeElapsed && isDataLoadingComplete) {
      console.log("LoadingDataPage: Conditions met, navigating to /inbox...");
      navigate("/inbox", { replace: true }); // Navigate to inbox and replace history
    }
  }, [isMinTimeElapsed, isDataLoadingComplete, navigate]); // Depend on both flags

  // Render the loading UI
  return (
    <div className="loading-container">
      <img className="loading-logo" src={logo} alt="Falcon Logo" />
      <img className="loading-spinner" src={spinnerIcon} alt="Loading..." />
      <p className="loading-text">
        <span>Fetching all your Data</span>
      </p>
      <p className="loading-subtext">
        This might take a few minutes, don’t close the app
      </p>
    </div>
  );
};

export default LoadingDataPage;
