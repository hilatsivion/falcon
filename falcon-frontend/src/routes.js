import React from "react";
import { Routes, Route } from "react-router-dom";
import WelcomeScreen from "./pages/Welcome/WelcomeScreens/WelcomeScreen";
import Login from "./pages/Welcome/Connect/Login";
import Signup from "./pages/Welcome/Connect/Signup";
import SelectInterests from "./pages/Welcome/SelectInterests/SelectInterests";
import LoadingScreen from "./pages/Welcome/LoadingScreen/LoadingScreen";

const AppRoutes = () => {
  return (
    <Routes>
      <Route path="/" element={<WelcomeScreen />} />
      <Route path="/signup" element={<Signup />} />
      {/* <Route path="/login" element={<Login />} /> */}
      <Route path="/interests" element={<SelectInterests />} />
      {/* <Route path="/loading" element={<LoadingScreen />} /> */}
    </Routes>
  );
};

export default AppRoutes;
