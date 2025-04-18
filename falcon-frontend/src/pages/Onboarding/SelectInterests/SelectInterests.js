import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { motion } from "framer-motion";
import "./interests.css";
import "../../../styles/global.css";

import { useAuth } from "../../../context/AuthContext";
import { API_BASE_URL } from "../../../config/constants";
import Loader from "../../../components/Loader/Loader";
import { toast } from "react-toastify";

import selectSound from "../../../assets/sounds/select-tag.mp3";
import errorSound from "../../../assets/sounds/error-message.mp3";
import logo from "../../../assets/images/falcon-white-full.svg";
import workIcon from "../../../assets/icons/blue/work.svg";
import schoolIcon from "../../../assets/icons/blue/school.svg";
import socialIcon from "../../../assets/icons/blue/social.svg";
import newsIcon from "../../../assets/icons/blue/news.svg";
import promotionsIcon from "../../../assets/icons/blue/promotions.svg";
import financeIcon from "../../../assets/icons/blue/finance.svg";
import familyIcon from "../../../assets/icons/blue/family.svg";
import personalIcon from "../../../assets/icons/blue/personal.svg";
import travelIcon from "../../../assets/icons/blue/travel.svg";
import healthIcon from "../../../assets/icons/blue/health.svg";

const interestOptions = [
  { name: "Work", icon: workIcon },
  { name: "School", icon: schoolIcon },
  { name: "Social", icon: socialIcon },
  { name: "News", icon: newsIcon },
  { name: "Promotions", icon: promotionsIcon },
  { name: "Finance", icon: financeIcon },
  { name: "Family & Friends", icon: familyIcon },
  { name: "Personal", icon: personalIcon },
  { name: "Travel", icon: travelIcon },
  { name: "Health", icon: healthIcon },
];

const SelectInterests = () => {
  const [selectedTags, setSelectedTags] = useState([]);
  const [animatedTags, setAnimatedTags] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const { authToken } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    setTimeout(() => {
      setAnimatedTags(interestOptions.map((item) => item.name));
    }, 100);
  }, []);

  let isPlaying = false;
  const toggleTag = (tag) => {
    setSelectedTags((prev) => {
      const isSelected = prev.includes(tag);
      if (!isSelected && !isPlaying) {
        isPlaying = true;
        const audio = new Audio(selectSound);
        audio.play().catch((e) => console.error("Audio play failed:", e));
        setTimeout(() => {
          isPlaying = false;
        }, 500);
      }
      return isSelected ? prev.filter((t) => t !== tag) : [...prev, tag];
    });
  };

  const selectAll = () => {
    if (selectedTags.length === interestOptions.length) {
      setSelectedTags([]);
    } else {
      setSelectedTags(interestOptions.map((item) => item.name));
    }
  };

  const showError = (message) => {
    const audio = new Audio(errorSound);
    audio.play().catch((e) => console.error("Audio play failed:", e));
    toast.error(message, {
      position: "top-right",
      autoClose: 4000,
      hideProgressBar: false,
      pauseOnHover: true,
      draggable: true,
    });
  };

  const handleDone = async () => {
    if (selectedTags.length === 0) {
      showError("Please select at least one interest.");
      return;
    }

    if (!authToken) {
      showError("Authentication error. Please log in again.");
      navigate("/login");
      return;
    }

    setIsLoading(true);

    try {
      const response = await fetch(`${API_BASE_URL}/api/user/save-tags`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${authToken}`,
        },
        body: JSON.stringify({ Tags: selectedTags }),
      });

      if (!response.ok) {
        let errorMsg = "Failed to save interests.";
        try {
          const errorData = await response.json();
          errorMsg = errorData.message || errorMsg;
        } catch {}
        throw new Error(errorMsg);
      }

      navigate("/loadingData");
    } catch (err) {
      console.error("Save Interests Error:", err);
      showError(err.message || "An error occurred while saving interests.");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="welcome-screen-container interests-container">
      {isLoading && <Loader />}

      <motion.img
        className="logo-full-white-small"
        src={logo}
        alt="logo-falcon"
      />

      <div className="interests-content">
        <motion.h2 className="interests-title">Select Your Interests</motion.h2>
        <motion.p className="sub-title">
          Select the tags most relevant and important to you.
        </motion.p>

        <div className="tags-containers">
          {interestOptions.map((item) => (
            <div
              key={item.name}
              className={`tag animate pop ${
                animatedTags.includes(item.name)
                  ? "animated-finish"
                  : "animated"
              } ${selectedTags.includes(item.name) ? "selected" : ""}`}
              style={{ animationDelay: `${Math.random() * 0.9}s` }}
              onClick={() => toggleTag(item.name)}
            >
              <img src={item.icon} alt={item.name} />
              <span>{item.name}</span>
            </div>
          ))}
        </div>

        <motion.button className="btn-all" onClick={selectAll}>
          {selectedTags.length === interestOptions.length
            ? "Deselect All"
            : "Select All"}
        </motion.button>
      </div>

      <button
        className="btn-white btn-done"
        onClick={handleDone}
        disabled={isLoading}
      >
        {isLoading ? "Saving..." : "Done"}
      </button>
    </div>
  );
};

export default SelectInterests;
