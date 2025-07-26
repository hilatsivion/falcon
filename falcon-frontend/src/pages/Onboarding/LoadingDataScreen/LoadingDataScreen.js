import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../../context/AuthContext";
import { API_BASE_URL } from "../../../config/constants";
import { toast } from "react-toastify";

import "./loadingData.css";
import "../../../styles/global.css";

import { ReactComponent as Logo } from "../../../assets/images/falcon-logo-full-blue.svg";
import { ReactComponent as SpinnerIcon } from "../../../assets/icons/black/spinner.svg";

const LoadingDataPage = () => {
  const [isDataLoadingComplete, setIsDataLoadingComplete] = useState(false);
  const [isMinTimeElapsed, setIsMinTimeElapsed] = useState(false);
  const { authToken } = useAuth();
  const navigate = useNavigate();

  // Minimum Display Time (3 seconds)
  useEffect(() => {
    const timer = setTimeout(() => {
      setIsMinTimeElapsed(true);
    }, 30000); // 30 seconds

    return () => clearTimeout(timer);
  }, []);

  // Trigger Data Initialization API Call
  useEffect(() => {
    const initializeData = async () => {
      if (!authToken) {
        toast.error("Authentication error. Please log in.");
        navigate("/login", { replace: true });
        return;
      }
      setIsDataLoadingComplete(false);

      try {
        const response = await fetch(
          `${API_BASE_URL}/api/user/initialize-account`,
          {
            method: "POST",
            headers: {
              Authorization: `Bearer ${authToken}`,
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
        // navigate to inbox anyway
        setIsDataLoadingComplete(true); // Mark as complete even on error to allow navigation
      }
    };

    initializeData();
  }, [authToken, navigate]); // Re-run if token changes

  // Navigate when both conditions are met
  useEffect(() => {
    console.log(
      `LoadingDataPage: Checking navigation conditions - MinTime: ${isMinTimeElapsed}, DataLoaded: ${isDataLoadingComplete}`
    );
    if (isMinTimeElapsed && isDataLoadingComplete) {
      navigate("/inbox", { replace: true }); // Navigate to inbox and replace history
    }
  }, [isMinTimeElapsed, isDataLoadingComplete, navigate]); // Depend on both flags

  return (
    <div className="loading-container">
      <Logo className="loading-logo" />
      <SpinnerIcon className="loading-spinner" />
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
