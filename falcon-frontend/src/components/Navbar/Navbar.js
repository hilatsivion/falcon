import React from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { ReactComponent as InboxIcon } from "../../assets/icons/black/direct.svg";
import { ReactComponent as InboxIconActive } from "../../assets/icons/blue/direct.svg";
import { ReactComponent as AnalyticsIcon } from "../../assets/icons/black/diagram.svg";
import { ReactComponent as AnalyticsIconActive } from "../../assets/icons/blue/diagram.svg";
import { ReactComponent as ComposeIcon } from "../../assets/icons/black/add-square.svg";
import { ReactComponent as ComposeIconActive } from "../../assets/icons/blue/add-square.svg";
import "./Navbar.css";

const Navbar = () => {
  const navigate = useNavigate();
  const location = useLocation();

  return (
    <nav className="navbar">
      <div
        className={`nav-item ${
          location.pathname === "/analytics" ? "active" : ""
        }`}
        onClick={() => navigate("/analytics")}
      >
        {location.pathname === "/analytics" ? (
          <AnalyticsIconActive />
        ) : (
          <AnalyticsIcon />
        )}
        <span>analytics</span>
      </div>

      <div
        className={`nav-item ${location.pathname === "/inbox" ? "active" : ""}`}
        onClick={() => navigate("/inbox")}
      >
        {location.pathname === "/inbox" ? <InboxIconActive /> : <InboxIcon />}
        <span>inbox</span>
      </div>

      <div
        className={`nav-item ${
          location.pathname === "/compose" ? "active" : ""
        }`}
        onClick={() => navigate("/compose")}
      >
        {location.pathname === "/compose" ? (
          <ComposeIconActive />
        ) : (
          <ComposeIcon />
        )}
        <span>compose</span>
      </div>
    </nav>
  );
};

export default Navbar;
