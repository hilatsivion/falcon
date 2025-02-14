import React from "react";
// import { BrowserRouter } from "react-router-dom";
import { HashRouter as Router } from "react-router-dom";
import AppRoutes from "./routes";
import "./styles/global.css";

const App = () => {
  return (
    <Router basename={process.env.NODE_ENV === "production" ? "/falcon" : "/"}>
      <AppRoutes />
    </Router>
  );
};

export default App;
