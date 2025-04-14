import React, { createContext, useState, useEffect, useContext } from "react";
import { useNavigate } from "react-router-dom";
import { getAuthToken, loginUser, logoutUser } from "../utils/auth";
import { API_BASE_URL } from "../config/constants";

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
  const navigate = useNavigate();

  const [authToken, setAuthToken] = useState(() => getAuthToken());
  const [isAuthenticated, setIsAuthenticated] = useState(
    () => !!getAuthToken()
  );

  const login = (token) => {
    loginUser(token);
    setAuthToken(token);
    setIsAuthenticated(true);
  };

  const logout = () => {
    logoutUser();
    setAuthToken(null);
    setIsAuthenticated(false);
    navigate("/login");
  };

  // Validate token once on app load
  useEffect(() => {
    const token = getAuthToken();
    if (!token) {
      logout(); // just to ensure state is clean
      return;
    }

    const validateToken = async () => {
      try {
        const res = await fetch(`${API_BASE_URL}/api/auth/profile`, {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        });

        if (!res.ok) {
          logout(); // invalid token or expired
        }
      } catch (err) {
        console.error("Token validation failed:", err);
        logout(); // network error fallback
      }
    };

    validateToken();
  }, []); // run once on mount

  const value = {
    authToken,
    isAuthenticated,
    login,
    logout,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = () => useContext(AuthContext);
