import React from "react";
import { Routes, Route } from "react-router-dom";
import WelcomeScreen from "./pages/Welcome/WelcomeScreens/WelcomeScreen";
import Login from "./pages/Welcome/Connect/Login";
import Signup from "./pages/Welcome/Connect/Signup";
import SelectInterests from "./pages/Welcome/SelectInterests/SelectInterests";
import LoadingDataScreen from "./pages/Welcome/LoadingDataScreen/LoadingDataScreen";

import Inbox from "./pages/Main/Inbox/Inbox";

const AppRoutes = () => {
  return (
    <Routes>
      <Route path="/" element={<WelcomeScreen />} />
      <Route path="/signup" element={<Signup />} />
      <Route path="/login" element={<Login />} />
      <Route path="/interests" element={<SelectInterests />} />
      <Route path="/loadingData" element={<LoadingDataScreen />} />

      <Route path="/inbox" element={<Inbox />} />
    </Routes>
  );
};

export default AppRoutes;
