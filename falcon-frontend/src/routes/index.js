import React from "react";
import { Routes, Route, Navigate } from "react-router-dom";
import Analytics from "../pages/Main/Analytics/Analytics";
import Compose from "../pages/Main/Compose/Compose";
import MainLayout from "../layouts/MainLayout";
import OnboardingRoutes from "./OnboardingRoutes";
import AdvancedSearch from "../components/AdvancedSearch/AdvancedSearch";
import SelectInterests from "../pages/Onboarding/SelectInterests/SelectInterests"; // <<< Import Interests
import LoadingDataScreen from "../pages/Onboarding/LoadingDataScreen/LoadingDataScreen"; // <<< Import Loading
import GenericEmailPage from "../pages/Main/Inbox/GenericEmailPage";

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
                <GenericEmailPage />
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
          <Route
            path="/unread"
            element={
              <MainLayout>
                <GenericEmailPage />
              </MainLayout>
            }
          />
          <Route
            path="/favorite"
            element={
              <MainLayout>
                <GenericEmailPage />
              </MainLayout>
            }
          />
          <Route
            path="/sent"
            element={
              <MainLayout>
                <GenericEmailPage />
              </MainLayout>
            }
          />
          <Route
            path="/search-results"
            element={
              <MainLayout>
                <GenericEmailPage />
              </MainLayout>
            }
          />
          <Route
            path="/filter-results"
            element={
              <MainLayout>
                <GenericEmailPage />
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
