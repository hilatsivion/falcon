import React, { useState, useEffect, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import "./FilterFolderPage.css";
import NewFilterPopup from "./NewFilterPopup";
import FilterActionsPopup from "../../../components/Popup/FilterActionsPopup";
import { ReactComponent as NumberOfEmailIcon } from "../../../assets/icons/black/email-enter-icon.svg";
import { ReactComponent as Dots } from "../../../assets/icons/black/more-dots.svg";
import Loader from "../../../components/Loader/Loader";
import { useAuth } from "../../../context/AuthContext";
import { API_BASE_URL } from "../../../config/constants";
import { toast } from "react-toastify";

const FilterFolderPage = ({ setIsListView }) => {
  const [isPopupOpen, setIsPopupOpen] = useState(false);
  const [actionPopupOpen, setActionPopupOpen] = useState(false);
  const [selectedFilter, setSelectedFilter] = useState(null);
  const navigate = useNavigate();
  const { authToken } = useAuth();
  const [filters, setFilters] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState(null);
  const [availableTags, setAvailableTags] = useState([]);

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
      const [filtersResponse, tagsResponse] = await Promise.all([
        fetch(`${API_BASE_URL}/api/mail/filters`, {
          headers: { Authorization: `Bearer ${authToken}` },
        }),
        fetch(`${API_BASE_URL}/api/user/tags`, {
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
      setAvailableTags(
        Array.isArray(tagsData)
          ? tagsData.map((tag) => ({ tagId: tag.tagId, tagName: tag.tagName }))
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

  useEffect(() => {
    fetchFiltersAndTags();
  }, [fetchFiltersAndTags]);

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
        body: JSON.stringify(filterDataFromPopup),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(
          errorData.message || `Failed to create filter (${response.status})`
        );
      }

      toast.success(`Filter "${filterDataFromPopup.Name}" created!`);
      setIsPopupOpen(false);
      fetchFiltersAndTags();
    } catch (err) {
      console.error("Save filter error:", err);
      toast.error(`Failed to save filter: ${err.message}`);
    } finally {
      setIsLoading(false);
    }
  };

  const handleFilterClick = (filter) => {
    navigate(`/filters/${filter.filterFolderId}`, {
      state: {
        id: filter.filterFolderId,
        name: filter.name,
        color: filter.folderColor,
      },
    });
    if (setIsListView) setIsListView(true);
  };

  const openActionPopup = (e, filter) => {
    e.stopPropagation(); // prevent triggering folder open
    setSelectedFilter(filter);
    setActionPopupOpen(true);
  };

  if (isLoading) return <Loader />;
  if (error)
    return <p className="error-message padding-sides">Error: {error}</p>;

  return (
    <>
      <div className="folder-grid">
        {filters.map((filter) => (
          <div
            className="folder-card"
            key={filter.filterFolderId}
            onClick={() => handleFilterClick(filter)}
          >
            <div
              className="folder-top-row"
              style={{ background: filter.folderColor || "var(--folder-blue)" }}
            >
              <Dots
                className="folder-dot"
                onClick={(e) => openActionPopup(e, filter)}
              />
            </div>
            <div className="body-folder">
              <div>
                <p className="folder-title">{filter.name}</p>
                <p className="folder-subtext">
                  {filter.newEmailsCount ?? 0} new email
                  {filter.newEmailsCount !== 1 ? "s" : ""}
                </p>
              </div>
              <div className="folder-count-row">
                <span className="num-email-icon">
                  <NumberOfEmailIcon />
                </span>
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
          onSave={handleSaveFilter}
          availableTags={availableTags}
        />
      )}

      {actionPopupOpen && (
        <FilterActionsPopup
          onClose={() => setActionPopupOpen(false)}
          onEdit={() => {
            console.log("Edit", selectedFilter);
            setActionPopupOpen(false);
          }}
          onDelete={() => {
            console.log("Delete", selectedFilter);
            setActionPopupOpen(false);
          }}
        />
      )}
    </>
  );
};

export default FilterFolderPage;
