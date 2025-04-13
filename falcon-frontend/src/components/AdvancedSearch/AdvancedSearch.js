import React, { useState, useEffect, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { toast } from "react-toastify";
import { useAuth } from "../../context/AuthContext";
import { API_BASE_URL } from "../../config/constants";
import Loader from "../Loader/Loader"; // Assuming Loader is in ../Loader/Loader path
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

  useEffect(() => {
    try {
      const storedHistory = localStorage.getItem(SEARCH_HISTORY_KEY);
      if (storedHistory) {
        setLastSearches(JSON.parse(storedHistory));
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

  const isSearchDisabled =
    (!keyword.trim() && !sender.trim() && !receiver.trim()) || isLoading;

  const handleSearch = async (e) => {
    if (e) e.preventDefault();

    if (isSearchDisabled && !isLoading) {
      toast.error("Please enter at least one search criterion.");
      return;
    }
    if (!authToken) {
      toast.error("Authentication error. Please log in again.");
      return;
    }

    setIsLoading(true);

    const params = new URLSearchParams();
    if (keyword.trim()) params.append("keywords", keyword.trim());
    if (sender.trim()) params.append("sender", sender.trim());
    if (receiver.trim()) params.append("recipient", receiver.trim());
    const queryString = params.toString();

    let currentSearchTerm = [keyword.trim(), sender.trim(), receiver.trim()]
      .filter(Boolean)
      .join("; ");

    try {
      const response = await fetch(
        `${API_BASE_URL}/api/mail/search?${queryString}`,
        {
          method: "GET",
          headers: { Authorization: `Bearer ${authToken}` },
        }
      );

      if (!response.ok) {
        let errorMsg = `Search failed (${response.status})`;
        try {
          const errData = await response.json();
          errorMsg = errData.message || errorMsg;
        } catch (errParsing) {}
        throw new Error(errorMsg);
      }

      const results = await response.json();

      if (
        currentSearchTerm &&
        (!lastSearches.length || lastSearches[0] !== currentSearchTerm)
      ) {
        const updatedHistory = [
          currentSearchTerm,
          ...lastSearches.filter((item) => item !== currentSearchTerm),
        ].slice(0, MAX_HISTORY_ITEMS);
        setLastSearches(updatedHistory);
        saveSearchHistory(updatedHistory);
      }

      navigate("/search/results", {
        state: {
          results: results,
          query: { keyword, sender, receiver },
        },
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
    setSender("");
    setReceiver("");
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
                  <li
                    key={i}
                    onClick={() => reuseSearchTerm(item)}
                    style={{ cursor: "pointer" }}
                  >
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
              </p> // Adjusted style
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
