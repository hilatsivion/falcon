import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import "./AdvancedSearch.css";

const AdvancedSearch = () => {
  const [keyword, setKeyword] = useState("");
  const [sender, setSender] = useState("");
  const [receiver, setReceiver] = useState("");
  const [lastSearches, setLastSearches] = useState([
    "Files",
    "Movie tickets",
    "Hand over by",
  ]);
  const navigate = useNavigate();

  const handleSearch = () => {
    if (!keyword && !sender && !receiver) return;
    // Optional: save search to history, make API call, etc
    console.log("Search submitted:", { keyword, sender, receiver });
    navigate("/search-results"); // or any route you have
  };

  return (
    <div className="advanced-search-container">
      <input
        type="text"
        placeholder="Enter your keywords"
        value={keyword}
        onChange={(e) => setKeyword(e.target.value)}
        className="search-input"
      />

      <div className="last-searched">
        <p>Last searched</p>
        <ul>
          {lastSearches.map((item, i) => (
            <li key={i}>{item}</li>
          ))}
        </ul>
        <button className="clear-btn" onClick={() => setLastSearches([])}>
          Clear
        </button>
      </div>

      <div className="search-section">
        <label>Sender address</label>
        <input
          type="text"
          placeholder="Enter name or email address"
          value={sender}
          onChange={(e) => setSender(e.target.value)}
          className="search-input"
        />

        <label>Receiver address</label>
        <input
          type="text"
          placeholder="Enter name or email address"
          value={receiver}
          onChange={(e) => setReceiver(e.target.value)}
          className="search-input"
        />
      </div>

      <button className="search-btn" onClick={handleSearch}>
        Search
      </button>
    </div>
  );
};

export default AdvancedSearch;
