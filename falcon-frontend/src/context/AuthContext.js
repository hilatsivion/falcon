import React, {
  createContext,
  useState,
  useEffect,
  useContext,
  useCallback,
} from "react";
import { useNavigate } from "react-router-dom";
import {
  getAuthToken,
  getAiKey,
  handleLoginStorage,
  handleLogoutStorage,
} from "../utils/auth";
import { API_BASE_URL } from "../config/constants";
import Loader from "../components/Loader/Loader";

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
  const navigate = useNavigate();

  const [authToken, setAuthToken] = useState(getAuthToken);
  const [aiKey, setAiKey] = useState(getAiKey);
  const [isAuthenticated, setIsAuthenticated] = useState(!!getAuthToken());
  const [isValidating, setIsValidating] = useState(!!getAuthToken()); // Start validating only if a token exists

  const login = useCallback((token, key) => {
    handleLoginStorage(token, key);
    setAuthToken(token);
    setAiKey(key);
    setIsAuthenticated(true);
    setIsValidating(false);
  }, []);

  const logout = useCallback(() => {
    handleLogoutStorage();
    setAuthToken(null);
    setAiKey(null);
    setIsAuthenticated(false);
    setIsValidating(false);
    navigate("/login");
  }, [navigate]);

  useEffect(() => {
    const currentToken = getAuthToken();

    if (!currentToken) {
      setIsAuthenticated(false);
      setAuthToken(null);
      setAiKey(null);
      setIsValidating(false);
      return;
    }

    setIsAuthenticated(true);
    setAuthToken(currentToken);
    setAiKey(getAiKey());
    setIsValidating(true);

    const validateToken = async () => {
      try {
        const res = await fetch(`${API_BASE_URL}/api/auth/profile`, {
          headers: {
            Authorization: `Bearer ${currentToken}`,
          },
        });

        if (!res.ok) {
          console.warn(
            `Token validation failed (Status: ${res.status}). Logging out.`
          );
          logout();
        } else {
          // console.log("Token validated successfully.");
        }
      } catch (err) {
        console.error("Error during token validation fetch:", err);
        logout();
      } finally {
        setIsValidating(false);
      }
    };

    validateToken();
  }, [logout]);

  const value = {
    authToken,
    aiKey,
    isAuthenticated,
    isValidating,
    login,
    logout,
  };

  return (
    <AuthContext.Provider value={value}>
      {isValidating ? (
        <div className="centered-loader">
          <Loader />
        </div>
      ) : (
        children
      )}
    </AuthContext.Provider>
  );
};

export const useAuth = () => useContext(AuthContext);
