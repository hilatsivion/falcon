import React from "react";
import { Routes, Route } from "react-router-dom";
import OnboardingRoutes from "./OnboardingRoutes";
import MainRoutes from "./MainRoutes";

const AppRoutes = () => {
  const isAuthenticated = localStorage.getItem("isAuthenticated") === "true";

  return (
    <Routes>
      <Route
        path="/*"
        element={isAuthenticated ? <MainRoutes /> : <OnboardingRoutes />}
      />
    </Routes>
  );
};

export default AppRoutes;
