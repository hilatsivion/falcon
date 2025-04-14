import React, { createContext, useState, useEffect, useContext } from "react";
import { useNavigate } from "react-router-dom";
import { getAuthToken, loginUser, logoutUser } from "../utils/auth";
import { API_BASE_URL } from "../config/constants";

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
  const [authToken, setAuthToken] = useState(getAuthToken());
  const [isAuthenticated, setIsAuthenticated] = useState(!!getAuthToken());
  const navigate = useNavigate(); // ✅ needed to redirect

  const login = (token) => {
    loginUser(token);
    setAuthToken(token);
    setIsAuthenticated(true);
  };

  const logout = () => {
    logoutUser();
    setAuthToken(null);
    setIsAuthenticated(false);
    navigate("/login"); // ✅ redirect on logout
  };

  // ✅ validate token once on app load
  useEffect(() => {
    const validateToken = async () => {
      const token = getAuthToken();
      if (!token) return;

      try {
        const res = await fetch(`${API_BASE_URL}/api/auth/profile`, {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        });

        if (!res.ok) {
          // token invalid or expired
          logout(); // will redirect
        }
      } catch (err) {
        console.error("Token validation failed:", err);
        logout(); // fallback
      }
    };

    validateToken();
  }, []);

  const value = {
    authToken,
    isAuthenticated,
    login,
    logout,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = () => useContext(AuthContext);
