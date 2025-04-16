// Create and store a persistent avatar color (session-based)
export const getOrCreateAvatarColor = () => {
  const existingColor = sessionStorage.getItem("avatarColor");
  if (existingColor) return existingColor;

  const pastelColors = [
    "#FFB6B6",
    "#FFD6A5",
    "#FDFFB6",
    "#CAFFBF",
    "#9BF6FF",
    "#A0C4FF",
    "#BDB2FF",
  ];
  const randomColor =
    pastelColors[Math.floor(Math.random() * pastelColors.length)];

  sessionStorage.setItem("avatarColor", randomColor);
  return randomColor;
};

// Extract the first uppercase letter from full name
export const getUserInitial = (fullName) => {
  return typeof fullName === "string" && fullName.trim().length > 0
    ? fullName.trim()[0].toUpperCase()
    : "";
};
