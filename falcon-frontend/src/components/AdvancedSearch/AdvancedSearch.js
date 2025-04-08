import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import "./AdvancedSearch.css";

const AdvancedSearch = () => {
  const [keyword, setKeyword] = useState("");
  const [sender, setSender] = useState("");
  const [receiver, setReceiver] = useState("");
  // last searches by user:
  const [lastSearches, setLastSearches] = useState([
    "Files",
    "Movie tickets",
    "Hand over by",
  ]);
  const navigate = useNavigate();

  const handleSearch = () => {
    if (!keyword && !sender && !receiver) return;
    // TODO:
    // save search to history:
    console.log("Search submitted:", { keyword, sender, receiver });
  };

  return (
    <div className="page-container">
      <div className="advanced-search-container space-between-full-wid">
        <div className="width-100">
          <input
            type="text"
            placeholder="Enter your keywords"
            value={keyword}
            onChange={(e) => setKeyword(e.target.value)}
            className="form-input"
          />

          <div className="last-searched">
            <div className="space-between-full-wid no-padding">
              <p className="sub-title">Last searched</p>
              <button
                className="clear-btn small-blue-btn"
                onClick={() => setLastSearches([])}
              >
                Clear
              </button>
            </div>

            <ul>
              {lastSearches.map((item, i) => (
                <li key={i}>{item}</li>
              ))}
            </ul>
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
            />
          </div>
        </div>

        <button className="search-btn" onClick={handleSearch}>
          Search
        </button>
      </div>
    </div>
  );
};

export default AdvancedSearch;
