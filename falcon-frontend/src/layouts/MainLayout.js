import React from "react";
import TopNav from "../components/TopNav/TopNav";
import Navbar from "../components/Navbar/Navbar";

const MainLayout = ({ children }) => {
  return (
    <div>
      <TopNav />
      {children}
      <Navbar />
    </div>
  );
};

export default MainLayout;
