import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { toast } from "react-toastify";
import { useAuth } from "../../context/AuthContext";
import { API_BASE_URL } from "../../config/constants";
import Loader from "../Loader/Loader";
import "./AdvancedSearch.css";

const SEARCH_HISTORY_KEY = "falconSearchHistory";
const MAX_HISTORY_ITEMS = 5;

const AdvancedSearch = () => {
  const [keyword, setKeyword] = useState("");
  const [sender, setSender] = useState("");
  const [receiver, setReceiver] = useState("");
  const [lastSearches, setLastSearches] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const { authToken } = useAuth();
  const navigate = useNavigate();
  const trimmedKeyword = keyword.trim();

  // Load search history from localStorage on component mount
  useEffect(() => {
    try {
      const storedHistory = localStorage.getItem(SEARCH_HISTORY_KEY);
      if (storedHistory) {
        setLastSearches(JSON.parse(storedHistory));
      } else {
        setLastSearches([]);
      }
    } catch (error) {
      console.error("Failed to load search history:", error);
      setLastSearches([]);
    }
  }, []);

  const saveSearchHistory = (newHistory) => {
    try {
      localStorage.setItem(SEARCH_HISTORY_KEY, JSON.stringify(newHistory));
    } catch (error) {
      console.error("Failed to save search history:", error);
    }
  };

  // Disable search button if all inputs are empty or loading is in progress
  const isSearchDisabled =
    (!keyword.trim() && !sender.trim() && !receiver.trim()) || isLoading;

  // Ensure at least one search criterion is provided before sending the request
  const handleSearch = async (e) => {
    if (e) e.preventDefault();

    const isActuallyEmpty =
      !keyword.trim() && !sender.trim() && !receiver.trim();
    if (isActuallyEmpty && !isLoading) {
      toast.error("Please enter at least one search criterion.");
      return;
    }

    if (!authToken) {
      toast.error("Authentication error. Please log in again.");
      return;
    }

    setIsLoading(true);

    const PLACEHOLDER = "-";
    const requestBody = {
      Keywords: keyword.trim() || PLACEHOLDER,
      Sender: sender.trim() || PLACEHOLDER,
      Recipient: receiver.trim() || PLACEHOLDER,
    };

    const searchUrl = `${API_BASE_URL}/api/mail/search`;
    try {
      const response = await fetch(searchUrl, {
        method: "POST",
        headers: {
          Authorization: `Bearer ${authToken}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify(requestBody),
      });

      if (!response.ok) {
        const errData = await response.json().catch(() => null);
        const errorMsg =
          errData?.message || `Search failed (${response.status})`;
        throw new Error(errorMsg);
      }
      const results = await response.json();

      // Add current keyword to history if it's not already first in the list
      if (
        trimmedKeyword &&
        (!lastSearches.length || lastSearches[0] !== trimmedKeyword)
      ) {
        const updatedHistory = [
          trimmedKeyword,
          ...lastSearches.filter((item) => item !== trimmedKeyword),
        ].slice(0, MAX_HISTORY_ITEMS);
        setLastSearches(updatedHistory);
        saveSearchHistory(updatedHistory);
      }
      navigate("/search-results", {
        state: { results, query: { keyword, sender, receiver } },
      });
    } catch (error) {
      console.error("Search API error:", error);
      toast.error(`Search failed: ${error.message}`);
    } finally {
      setIsLoading(false);
    }
  };

  const handleClearLastSearches = () => {
    setLastSearches([]);
    saveSearchHistory([]);
  };

  const reuseSearchTerm = (term) => {
    setKeyword(term);
  };

  return (
    <div className="page-container">
      {isLoading && <Loader />}
      <div className="advanced-search-container space-between-full-wid padding-sides">
        <div className="width-100">
          <input
            type="text"
            placeholder="Enter your keywords"
            value={keyword}
            onChange={(e) => setKeyword(e.target.value)}
            className="form-input"
            disabled={isLoading}
          />
          <div className="last-searched">
            <div className="space-between-full-wid no-padding">
              <p className="sub-title">Last searched</p>
              {lastSearches.length > 0 && (
                <button
                  className="clear-btn small-blue-btn"
                  onClick={handleClearLastSearches}
                  disabled={isLoading}
                >
                  Clear
                </button>
              )}
            </div>
            {lastSearches.length > 0 ? (
              <ul>
                {lastSearches.map((item, i) => (
                  <li key={i} onClick={() => reuseSearchTerm(item)}>
                    {item}
                  </li>
                ))}
              </ul>
            ) : (
              <p
                className="info-message"
                style={{ marginTop: "5px", fontSize: "11px" }}
              >
                No recent searches.
              </p>
            )}
          </div>
        </div>
        <div className="search-section">
          <div>
            <p className="sub-title">Sender address</p>
            <input
              type="text"
              placeholder="Enter email address"
              value={sender}
              onChange={(e) => setSender(e.target.value)}
              className="form-input"
              disabled={isLoading}
            />
          </div>
          <div>
            <p className="sub-title">Receiver address</p>
            <input
              type="text"
              placeholder="Enter email address"
              value={receiver}
              onChange={(e) => setReceiver(e.target.value)}
              className="form-input"
              disabled={isLoading}
            />
          </div>
          <button
            className="search-btn"
            onClick={handleSearch}
            disabled={isSearchDisabled}
          >
            {isLoading ? "Searching..." : "Search"}
          </button>
        </div>
      </div>
    </div>
  );
};

export default AdvancedSearch;
