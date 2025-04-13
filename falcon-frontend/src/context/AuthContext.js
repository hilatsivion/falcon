import React, { createContext, useState, useEffect, useContext } from "react";
import { getAuthToken, loginUser, logoutUser } from "../utils/auth";

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
  const [authToken, setAuthToken] = useState(getAuthToken());
  const [isAuthenticated, setIsAuthenticated] = useState(!!getAuthToken());

  const login = (token) => {
    loginUser(token);
    setAuthToken(token);
    setIsAuthenticated(true);
  };

  const logout = () => {
    logoutUser();
    setAuthToken(null);
    setIsAuthenticated(false);
  };

  const value = {
    authToken,
    isAuthenticated,
    login,
    logout,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = () => {
  return useContext(AuthContext);
};
