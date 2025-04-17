import React, { useState } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { ReactComponent as HamburgerIcon } from "../../assets/icons/black/hamburger.svg";
import { ReactComponent as SearchIcon } from "../../assets/icons/black/search.svg";
import Sidebar from "../Sidebar/Sidebar";
import { ReactComponent as ArrowRightIcon } from "../../assets/icons/black/arrow-right-nav.svg";

import "./TopNav.css";

const TopNav = () => {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();
  const isSearchPage = location.pathname === "/search";

  return (
    <>
      {/* Top Navigation */}
      <div className="top-nav">
        <button className="icon-button" onClick={() => setIsSidebarOpen(true)}>
          <HamburgerIcon />
        </button>

        <button
          className="icon-button"
          onClick={() => {
            if (isSearchPage) {
              navigate("/inbox");
            } else {
              navigate("/search");
            }
          }}
        >
          {isSearchPage ? <ArrowRightIcon /> : <SearchIcon />}
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
