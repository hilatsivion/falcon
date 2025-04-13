import React from "react";
import { Routes, Route, Navigate } from "react-router-dom";
import Inbox from "../pages/Main/Inbox/Inbox";
import Analytics from "../pages/Main/Analytics/Analytics";
import Compose from "../pages/Main/Compose/Compose";
import MainLayout from "../layouts/MainLayout";
import OnboardingRoutes from "./OnboardingRoutes";
import AdvancedSearch from "../components/AdvancedSearch/AdvancedSearch";
import SelectInterests from "../pages/Onboarding/SelectInterests/SelectInterests"; // <<< Import Interests
import LoadingDataScreen from "../pages/Onboarding/LoadingDataScreen/LoadingDataScreen"; // <<< Import Loading

import { useAuth } from "../context/AuthContext";

const AppRoutes = () => {
  const { isAuthenticated } = useAuth();

  return (
    <Routes>
      {/* Onboarding Routes */}
      {!isAuthenticated && (
        <>
          <Route path="/*" element={<OnboardingRoutes />} />
          <Route path="*" element={<Navigate to="/" />} />
        </>
      )}

      {/* Main Routes */}
      {isAuthenticated && (
        <>
          <Route path="/interests" element={<SelectInterests />} />
          <Route path="/loadingData" element={<LoadingDataScreen />} />
          <Route
            path="/inbox"
            element={
              <MainLayout>
                <Inbox />
              </MainLayout>
            }
          />
          <Route
            path="/analytics"
            element={
              <MainLayout>
                <Analytics />
              </MainLayout>
            }
          />
          <Route
            path="/compose"
            element={
              <MainLayout>
                <Compose />
              </MainLayout>
            }
          />
          <Route
            path="/search"
            element={
              <MainLayout>
                <AdvancedSearch />
              </MainLayout>
            }
          />

          <Route path="*" element={<Navigate to="/inbox" />} />
        </>
      )}
    </Routes>
  );
};

export default AppRoutes;
