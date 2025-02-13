import React from "react";
import { BrowserRouter as Router, Routes, Route } from "react-router-dom";
import WelcomeScreen from "./pages/WelcomeScreen";
import Login from "./pages/Login";
import Signup from "./pages/Signup";
import SelectInterests from "./pages/SelectInterests";
import LoadingScreen from "./pages/LoadingScreen";

const AppRoutes = () => {
  return (
    <Router>
      <Routes>
        <Route path="/" element={<WelcomeScreen />} />
        <Route path="/login" element={<Login />} />
        <Route path="/signup" element={<Signup />} />
        <Route path="/interests" element={<SelectInterests />} />
        <Route path="/loading" element={<LoadingScreen />} />
      </Routes>
    </Router>
  );
};

export default AppRoutes;
