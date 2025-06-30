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

## Spam Page Integration

- The Spam page in the frontend currently uses mock data from `src/utils/mockSpamEmails.js`.
- Backend should implement:
  - `GET /api/mail/spam/preview?page=1&pageSize=100` (returns only spam emails for the authenticated user, same structure as MailReceivedPreviewDto)
  - When an email is marked as spam, its favorite status should be removed (`IsFavorite = false`).
- In `src/pages/Main/Inbox/GenericEmailPage.js`, remove the mock block for `/spam` when backend is ready.

## Monthly Email Categories Chart Integration

- The donut chart for Monthly Email Categories in the analytics page currently uses dummy data.
- Backend should implement:
  - `GET /api/analytics/email-category-breakdown` (returns an array of objects with `name` and `value` fields, where value is the percentage for each category for the month)
  - Example response:
    ```json
    [
      { "name": "Work", "value": 10.33 },
      { "name": "School", "value": 4.19 },
      { "name": "Finance", "value": 25.33 },
      { "name": "Marketing", "value": 10.33 },
      { "name": "Design", "value": 4.19 }
    ]
    ```
- In `src/components/InsightCard/MonthlyEmailCategoriesCard.js`, replace the dummy data with a fetch to this endpoint and update the chart accordingly.

## Emails Received by Time of Day Chart Integration

- The bar chart for "Emails Received by Time of Day" in the analytics page currently uses dummy data.
- Backend should implement:
  - `GET /api/analytics/emails-by-time-of-day` (returns an array of objects with `range` and `avg` fields, where `range` is a string like "09–12" and `avg` is the average number of emails received in that range for the current week)
  - Example response:
    ```json
    [
      { "range": "00–06", "avg": 2 },
      { "range": "06–09", "avg": 5 },
      { "range": "09–12", "avg": 12 },
      { "range": "12–15", "avg": 8 },
      { "range": "15–18", "avg": 6 },
      { "range": "18–24", "avg": 3 }
    ]
    ```
- In `src/components/InsightCard/EmailsByTimeOfDayCard.js`, replace the dummy data with a fetch to this endpoint and update the chart accordingly.

## Top Senders (Last 7 Days) Chart Integration

- The bar chart for "Top Senders (Last 7 Days)" in the analytics page currently uses dummy data.
- Backend should implement:
  - `GET /api/analytics/top-senders` (returns an array of up to 5 objects with `sender` and `count` fields, where `sender` is the sender's name/email and `count` is the number of emails received from that sender in the last 7 days)
  - Ensure that the `sender` value is always a string (preferably an email address). If the sender is missing or malformed, return an empty string or a placeholder.
  - Example response:
    ```json
    [
      { "sender": "alice.averylongemailaddress@example.com", "count": 18 },
      { "sender": "bob@example.com", "count": 14 },
      { "sender": "carol@example.com", "count": 10 },
      { "sender": "dave@example.com", "count": 7 },
      { "sender": "eve@example.com", "count": 5 }
    ]
    ```
- In `src/components/InsightCard/TopSendersCard.js`, replace the dummy data with a fetch to this endpoint and update the chart accordingly.
