// Dummy spam email data for development purposes
const mockSpamEmails = [
  {
    mailId: 101,
    mailAccountId: "acc1",
    sender: "spammer1@spam.com",
    subject: "Win a million dollars!",
    bodySnippet: "Congratulations! You have won...",
    timeReceived: "2024-06-28T12:00:00Z",
    isRead: false,
    isFavorite: false,
    tags: ["spam", "promo"],
  },
  {
    mailId: 102,
    mailAccountId: "acc2",
    sender: "phish@fakebank.com",
    subject: "Your account is locked",
    bodySnippet: "Please click here to unlock your account...",
    timeReceived: "2024-06-27T09:30:00Z",
    isRead: true,
    isFavorite: false,
    tags: ["spam"],
  },
  // Add more dummy emails as needed
];

export default mockSpamEmails;
