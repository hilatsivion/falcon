import React, { useState, useEffect, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import "./FilterFolderPage.css";
import NewFilterPopup from "./NewFilterPopup";
import FilterActionsPopup from "../../../components/Popup/FilterActionsPopup";
import ConfirmPopup from "../../../components/Popup/ConfirmPopup";
import { ReactComponent as NumberOfEmailIcon } from "../../../assets/icons/black/email-enter-icon.svg";
import { ReactComponent as Dots } from "../../../assets/icons/black/more-dots.svg";
import Loader from "../../../components/Loader/Loader";
import { useAuth } from "../../../context/AuthContext";
import { API_BASE_URL } from "../../../config/constants";
import { toast } from "react-toastify";

const FilterFolderPage = ({ setIsListView }) => {
  const [isCreatePopupOpen, setIsCreatePopupOpen] = useState(false);
  const [isEditPopupOpen, setIsEditPopupOpen] = useState(false);
  const [actionPopupOpen, setActionPopupOpen] = useState(false);
  const [selectedFilter, setSelectedFilter] = useState(null);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [filterToDelete, setFilterToDelete] = useState(null);

  const navigate = useNavigate();
  const { authToken } = useAuth();
  const [filters, setFilters] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState(null);
  const [availableTags, setAvailableTags] = useState([]);

  // fetch all the filters from the server
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

  const handleCreateFilter = async (filterDataFromPopup) => {
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
      setIsCreatePopupOpen(false);
      fetchFiltersAndTags();
    } catch (err) {
      console.error("Create filter error:", err);
      toast.error(`Failed to create filter: ${err.message}`);
    } finally {
      setIsLoading(false);
    }
  };

  const handleUpdateFilter = async (filterId, filterDataFromPopup) => {
    if (!authToken) {
      toast.error("Authentication error.");
      return;
    }
    setIsLoading(true);
    try {
      const response = await fetch(
        `${API_BASE_URL}/api/mail/filters/${filterId}`,
        {
          method: "PUT",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${authToken}`,
          },
          body: JSON.stringify(filterDataFromPopup),
        }
      );

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(
          errorData.message || `Failed to update filter (${response.status})`
        );
      }

      toast.success(`Filter "${filterDataFromPopup.Name}" updated!`);
      setIsEditPopupOpen(false);
      setSelectedFilter(null);
      fetchFiltersAndTags();
    } catch (err) {
      console.error("Update filter error:", err);
      toast.error(`Failed to update filter: ${err.message}`);
    } finally {
      setIsLoading(false);
    }
  };

  const handleDeleteFilter = (filter) => {
    setFilterToDelete(filter);
    setShowDeleteConfirm(true);
    setActionPopupOpen(false);
  };

  const handleConfirmDelete = async () => {
    if (!authToken || !filterToDelete) {
      toast.error("Cannot delete filter: Missing information.");
      setShowDeleteConfirm(false);
      setFilterToDelete(null);
      return;
    }
    setIsLoading(true);
    try {
      const response = await fetch(
        `${API_BASE_URL}/api/mail/filters/${filterToDelete.filterFolderId}`,
        {
          method: "DELETE",
          headers: {
            Authorization: `Bearer ${authToken}`,
          },
        }
      );

      if (!response.ok && response.status !== 204) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(
          errorData.message || `Failed to delete filter (${response.status})`
        );
      }

      toast.success(`Filter "${filterToDelete.name}" deleted!`);
      fetchFiltersAndTags();
    } catch (err) {
      console.error("Delete filter error:", err);
      toast.error(`Failed to delete filter: ${err.message}`);
    } finally {
      setIsLoading(false);
      setShowDeleteConfirm(false);
      setFilterToDelete(null);
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
    e.stopPropagation();
    setSelectedFilter(filter);
    setActionPopupOpen(true);
  };

  const openEditPopup = () => {
    if (selectedFilter) {
      const filterWithTagIds = {
        ...selectedFilter,
        tagIds: selectedFilter.tags?.map((tag) => tag.tagId) || [],
      };
      setSelectedFilter(filterWithTagIds);
      setIsEditPopupOpen(true);
      setActionPopupOpen(false);
    }
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
          onClick={() => setIsCreatePopupOpen(true)}
        >
          <div className="plus-circle">＋</div>
          <p className="add-folder-text">Add filter folder</p>
        </div>
      </div>

      {isCreatePopupOpen && (
        <NewFilterPopup
          onClose={() => setIsCreatePopupOpen(false)}
          onSave={handleCreateFilter}
          availableTags={availableTags}
          isEditing={false}
        />
      )}

      {isEditPopupOpen && selectedFilter && (
        <NewFilterPopup
          onClose={() => {
            setIsEditPopupOpen(false);
            setSelectedFilter(null);
          }}
          onSave={handleUpdateFilter}
          availableTags={availableTags}
          isEditing={true}
          editingFilter={selectedFilter}
        />
      )}

      {actionPopupOpen && selectedFilter && (
        <FilterActionsPopup
          onClose={() => {
            setActionPopupOpen(false);
            setSelectedFilter(null);
          }}
          onEdit={openEditPopup}
          onDelete={() => handleDeleteFilter(selectedFilter)}
        />
      )}

      {showDeleteConfirm && filterToDelete && (
        <ConfirmPopup
          isOpen={showDeleteConfirm}
          message={`Are you sure you want to delete the filter "${filterToDelete.name}"?`}
          confirmText="Delete"
          cancelText="Cancel"
          onConfirm={handleConfirmDelete}
          onCancel={() => {
            setShowDeleteConfirm(false);
            setFilterToDelete(null);
          }}
        />
      )}
    </>
  );
};

export default FilterFolderPage;
