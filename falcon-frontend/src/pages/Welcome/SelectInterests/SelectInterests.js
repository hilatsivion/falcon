import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { motion, AnimatePresence } from "framer-motion";
import "./interests.css";
import "../../../styles/global.css";

import selectSound from "../../../assets/sounds/select-tag.mp3";
import logo from "../../../assets/images/falcon-white-full.svg";

// Import icons from assets
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

const errorAnimation = {
  hidden: { opacity: 0, y: -50 },
  visible: { opacity: 1, y: 0, transition: { duration: 0.5 } },
  exit: { opacity: 0, transition: { duration: 0.5 } },
};

const SelectInterests = () => {
  const [selectedTags, setSelectedTags] = useState([]);
  const [error, setError] = useState("");
  const [animatedTags, setAnimatedTags] = useState([]);

  const navigate = useNavigate();

  useEffect(() => {
    setTimeout(() => {
      setAnimatedTags(interestOptions.map((item) => item.name)); // Apply class to all tags
    }, 2000);
  }, []);

  let isPlaying = false;
  // Toggle a single tag selection
  const toggleTag = (tag) => {
    setSelectedTags((prev) => {
      const isSelected = prev.includes(tag);

      if (!isSelected && !isPlaying) {
        isPlaying = true; // Set flag to prevent multiple triggers
        const audio = new Audio(selectSound);
        audio.play();

        setTimeout(() => {
          isPlaying = false; // Reset after sound finishes
        }, 500); // Adjust timeout if needed
      }

      return isSelected ? prev.filter((t) => t !== tag) : [...prev, tag];
    });
  };

  // Select All Tags
  const selectAll = () => {
    if (selectedTags.length === interestOptions.length) {
      setSelectedTags([]); // Deselect all if all are selected
    } else {
      setSelectedTags(interestOptions.map((item) => item.name)); // Select all
    }
  };

  // Show Error for 5 seconds
  const showError = (message) => {
    setError(message);
    setTimeout(() => {
      setError("");
    }, 5000);
  };

  // Handle Done Button Click
  const handleDone = () => {
    if (selectedTags.length === 0) {
      console.log(selectedTags);
      showError("Please select at least one interest.");
      return;
    }
    console.log("Selected Interests:", selectedTags);
    // Here you could send `selectedTags` to an API or store it
    navigate("/next-page"); // Replace with the actual next page
  };

  return (
    <div className="welcome-screen-container interests-container">
      {/* Error Popup */}
      <AnimatePresence>
        {error && (
          <motion.div
            className="error-popup"
            variants={errorAnimation}
            initial="hidden"
            animate="visible"
            exit="exit"
          >
            {error}
          </motion.div>
        )}
      </AnimatePresence>

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
          {interestOptions.map((item, index) => (
            <div
              key={item.name}
              className={`tag animate pop ${
                animatedTags.includes(item.name)
                  ? "animated-finish"
                  : "animated"
              } ${selectedTags.includes(item.name) ? "selected" : ""}`}
              style={{ animationDelay: `${Math.random() * 0.9}s` }}
              onPointerDown={(event) => toggleTag(item.name, event)}
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
      <button className="btn-white btn-done" onClick={handleDone}>
        Done
      </button>
    </div>
  );
};

export default SelectInterests;
