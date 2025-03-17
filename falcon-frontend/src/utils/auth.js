export const getAuthToken = () => {
  return localStorage.getItem("authToken"); // Retrieve token from storage
};

export const isUserLoggedIn = () => {
  const token = getAuthToken();
  return !!token; // Returns true if token exists, otherwise false
};

export const loginUser = (token) => {
  localStorage.setItem("authToken", token);
};

export const logoutUser = () => {
  localStorage.removeItem("authToken");
};
