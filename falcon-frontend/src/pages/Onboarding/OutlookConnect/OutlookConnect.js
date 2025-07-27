import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  API_BASE_URL,
  OAUTH_SCOPES,
  REDIRECT_URI,
} from "../../../config/constants";
import "../Connect/connect.css";
import "./OutlookConnect.css";
import { getAuthToken } from "../../../utils/auth";

const OutlookConnectPage = () => {
  const [status, setStatus] = useState("");
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    const urlParams = new URLSearchParams(window.location.search);
    const code = urlParams.get("code");
    const error = urlParams.get("error");

    const token = getAuthToken();
    if (!token) return;

    if (error) {
      setStatus(`❌ OAuth failed: ${error}`);
      return;
    }

    if (code) {
      handleOAuthCallback(code, token);
    }
  }, []);

  const handleOAuthCallback = async (authCode, token) => {
    setLoading(true);
    setStatus("Exchanging authorization code for tokens...");

    try {
      const response = await fetch(`${API_BASE_URL}/api/oauth/exchange-token`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({
          code: authCode,
          state: localStorage.getItem("oauthState") || "",
          redirectUri: REDIRECT_URI,
        }),
      });

      const result = await response.json();
      if (response.ok && result.success) {
        setStatus("✅ Successfully connected to Outlook!");
        localStorage.removeItem("oauthState");
        window.history.replaceState(
          {},
          document.title,
          window.location.pathname
        );
        // Check user profile for Outlook connection flag
        try {
          const profileRes = await fetch(`${API_BASE_URL}/api/auth/profile`, {
            headers: { Authorization: `Bearer ${token}` },
          });
          const profile = await profileRes.json();
          // Adjust the flag name as needed (hasOutlookConnection or isOutlookConnected)
          if (profile.hasOutlookConnection || profile.isOutlookConnected) {
            navigate("/");
          }
        } catch (profileErr) {
          // If profile check fails, still allow navigation
          navigate("/");
        }
      } else {
        throw new Error(result.message || "Token exchange failed");
      }
    } catch (err) {
      setStatus(`❌ OAuth completion failed: ${err.message}`);
    } finally {
      setLoading(false);
    }
  };

  const connectToOutlook = async () => {
    const token = getAuthToken();
    if (!token) {
      setStatus("Please log in first");
      return;
    }

    setLoading(true);
    setStatus("Generating OAuth2 authorization URL...");

    try {
      const response = await fetch(`${API_BASE_URL}/api/oauth/authorize-url`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({
          redirectUri: REDIRECT_URI,
          scope: OAUTH_SCOPES.join(" "),
        }),
      });

      const data = await response.json();

      if (data.authorizationUrl) {
        if (data.state) {
          localStorage.setItem("oauthState", data.state);
        }
        window.location.href = data.authorizationUrl;
      } else {
        throw new Error("Failed to retrieve authorization URL");
      }
    } catch (err) {
      setStatus(`❌ OAuth setup failed: ${err.message}`);
      setLoading(false);
    }
  };

  return (
    <div className="welcome-container">
      <div>
        <h2>Connect Your Outlook Account</h2>
        <p className="sub-title">
          Link your Outlook account to sync your emails securely with Falcon.
        </p>
        <button onClick={connectToOutlook} className="btn-outlook">
          Connect to Outlook
        </button>
        {status && (
          <div className="status-message info" style={{ marginTop: "15px" }}>
            {status}
          </div>
        )}
      </div>

      {loading && (
        <div className="loading-overlay">
          <div className="spinner"></div>
          <p>Connecting to Outlook...</p>
        </div>
      )}
    </div>
  );
};

export default OutlookConnectPage;
