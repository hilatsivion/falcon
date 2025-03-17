import React from "react";
import { Routes, Route } from "react-router-dom";
import Inbox from "../pages/Main/Inbox/Inbox";
import Analytics from "../pages/Main/Analytics/Analytics";
import Compose from "../pages/Main/Compose/Compose";
import MainLayout from "../layouts/MainLayout";

const MainRoutes = () => {
  return (
    <Routes>
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
    </Routes>
  );
};

export default MainRoutes;
