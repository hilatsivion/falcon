export const Tag = ({ name }) => {
  const tagColors = {
    "Social network": "#D2F9E5", // mint green
    School: "#FFE9C6", // warm pastel peach
    Work: "#D1F0FF", // cool sky blue
    Personal: "#FFD6E0", // soft pink
    Finance: "#FFF6B2", // light gold
    Discounts: "#FFDEAD", // light apricot
    News: "#FFEBCC", // pale orange (attention-grabbing but soft)
    Health: "#E0FFE0", // pale mint (fresh, clean)
    "Family & friends": "#F9D6FF", // lavender pink (warm & social)
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
