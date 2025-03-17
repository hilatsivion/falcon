import React from "react";
import "./loadingData.css";
import "../../../styles/global.css";

import logo from "../../../assets/images/falcon-logo-full-blue.svg";
import spinnerIcon from "../../../assets/icons/black/spinner.svg";

const LoadingDataPage = () => {
  return (
    <div className="loading-container">
      {/* Falcon Logo */}
      <img className="loading-logo" src={logo} alt="Falcon Logo" />

      {/* Spinning Loader */}
      <img className="loading-spinner" src={spinnerIcon} alt="Loading..." />

      {/* Loading Text */}
      <p className="loading-text">
        <span>Fetching all your Data</span>
      </p>
      <p className="loading-subtext">
        This might take a few minutes, don’t close the app
      </p>
    </div>
  );
};

export default LoadingDataPage;
