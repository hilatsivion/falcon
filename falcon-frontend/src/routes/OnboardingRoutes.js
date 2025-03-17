import React from "react";
import { Routes, Route } from "react-router-dom";
import WelcomeScreen from "../pages/Onboarding/WelcomeScreens/WelcomeScreen";
import Login from "../pages/Onboarding/Connect/Login";
import Signup from "../pages/Onboarding/Connect/Signup";
import SelectInterests from "../pages/Onboarding/SelectInterests/SelectInterests";
import LoadingDataScreen from "../pages/Onboarding/LoadingDataScreen/LoadingDataScreen";

const OnboardingRoutes = () => {
  return (
    <Routes>
      <Route path="/" element={<WelcomeScreen />} />
      <Route path="/signup" element={<Signup />} />
      <Route path="/login" element={<Login />} />
      <Route path="/interests" element={<SelectInterests />} />
      <Route path="/loadingData" element={<LoadingDataScreen />} />
    </Routes>
  );
};

export default OnboardingRoutes;
