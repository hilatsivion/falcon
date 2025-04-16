// src/pages/Main/FilterFolderPage/FilterFolderPage.js

import React, { useState, useEffect, useCallback } from "react"; // Import hooks
import { useNavigate } from "react-router-dom";
import "./FilterFolderPage.css";
import NewFilterPopup from "./NewFilterPopup";
import { ReactComponent as NumberOfEmailIcon } from "../../../assets/icons/black/email-enter-icon.svg";
import { ReactComponent as Dots } from "../../../assets/icons/black/more-dots.svg";
import Loader from "../../../components/Loader/Loader"; // Import Loader
import { useAuth } from "../../../context/AuthContext"; // Import useAuth
import { API_BASE_URL } from "../../../config/constants"; // Import API Base URL
import { toast } from "react-toastify"; // Import toast

// Remove dummyFilters array

const FilterFolderPage = ({ setIsListView }) => {
  const [isPopupOpen, setIsPopupOpen] = useState(false);
  const navigate = useNavigate();
  const { authToken } = useAuth(); // Get token

  // --- State for real data ---
  const [filters, setFilters] = useState([]); // To hold filters from backend
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState(null);
  const [availableTags, setAvailableTags] = useState([]); // State for tags for the popup

  // --- Function to fetch filters and tags ---
  const fetchFiltersAndTags = useCallback(async () => {
    if (!authToken) {
      setError("Not authenticated");
      setFilters([]);
      setAvailableTags([]);
      return;
    }
    setIsLoading(true);
    setError(null);
    try {
      // Fetch filters and tags concurrently
      const [filtersResponse, tagsResponse] = await Promise.all([
        fetch(`${API_BASE_URL}/api/mail/filters`, {
          headers: { Authorization: `Bearer ${authToken}` },
        }),
        fetch(`${API_BASE_URL}/api/user/tags`, {
          // Fetch tags needed for the popup
          headers: { Authorization: `Bearer ${authToken}` },
        }),
      ]);

      if (!filtersResponse.ok)
        throw new Error(`Failed to fetch filters (${filtersResponse.status})`);
      if (!tagsResponse.ok)
        throw new Error(`Failed to fetch tags (${tagsResponse.status})`);

      const filtersData = await filtersResponse.json();
      const tagsData = await tagsResponse.json();

      setFilters(Array.isArray(filtersData) ? filtersData : []);
      // Process tags for the popup
      setAvailableTags(
        Array.isArray(tagsData)
          ? tagsData.map((tag) => ({
              tagId: tag.tagId, // Ensure field names match what backend sends
              tagName: tag.tagName,
            }))
          : []
      );
    } catch (err) {
      const errorMsg = err.message || "Could not load filter data.";
      console.error("Fetch error:", err);
      setError(errorMsg);
      toast.error(errorMsg);
      setFilters([]);
      setAvailableTags([]);
    } finally {
      setIsLoading(false);
    }
  }, [authToken]);

  // --- Fetch data on component mount ---
  useEffect(() => {
    fetchFiltersAndTags();
  }, [fetchFiltersAndTags]);

  // --- Handler to create a new filter ---
  const handleSaveFilter = async (filterDataFromPopup) => {
    if (!authToken) {
      toast.error("Authentication error.");
      return;
    }
    setIsLoading(true);
    try {
      const response = await fetch(`${API_BASE_URL}/api/mail/filters`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${authToken}`,
        },
        body: JSON.stringify(filterDataFromPopup), // Send data from popup
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(
          errorData.message || `Failed to create filter (${response.status})`
        );
      }

      toast.success(`Filter "${filterDataFromPopup.Name}" created!`);
      setIsPopupOpen(false); // Close popup on success
      fetchFiltersAndTags(); // Refresh the list
    } catch (err) {
      console.error("Save filter error:", err);
      toast.error(`Failed to save filter: ${err.message}`);
      // Decide if popup should stay open on error
      // setIsPopupOpen(false);
    } finally {
      setIsLoading(false);
    }
  };

  // --- Navigation Handler (Updated to use backend data structure) ---
  const handleFilterClick = (filter) => {
    // Use filterFolderId from the backend DTO
    navigate(`/filters/${filter.filterFolderId}`, {
      // Pass necessary state for GenericEmailPage header
      state: {
        id: filter.filterFolderId,
        name: filter.name,
        color: filter.folderColor, // Pass color for header bar
      },
    });
    // Keep the logic to switch view in parent
    if (setIsListView) {
      setIsListView(true);
    } else {
      console.warn("setIsListView prop missing from FilterFolderPage");
    }
  };

  // --- Render Logic ---
  if (isLoading) {
    return <Loader />;
  }

  if (error) {
    return <p className="error-message padding-sides">{error}</p>;
  }

  return (
    <>
      <div className="folder-grid">
        {/* Map over the filters state variable */}
        {filters.map((filter) => (
          <div
            className={`folder-card`}
            // Use unique key from backend data
            key={filter.filterFolderId}
            onClick={() => handleFilterClick(filter)}
          >
            {/* Use folderColor from backend for the top row style */}
            <div
              className={`folder-top-row`}
              style={{ background: filter.folderColor || "var(--folder-blue)" }} // Apply color dynamically
            >
              <Dots className="folder-dot" />
            </div>

            <div className="body-folder">
              <div>
                {/* Use name from backend */}
                <p className="folder-title">{filter.name}</p>
                <p className="folder-subtext">
                  {/* Use newEmailsCount from backend */}
                  {filter.newEmailsCount ?? 0} new email
                  {filter.newEmailsCount !== 1 ? "s" : ""}
                </p>
              </div>
              <div className="folder-count-row">
                <span className="num-email-icon">
                  <NumberOfEmailIcon />
                </span>
                {/* Use totalEmails from backend */}
                <span className="count-num">{filter.totalEmails ?? 0}</span>
              </div>
            </div>
          </div>
        ))}

        <div
          className="add-folder-card dashed"
          onClick={() => setIsPopupOpen(true)}
        >
          <div className="plus-circle">＋</div>
          <p className="add-folder-text">Add filter folder</p>
        </div>
      </div>

      {isPopupOpen && (
        <NewFilterPopup
          onClose={() => setIsPopupOpen(false)}
          onSave={handleSaveFilter} // Pass the actual save handler
          availableTags={availableTags} // Pass fetched tags
        />
      )}
    </>
  );
};

export default FilterFolderPage;
