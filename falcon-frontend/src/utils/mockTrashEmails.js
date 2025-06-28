// Dummy trash email data for development purposes
const mockTrashEmails = [
  {
    mailId: 1,
    mailAccountId: "acc1",
    sender: "deleteduser1@example.com",
    subject: "Deleted: Welcome to Falcon!",
    bodySnippet: "This is a deleted welcome email...",
    timeReceived: "2024-06-28T10:00:00Z",
    isRead: false,
    isFavorite: false,
    tags: ["welcome", "info"],
  },
  {
    mailId: 2,
    mailAccountId: "acc2",
    sender: "deleteduser2@example.com",
    subject: "Deleted: Your Invoice",
    bodySnippet: "This is a deleted invoice email...",
    timeReceived: "2024-06-27T15:30:00Z",
    isRead: true,
    isFavorite: true,
    tags: ["invoice"],
  },
  // Add more dummy emails as needed
];

export default mockTrashEmails;
