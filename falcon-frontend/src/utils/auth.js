const TOKEN_KEY = "token";
const AUTH_FLAG_KEY = "isAuthenticated";
const AI_KEY = "aiKey";

export const getAuthToken = () => {
  return localStorage.getItem(TOKEN_KEY);
};

export const getAiKey = () => {
  return localStorage.getItem(AI_KEY);
};

export const storeAuthToken = (token) => {
  if (token) {
    localStorage.setItem(TOKEN_KEY, token);
  } else {
    localStorage.removeItem(TOKEN_KEY);
  }
};

export const removeAuthToken = () => {
  localStorage.removeItem(TOKEN_KEY);
};

export const storeAiKey = (key) => {
  if (key) {
    localStorage.setItem(AI_KEY, key);
  } else {
    localStorage.removeItem(AI_KEY);
  }
};

export const removeAiKey = () => {
  localStorage.removeItem(AI_KEY);
};

export const handleLoginStorage = (token, aiKey) => {
  storeAuthToken(token);
  storeAiKey(aiKey);
  localStorage.setItem(AUTH_FLAG_KEY, "true");
};

export const handleLogoutStorage = () => {
  removeAuthToken();
  removeAiKey();
  localStorage.removeItem(AUTH_FLAG_KEY);
};

export const isUserLoggedIn = () => {
  return !!getAuthToken();
};
