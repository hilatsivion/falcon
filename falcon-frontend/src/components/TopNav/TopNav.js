import React, { useState } from "react";
import { ReactComponent as HamburgerIcon } from "../../assets/icons/black/hamburger.svg";
import { ReactComponent as SearchIcon } from "../../assets/icons/black/search.svg";
import Sidebar from "../Sidebar/Sidebar";
import "./TopNav.css";

const TopNav = () => {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);

  return (
    <>
      {/* Top Navigation */}
      <div className="top-nav">
        <button className="icon-button" onClick={() => setIsSidebarOpen(true)}>
          <HamburgerIcon />
        </button>

        <button className="icon-button">
          <SearchIcon />
        </button>
      </div>

      {/* Sidebar Component */}
      <Sidebar
        isOpen={isSidebarOpen}
        closeSidebar={() => setIsSidebarOpen(false)}
      />
    </>
  );
};

export default TopNav;
