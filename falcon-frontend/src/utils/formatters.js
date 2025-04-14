export const formatEmailTime = (isoDateTimeString) => {
  if (!isoDateTimeString) return "";

  const now = new Date();
  const emailDate = new Date(isoDateTimeString);

  if (isNaN(emailDate.getTime())) return "";

  const isToday = emailDate.toDateString() === now.toDateString();

  if (isToday) {
    // Format as HH:mm (24-hour)
    return emailDate.toLocaleTimeString(navigator.language, {
      hour: "2-digit",
      minute: "2-digit",
      hour12: false,
    });
  } else {
    // Format as: Apr 14
    return emailDate.toLocaleDateString(navigator.language, {
      month: "short",
      day: "numeric",
    });
  }
};
