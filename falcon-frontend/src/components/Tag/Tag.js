export const Tag = ({ name }) => {
  const tagColors = {
    Inbox: "#cbd5ff",
    Social: "#c8facc",
    School: "#f6d6b8",
    Work: "#b8ebf6",
    Personal: "#ffb3c6",
    Finance: "#ffd700",
    Promotions: "#ff9f43",
    Updates: "#6c757d",
    Forums: "#28a745",
    Travel: "#007bff",
  };
  return (
    <span
      className="email-tag"
      style={{ backgroundColor: tagColors[name] || "#ddd" }}
    >
      {name}
    </span>
  );
};
