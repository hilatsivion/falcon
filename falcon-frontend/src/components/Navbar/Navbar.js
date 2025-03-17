import React, { useState } from "react";
import { ReactComponent as InboxIcon } from "../../assets/icons/black/direct.svg";
import { ReactComponent as InboxIconActive } from "../../assets/icons/blue/direct.svg";
import { ReactComponent as AnalyticsIcon } from "../../assets/icons/black/diagram.svg";
import { ReactComponent as AnalyticsIconActive } from "../../assets/icons/blue/diagram.svg";
import { ReactComponent as ComposeIcon } from "../../assets/icons/black/add-square.svg";
import { ReactComponent as ComposeIconActive } from "../../assets/icons/blue/add-square.svg";
import "./Navbar.css";

const Navbar = () => {
  const [activePage, setActivePage] = useState("inbox");

  return (
    <nav className="navbar">
      <div
        className={`nav-item ${activePage === "analytics" ? "active" : ""}`}
        onClick={() => setActivePage("analytics")}
      >
        {activePage === "analytics" ? (
          <AnalyticsIconActive />
        ) : (
          <AnalyticsIcon />
        )}
        <span>analytics</span>
      </div>

      <div
        className={`nav-item ${activePage === "inbox" ? "active" : ""}`}
        onClick={() => setActivePage("inbox")}
      >
        {activePage === "inbox" ? <InboxIconActive /> : <InboxIcon />}
        <span>inbox</span>
      </div>

      <div
        className={`nav-item ${activePage === "compose" ? "active" : ""}`}
        onClick={() => setActivePage("compose")}
      >
        {activePage === "compose" ? <ComposeIconActive /> : <ComposeIcon />}
        <span>compose</span>
      </div>
    </nav>
  );
};

export default Navbar;
