# Project Tasks: Backend Integration - Trash, Spam & Analytics Charts

**Generated:** January 15, 2025
**PRD Version:** README_BACKEND.md

---

## Section 1: Foundation & Setup
*Establishes core data models and database schema updates required for spam functionality and analytics endpoints*

- [x] **001**: Add IsSpam Property to Mail Model (Priority: 9, Dependencies: None) - Add IsSpam boolean property to the abstract Mail class to support spam email functionality
- [x] **002**: Create Database Migration for IsSpam Field (Priority: 9, Dependencies: 001) - Generate and apply Entity Framework migration to add IsSpam column to database
- [x] **003**: Update Mail DTOs for Spam Support (Priority: 8, Dependencies: 001) - Add IsSpam property to relevant DTOs (MailReceivedPreviewDto, MailSearchResultDto) for spam email handling

### Task Details

**Task 001**: Add IsSpam Property to Mail Model
**Status:** ✅ COMPLETED on January 15, 2025
**Completion Notes:** Successfully added IsSpam boolean property to abstract Mail class following exact pattern of existing IsFavorite and IsDeleted properties. Code compiles without errors.
**Acceptance Criteria:**
- [x] IsSpam boolean property added to abstract Mail class in Models/Mail.cs
- [x] Property defaults to false (IsSpam = false)
- [x] Property includes appropriate data annotations if needed
- [x] Code compiles successfully without errors
- [x] Property follows existing naming conventions in the model

**Notes:** This is a foundational change that enables all spam functionality. The IsSpam property will be used to filter emails into spam folders and should work alongside the existing IsDeleted property.

---

**Task 002**: Create Database Migration for IsSpam Field
**Status:** ✅ COMPLETED on January 15, 2025
**Completion Notes:** Successfully generated and applied Entity Framework migration `20250703142810_AddIsSpamToMail` that adds IsSpam boolean column to Mails table with default value false. Database schema updated without data loss.
**Acceptance Criteria:**
- [x] Entity Framework migration generated using Add-Migration command
- [x] Migration script properly adds IsSpam column to database
- [x] Migration applies successfully to development database
- [x] IsSpam column defaults to false (0) for existing records
- [x] Database schema updated without data loss

**Notes:** Use `dotnet ef migrations add AddIsSpamToMail` command. Ensure migration is tested before applying to production environments.

---

**Task 003**: Update Mail DTOs for Spam Support
**Status:** ✅ COMPLETED on January 15, 2025
**Completion Notes:** Successfully added IsSpam boolean property to both MailReceivedPreviewDto (line 110) and MailSearchResultDto (line 174). Properties follow exact pattern of existing IsFavorite properties and maintain backward compatibility.
**Acceptance Criteria:**
- [x] MailReceivedPreviewDto includes IsSpam property
- [x] MailSearchResultDto includes IsSpam property if applicable
- [x] All DTOs maintain consistent property naming
- [x] Mapping logic updated to handle IsSpam property
- [x] API response structure remains backwards compatible

**Notes:** Focus on DTOs used by frontend for email previews and search results. Consider whether IsSpam should be exposed in all contexts or only specific ones.

---

## Section 2: Core Development
*Implements the main spam functionality, analytics endpoints, and core business logic*

- [x] **004**: Implement Spam Email Service Methods (Priority: 8, Dependencies: 001, 002, 003) - Add GetSpamEmailPreviewsAsync method to MailService for retrieving spam emails
- [x] **005**: Create Spam Endpoint in MailController (Priority: 8, Dependencies: 004) - Add GET /api/mail/spam/preview endpoint to MailController with authentication and pagination
- [ ] **006**: Implement Spam Toggle Functionality (Priority: 7, Dependencies: 004) - Add methods to mark emails as spam/not spam and handle favorite status removal
- [ ] **007**: Implement Email Category Breakdown Analytics Endpoint (Priority: 6, Dependencies: None) - Create GET /api/analytics/email-category-breakdown endpoint for monthly email categories chart
- [ ] **008**: Implement Emails by Time of Day Analytics Endpoint (Priority: 6, Dependencies: None) - Create GET /api/analytics/emails-by-time-of-day endpoint for time-based email distribution chart
- [ ] **009**: Implement Top Senders Analytics Endpoint (Priority: 6, Dependencies: None) - Create GET /api/analytics/top-senders endpoint for top senders in last 7 days chart

### Task Details

**Task 004**: Implement Spam Email Service Methods
**Status:** ✅ COMPLETED on January 15, 2025
**Completion Notes:** Successfully implemented GetSpamEmailPreviewsAsync method in MailService following exact pattern of GetTrashEmailPreviewsAsync. Method filters by IsSpam=true AND IsDeleted=false, includes proper pagination, user authentication, and returns MailReceivedPreviewDto with IsSpam property. Also updated all existing DTO mappings to include IsSpam property for consistency.
**Acceptance Criteria:**
- [x] GetSpamEmailPreviewsAsync method added to MailService
- [x] Method filters emails where IsSpam = true and IsDeleted = false
- [x] Method includes pagination support (page, pageSize parameters)
- [x] Method returns List<MailReceivedPreviewDto> with proper mapping
- [x] Method handles user email authentication properly

**Notes:** Follow the existing pattern used in GetTrashEmailPreviewsAsync. Ensure spam emails are only retrieved for the authenticated user and exclude deleted emails.

---

**Task 005**: Create Spam Endpoint in MailController
**Status:** ✅ COMPLETED on January 15, 2025
**Completion Notes:** Successfully implemented GET /api/mail/spam/preview endpoint in MailController following exact pattern of GetTrashPreviews endpoint. Endpoint includes [Authorize] attribute, proper pagination parameters, calls GetSpamEmailPreviewsAsync service method, and maintains consistent error handling with console logging.
**Acceptance Criteria:**
- [x] GET /api/mail/spam/preview endpoint added to MailController
- [x] Endpoint requires [Authorize] attribute for authentication
- [x] Endpoint supports page and pageSize query parameters with defaults (page=1, pageSize=100)
- [x] Endpoint returns 200 OK with spam emails or appropriate error codes
- [x] Endpoint follows existing error handling patterns

**Notes:** Follow the exact pattern used by the existing trash endpoint. Ensure consistent error handling and response format.

---

**Task 006**: Implement Spam Toggle Functionality
**Acceptance Criteria:**
- [ ] Method to mark email as spam (sets IsSpam = true, IsFavorite = false)
- [ ] Method to unmark email as spam (sets IsSpam = false)
- [ ] Endpoint added to MailController for spam toggling
- [ ] Analytics service updated to track spam email counts
- [ ] Bulk operations supported for multiple emails

**Notes:** When marking as spam, automatically remove favorite status as per PRD requirements. Consider adding this to existing toggle endpoints or creating dedicated spam toggle endpoints.

---

**Task 007**: Implement Email Category Breakdown Analytics Endpoint
**Acceptance Criteria:**
- [ ] GET /api/analytics/email-category-breakdown endpoint added to AnalyticsController
- [ ] Endpoint returns array of objects with 'name' and 'value' fields
- [ ] Values represent percentage breakdown for current month
- [ ] Categories based on email tags or predefined system categories
- [ ] Endpoint requires authentication and filters by user

**Notes:** Consider using existing tag system to categorize emails. If no tags exist, may need to implement basic categorization logic based on sender domains or subjects.

---

**Task 008**: Implement Emails by Time of Day Analytics Endpoint
**Acceptance Criteria:**
- [ ] GET /api/analytics/emails-by-time-of-day endpoint added to AnalyticsController
- [ ] Endpoint returns array of objects with 'range' and 'avg' fields
- [ ] Time ranges follow 6-hour blocks (00–06, 06–09, 09–12, 12–15, 15–18, 18–24)
- [ ] Averages calculated for current week
- [ ] Endpoint requires authentication and filters by user

**Notes:** Use TimeReceived field from MailReceived table. Group emails by hour and calculate averages for the specified time ranges.

---

**Task 009**: Implement Top Senders Analytics Endpoint
**Acceptance Criteria:**
- [ ] GET /api/analytics/top-senders endpoint added to AnalyticsController
- [ ] Endpoint returns array of up to 5 objects with 'sender' and 'count' fields
- [ ] Counts based on emails received in last 7 days
- [ ] Sender field is always a string (email address or placeholder)
- [ ] Results ordered by count (highest first)

**Notes:** Handle cases where sender information is missing or malformed by using empty string or placeholder. Focus on received emails only, not sent emails.

---

## Section 3: Integration & Testing
*Handles frontend integration, removes mock data, and ensures system quality*

- [ ] **010**: Update MailService Delete Methods for Favorite Status (Priority: 7, Dependencies: None) - Ensure deleted emails have IsFavorite set to false when moved to trash
- [ ] **011**: Verify Trash Endpoint Functionality (Priority: 5, Dependencies: 010) - Test existing trash endpoint to ensure it works correctly with authentication and returns proper data
- [ ] **012**: Identify and Document Frontend Mock Data Locations (Priority: 4, Dependencies: None) - Locate all mock data blocks in frontend code that need to be removed
- [ ] **013**: Test Spam Endpoint Integration (Priority: 5, Dependencies: 005) - Perform end-to-end testing of spam endpoint with authentication and data validation
- [ ] **014**: Test Analytics Endpoints Integration (Priority: 5, Dependencies: 007, 008, 009) - Verify all analytics chart endpoints return correct data format and handle edge cases
- [ ] **015**: Validate Authentication and Authorization (Priority: 6, Dependencies: 005, 007, 008, 009) - Ensure all new endpoints properly authenticate users and return only user-specific data

### Task Details

**Task 010**: Update MailService Delete Methods for Favorite Status
**Acceptance Criteria:**
- [ ] DeleteMails method sets IsFavorite = false when IsDeleted = true
- [ ] Existing trash functionality maintains this behavior
- [ ] Bulk delete operations handle favorite status correctly
- [ ] Database updates are atomic (transaction-based)
- [ ] Method preserves other email properties

**Notes:** This may already be implemented but needs verification. Check the existing delete methods in MailService to ensure they follow PRD requirements.

---

**Task 011**: Verify Trash Endpoint Functionality
**Acceptance Criteria:**
- [ ] GET /api/mail/trash/preview returns only deleted emails (IsDeleted = true)
- [ ] Endpoint respects pagination parameters correctly
- [ ] Response format matches MailReceivedPreviewDto structure
- [ ] Authentication works correctly for endpoint
- [ ] Deleted emails do not appear as favorites

**Notes:** Since trash endpoint already exists, this is primarily verification and testing. Document any issues found for immediate resolution.

---

**Task 012**: Identify and Document Frontend Mock Data Locations
**Acceptance Criteria:**
- [ ] Located mock data block for trash page in GenericEmailPage.js
- [ ] Located mock data block for spam page in GenericEmailPage.js
- [ ] Identified dummy data in MonthlyEmailCategoriesCard.js
- [ ] Identified dummy data in EmailsByTimeOfDayCard.js
- [ ] Identified dummy data in TopSendersCard.js

**Notes:** This is a documentation task to prepare for frontend integration cleanup. Create a checklist of all locations where mock data needs to be removed.

---

**Task 013**: Test Spam Endpoint Integration
**Acceptance Criteria:**
- [ ] Endpoint returns 200 OK with valid spam emails
- [ ] Pagination works correctly with page and pageSize parameters
- [ ] Authentication rejects unauthorized requests with 401
- [ ] Response format matches expected MailReceivedPreviewDto structure
- [ ] No deleted emails appear in spam results

**Notes:** Use Postman or similar tool for API testing. Test edge cases like empty results, invalid authentication, and boundary pagination values.

---

**Task 014**: Test Analytics Endpoints Integration
**Acceptance Criteria:**
- [ ] Email category breakdown returns valid percentage data that sums to ~100%
- [ ] Time of day analytics returns data for all 6 time ranges
- [ ] Top senders returns maximum 5 results ordered by count
- [ ] All endpoints handle users with no data gracefully
- [ ] Response formats match exact specifications in PRD

**Notes:** Test with users who have varying amounts of email data. Ensure endpoints work correctly even with no emails or limited data sets.

---

**Task 015**: Validate Authentication and Authorization
**Acceptance Criteria:**
- [ ] All new endpoints require valid JWT tokens
- [ ] Endpoints return only data belonging to authenticated user
- [ ] Invalid or expired tokens result in 401 Unauthorized
- [ ] Missing authorization headers result in appropriate error responses
- [ ] User isolation is properly enforced (no data leakage between users)

**Notes:** Critical security verification task. Test with multiple user accounts to ensure data isolation. Use invalid/expired tokens to verify error handling.

---

## Section 4: Enhancement & Polish
*Covers documentation, performance optimization, and final cleanup tasks*

- [ ] **016**: Add API Documentation for New Endpoints (Priority: 3, Dependencies: 005, 007, 008, 009) - Document new spam and analytics endpoints with OpenAPI/Swagger specifications
- [ ] **017**: Optimize Analytics Query Performance (Priority: 4, Dependencies: 007, 008, 009) - Review and optimize database queries for analytics endpoints to ensure good performance
- [ ] **018**: Add Error Handling and Logging for New Endpoints (Priority: 5, Dependencies: 005, 007, 008, 009) - Implement comprehensive error handling and logging for all new endpoints
- [ ] **019**: Create Unit Tests for New Service Methods (Priority: 4, Dependencies: 004, 007, 008, 009) - Add unit tests for spam service methods and analytics service methods
- [ ] **020**: Perform Load Testing on New Endpoints (Priority: 2, Dependencies: 013, 014, 015) - Test new endpoints under load to ensure they meet performance requirements

### Task Details

**Task 016**: Add API Documentation for New Endpoints
**Acceptance Criteria:**
- [ ] Swagger/OpenAPI documentation added for spam endpoint
- [ ] Documentation added for all three analytics chart endpoints
- [ ] Request/response schemas properly documented
- [ ] Authentication requirements clearly specified
- [ ] Example responses provided for each endpoint

**Notes:** Follow existing documentation patterns in the codebase. Ensure documentation is accessible through the standard API documentation interface.

---

**Task 017**: Optimize Analytics Query Performance
**Acceptance Criteria:**
- [ ] Database queries use appropriate indexes for time-based filtering
- [ ] Analytics queries avoid N+1 query problems
- [ ] Query execution time is under 1 second for typical data volumes
- [ ] Memory usage is reasonable for large datasets
- [ ] Pagination implemented where appropriate

**Notes:** Use database query profiling tools to identify performance bottlenecks. Consider adding database indexes for commonly queried fields like TimeReceived and sender fields.

---

**Task 018**: Add Error Handling and Logging for New Endpoints
**Acceptance Criteria:**
- [ ] All new endpoints have try-catch blocks with appropriate error responses
- [ ] Errors are logged with sufficient detail for debugging
- [ ] User-friendly error messages returned to frontend
- [ ] Different error types return appropriate HTTP status codes
- [ ] Logging follows existing patterns in the codebase

**Notes:** Follow the error handling patterns used in existing endpoints. Ensure sensitive information is not exposed in error messages.

---

**Task 019**: Create Unit Tests for New Service Methods
**Acceptance Criteria:**
- [ ] Unit tests created for GetSpamEmailPreviewsAsync method
- [ ] Unit tests created for spam toggle functionality
- [ ] Unit tests created for all analytics service methods
- [ ] Tests cover happy path, edge cases, and error conditions
- [ ] Test coverage is at least 80% for new methods

**Notes:** Use existing test patterns in the codebase. Mock dependencies appropriately and test business logic thoroughly.

---

**Task 020**: Perform Load Testing on New Endpoints
**Acceptance Criteria:**
- [ ] Load testing performed with 100+ concurrent users
- [ ] Response times remain acceptable under load
- [ ] No memory leaks or resource exhaustion under sustained load
- [ ] Database connection pooling works correctly
- [ ] System gracefully handles peak traffic scenarios

**Notes:** Use tools like Apache JMeter or similar for load testing. Focus on analytics endpoints which may be more resource-intensive due to aggregation queries. 