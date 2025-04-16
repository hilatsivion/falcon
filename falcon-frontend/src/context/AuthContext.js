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
  // CHANGE: Added state to track validation process
  const [isValidating, setIsValidating] = useState(true);

  const login = (token) => {
    loginUser(token);
    setAuthToken(token);
    setIsAuthenticated(true);
    setIsValidating(false); // CHANGE: Update validation state on login
  };

  const logout = () => {
    logoutUser();
    setAuthToken(null);
    setIsAuthenticated(false);
    setIsValidating(false); // CHANGE: Update validation state on logout
    navigate("/login");
  };

  useEffect(() => {
    const token = getAuthToken();
    if (!token) {
      setIsAuthenticated(false);
      setIsValidating(false); // CHANGE: Update validation state if no token
      return;
    }

    setIsValidating(true);
    setIsAuthenticated(true);

    const validateToken = async () => {
      try {
        const res = await fetch(`${API_BASE_URL}/api/auth/profile`, {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        });

        if (!res.ok) {
          logoutUser();
          setAuthToken(null);
          setIsAuthenticated(false);
        } else {
          setAuthToken(token);
          setIsAuthenticated(true);
        }
      } catch (err) {
        logoutUser();
        setAuthToken(null);
        setIsAuthenticated(false);
      } finally {
        setIsValidating(false);
      }
    };

    validateToken();
  }, []);

  const value = {
    authToken,
    isAuthenticated,
    isValidating,
    login,
    logout,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = () => useContext(AuthContext);
