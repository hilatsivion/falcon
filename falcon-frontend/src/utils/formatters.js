export const formatEmailTime = (isoDateTimeString) => {
  if (!isoDateTimeString) return "";
  const now = new Date();
  const emailDate = new Date(isoDateTimeString);

  if (isNaN(emailDate.getTime())) return "";

  const startOfToday = new Date(
    now.getFullYear(),
    now.getMonth(),
    now.getDate()
  );

  if (emailDate >= startOfToday) {
    return emailDate.toLocaleTimeString(navigator.language, {
      hour: "2-digit",
      minute: "2-digit",
      hour12: false,
    });
  } else {
    return emailDate.toLocaleDateString(navigator.language, {
      day: "numeric",
      month: "short",
    });
  }
};
