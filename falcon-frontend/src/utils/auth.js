export const getAuthToken = () => {
  return localStorage.getItem("token"); // Reads "token"
};

export const isUserLoggedIn = () => {
  const token = getAuthToken();
  const authenticated = localStorage.getItem("isAuthenticated") === "true";
  return !!token && authenticated;
};

export const loginUser = (token) => {
  if (token) {
    localStorage.setItem("token", token);
    localStorage.setItem("isAuthenticated", "true");
  } else {
    console.error("loginUser called with null or undefined token");
  }
};

export const logoutUser = () => {
  localStorage.removeItem("token"); // Remove token
  localStorage.removeItem("isAuthenticated"); // Remove flag
};
