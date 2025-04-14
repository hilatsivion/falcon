export const parseSender = (rawSender) => {
  if (!rawSender) return { name: "", email: "" };

  const match = rawSender.match(/^(.*?)\s*<(.+?)>$/);
  if (match) {
    return { name: match[1].trim(), email: match[2].trim() };
  }

  return { name: "", email: rawSender.trim() };
};
