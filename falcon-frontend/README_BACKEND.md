# Backend Integration Guide: Trash Page

## Current State

- The Trash page in the frontend currently uses mock data from `src/utils/mockTrashEmails.js`.
- This is a temporary solution to allow UI development before the backend is ready.

## What Needs to Be Done

### 1. Implement the Backend Endpoint

- Create the following endpoint in the backend:
  - `GET /api/mail/trash/preview?page=1&pageSize=100`
- The endpoint should return an array of deleted (soft-deleted) emails for the authenticated user, matching the structure of `MailReceivedPreviewDto`:

```
{
  mailId: number,
  mailAccountId: string,
  sender: string,
  subject: string,
  bodySnippet: string,
  timeReceived: string (ISO date),
  isRead: boolean,
  isFavorite: boolean,
  tags: string[]
}
```

### 2. Remove the Mock Data Block

- In `src/pages/Main/Inbox/GenericEmailPage.js`, find the following code in the `fetchData` function:

```
if (pathname === "/trash") {
  // --- MOCK DATA FOR TRASH PAGE ---
  // TODO: Remove this block when backend is ready. See README_BACKEND.md for details.
  setEmails(mockTrashEmails.map((dto) => mapDtoToEmailItemProps(dto, "MailReceivedPreviewDto")));
  setIsLoading(false);
  setError(null);
  return;
}
```

- **Delete this block** and let the code fall through to the real API call logic below it.

### 3. Test the Integration

- After the backend endpoint is implemented and the mock block is removed, the Trash page will automatically fetch real data from the backend.

---

## Additional Notes

- Make sure the backend returns only emails that are soft-deleted (e.g., `IsDeleted = true`).
- The endpoint should require authentication and return only emails belonging to the logged-in user.
- If you change the data structure, update the frontend mapping logic accordingly.

---

- **When a mail is deleted (moved to trash), its favorite status should be removed.**

  - The backend should set `IsFavorite = false` for any mail that is soft-deleted (moved to trash).
  - This ensures that deleted emails do not appear as favorites in any view.

- If you change the data structure, update the frontend mapping logic accordingly.

---
