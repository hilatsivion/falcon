import React from "react";
import { BrowserRouter } from "react-router-dom";
import AppRoutes from "./routes";
import "./styles/global.css";

const App = () => {
  return (
    <BrowserRouter
      basename={process.env.NODE_ENV === "production" ? "/falcon" : "/"}
    >
      <AppRoutes />
    </BrowserRouter>
  );
};

export default App;
