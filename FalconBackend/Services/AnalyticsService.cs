using FalconBackend.Data;
using FalconBackend.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace FalconBackend.Services
{
    public class AnalyticsService
    {
        private readonly AppDbContext _context;

        public AnalyticsService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Creates an initial analytics record for a new user
        /// </summary>
        public async Task CreateAnalyticsForUserAsync(string userEmail)
        {
            var existingAnalytics = await _context.Analytics.AsNoTracking().FirstOrDefaultAsync(a => a.AppUserEmail == userEmail);
            if (existingAnalytics != null)
            {
                Console.WriteLine($"Analytics record already exists for user {userEmail}. Skipping creation.");
                return; 
            }

            DateTime now = DateTime.UtcNow;
            DateTime today = now.Date;
            DateTime startOfWeek = today.AddDays(-(int)today.DayOfWeek);

            var analytics = new Analytics
            {
                AppUserEmail = userEmail,
                LastDailyReset = today,      
                LastWeeklyReset = startOfWeek, 
                LastUpdated = now
            };

            _context.Analytics.Add(analytics);
            await _context.SaveChangesAsync();
            Console.WriteLine($"Initial analytics created for user {userEmail}.");
        }

        /// <summary>
        /// Retrieves analytics data for a user, ensuring stats are checked/reset first
        /// </summary>
        public async Task<Analytics> GetAnalyticsForUserAsync(string userEmail)
        {
            await CheckAndResetStatsOnLoginAsync(userEmail);

            var analytics = await _context.Analytics.FirstOrDefaultAsync(a => a.AppUserEmail == userEmail);

            if (analytics == null)
            {
                Console.WriteLine($"Analytics data not found for user {userEmail}. Creating new record.");
                await CreateAnalyticsForUserAsync(userEmail);
                analytics = await _context.Analytics.FirstOrDefaultAsync(a => a.AppUserEmail == userEmail);
            }
            
            return analytics;
        }

        /// <summary>
        /// Central method to check for and apply daily/weekly resets
        /// </summary>
        public async Task<bool> CheckAndResetStatsOnLoginAsync(string userEmail)
        {
            var analytics = await _context.Analytics.FirstOrDefaultAsync(a => a.AppUserEmail == userEmail);
            if (analytics == null)
            {
                Console.WriteLine($"Analytics not found for {userEmail}. Creating new record.");
                await CreateAnalyticsForUserAsync(userEmail);
                analytics = await _context.Analytics.FirstOrDefaultAsync(a => a.AppUserEmail == userEmail);
                if (analytics == null) return false;
            }

            DateTime now = DateTime.UtcNow;
            DateTime today = now.Date;
            bool requiresSave = false;
            bool dailyResetPerformed = false;

            // --- Daily Reset Logic ---
            if (analytics.LastDailyReset < today)
            {
                Console.WriteLine($"Performing daily reset for {analytics.AppUserEmail}");
                dailyResetPerformed = true;

                // Store previous day's value
                analytics.TimeSpentYesterday = analytics.TimeSpentToday;

                if (analytics.IsActiveToday)
                {
                    // Update totals and averages
                    analytics.TotalTimeSpent += analytics.TimeSpentToday;
                    analytics.TotalDaysTracked += 1;
                    analytics.AvgTimeSpentDaily = analytics.TotalTimeSpent / Math.Max(1, analytics.TotalDaysTracked);

                    // Update Streak
                    if (analytics.LastDailyReset == today.AddDays(-1))
                    {
                        analytics.CurrentStreak++;
                    }
                    else
                    {
                        analytics.CurrentStreak = 1;
                    }
                    analytics.LongestStreak = Math.Max(analytics.LongestStreak, analytics.CurrentStreak);
                }
                else
                {
                    // Break streak if not active
                    analytics.CurrentStreak = 0;
                }

                // Reset daily counters for the NEW day
                analytics.TimeSpentToday = 0;
                analytics.IsActiveToday = false;
                analytics.LastDailyReset = today;
                requiresSave = true;
            }

            // --- Weekly Reset Logic ---
            DateTime startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            if (analytics.LastWeeklyReset < startOfWeek)
            {
                Console.WriteLine($"Performing weekly reset for {analytics.AppUserEmail}");

                // Calculate and store averages before reset
                if (analytics.TimeSpentThisWeek > 0 || analytics.EmailsReceivedWeekly > 0 || analytics.EmailsSentWeekly > 0)
                {
                    analytics.AvgTimeSpentWeekly = analytics.TimeSpentThisWeek / 7.0f;
                    analytics.AvgEmailsPerWeek = (float)(analytics.EmailsReceivedWeekly + analytics.EmailsSentWeekly) / 7.0f;
                }

                // Store previous week's values
                analytics.TimeSpentLastWeek = analytics.TimeSpentThisWeek;
                analytics.EmailsReceivedLastWeek = analytics.EmailsReceivedWeekly;
                analytics.EmailsSentLastWeek = analytics.EmailsSentWeekly;
                analytics.SpamEmailsLastWeek = analytics.SpamEmailsWeekly;
                analytics.ReadEmailsLastWeek = analytics.ReadEmailsWeekly;
                analytics.DeletedEmailsLastWeek = analytics.DeletedEmailsWeekly;

                // Reset weekly counters for the NEW week
                analytics.TimeSpentThisWeek = 0;
                analytics.EmailsReceivedWeekly = 0;
                analytics.EmailsSentWeekly = 0;
                analytics.SpamEmailsWeekly = 0;
                analytics.ReadEmailsWeekly = 0;
                analytics.DeletedEmailsWeekly = 0;
                analytics.LastWeeklyReset = startOfWeek;
                requiresSave = true;
            }

            if (requiresSave)
            {
                await SaveAnalyticsAsync(analytics);
            }

            return dailyResetPerformed;
        }

        /// <summary>
        /// HEARTBEAT: Updates time spent in app (called on user activity/heartbeat)
        /// This should be called regularly while user is active in the app
        /// </summary>
        public async Task UpdateTimeSpentAsync(string userEmail)
        {
            DateTime currentTime = DateTime.UtcNow;
            DateTime today = currentTime.Date;

            // Check for resets first
            bool dailyResetJustPerformed = await CheckAndResetStatsOnLoginAsync(userEmail);

            var analytics = await _context.Analytics.FirstOrDefaultAsync(a => a.AppUserEmail == userEmail);
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null || analytics == null)
            {
                Console.WriteLine($"Cannot update time spent for {userEmail}. User or Analytics missing.");
                return;
            }

            // Initialize LastLogin if it's the first time
            if (!user.LastLogin.HasValue)
            {
                user.LastLogin = currentTime;
                analytics.IsActiveToday = true;
                await _context.SaveChangesAsync();
                Console.WriteLine($"Initialized LastLogin for {userEmail}.");
                return;
            }

            DateTime lastSeenTime = user.LastLogin.Value;
            DateTime effectiveStartTime = lastSeenTime;

            // If daily reset happened and last seen was before today, start calculating from today
            if (dailyResetJustPerformed && lastSeenTime < today)
            {
                effectiveStartTime = today;
                Console.WriteLine($"Daily reset occurred. Calculating time for {userEmail} from start of today.");
            }

            // Calculate session duration (max 30 minutes to prevent runaway sessions)
            double sessionDurationMinutes = Math.Min((currentTime - effectiveStartTime).TotalMinutes, 30);

            if (sessionDurationMinutes > 0 && sessionDurationMinutes <= 30)
            {
                analytics.TimeSpentToday += (float)sessionDurationMinutes;
                analytics.TimeSpentThisWeek += (float)sessionDurationMinutes;
                analytics.IsActiveToday = true;
                
                Console.WriteLine($"Added {sessionDurationMinutes:F2} minutes for {userEmail}. Total today: {analytics.TimeSpentToday:F2}");
            }

            // Update LastLogin to current time for next heartbeat calculation
            user.LastLogin = currentTime;
            analytics.LastUpdated = currentTime;

            _context.AppUsers.Update(user);
            _context.Analytics.Update(analytics);
            await _context.SaveChangesAsync();
        }

        // ===========================================
        // TIME-RELEVANT FUNCTIONS (Current Actions)
        // ===========================================
        // These should only be called for actions happening TODAY

        /// <summary>
        /// Call when user RECEIVES an email TODAY (not for old synced emails)
        /// </summary>
        public async Task OnEmailReceivedTodayAsync(string userEmail)
        {
            await CheckAndResetStatsOnLoginAsync(userEmail);
            var analytics = await _context.Analytics.FirstOrDefaultAsync(a => a.AppUserEmail == userEmail);
            if (analytics == null) return;

            analytics.EmailsReceivedWeekly++;
            analytics.IsActiveToday = true;
            await SaveAnalyticsAsync(analytics);
            Console.WriteLine($"Email received today for {userEmail}. Weekly count: {analytics.EmailsReceivedWeekly}");
        }

        /// <summary>
        /// Call when user SENDS an email TODAY (not for old synced emails)
        /// </summary>
        public async Task OnEmailSentTodayAsync(string userEmail)
        {
            await CheckAndResetStatsOnLoginAsync(userEmail);
            var analytics = await _context.Analytics.FirstOrDefaultAsync(a => a.AppUserEmail == userEmail);
            if (analytics == null) return;

            analytics.EmailsSentWeekly++;
            analytics.IsActiveToday = true;
            await SaveAnalyticsAsync(analytics);
            Console.WriteLine($"Email sent today for {userEmail}. Weekly count: {analytics.EmailsSentWeekly}");
        }

        /// <summary>
        /// Call when user READS an email TODAY
        /// </summary>
        public async Task OnEmailReadTodayAsync(string userEmail)
        {
            await CheckAndResetStatsOnLoginAsync(userEmail);
            var analytics = await _context.Analytics.FirstOrDefaultAsync(a => a.AppUserEmail == userEmail);
            if (analytics == null) return;

            analytics.ReadEmailsWeekly++;
            analytics.IsActiveToday = true;
            await SaveAnalyticsAsync(analytics);
        }

        /// <summary>
        /// Call when user DELETES an email TODAY
        /// </summary>
        public async Task OnEmailDeletedTodayAsync(string userEmail)
        {
            await CheckAndResetStatsOnLoginAsync(userEmail);
            var analytics = await _context.Analytics.FirstOrDefaultAsync(a => a.AppUserEmail == userEmail);
            if (analytics == null) return;

            analytics.DeletedEmailsWeekly++;
            analytics.IsActiveToday = true;
            await SaveAnalyticsAsync(analytics);
        }

        /// <summary>
        /// Call when user MARKS email as spam TODAY
        /// </summary>
        public async Task OnEmailMarkedSpamTodayAsync(string userEmail)
        {
            await CheckAndResetStatsOnLoginAsync(userEmail);
            var analytics = await _context.Analytics.FirstOrDefaultAsync(a => a.AppUserEmail == userEmail);
            if (analytics == null) return;

            analytics.SpamEmailsWeekly++;
            analytics.IsActiveToday = true;
            await SaveAnalyticsAsync(analytics);
        }

        // ===========================================
        // HISTORICAL DATA FUNCTIONS (Email Sync)
        // ===========================================

        /// <summary>
        /// Call when syncing historical emails (for building historical averages)
        /// This processes emails by their actual date, not current date
        /// </summary>
        public async Task ProcessHistoricalEmailsAsync(string userEmail, List<DateTime> receivedDates, List<DateTime> sentDates)
        {
            var analytics = await _context.Analytics.FirstOrDefaultAsync(a => a.AppUserEmail == userEmail);
            if (analytics == null)
            {
                await CreateAnalyticsForUserAsync(userEmail);
                analytics = await _context.Analytics.FirstOrDefaultAsync(a => a.AppUserEmail == userEmail);
            }

            DateTime now = DateTime.UtcNow;
            DateTime today = now.Date;
            DateTime startOfWeek = today.AddDays(-(int)today.DayOfWeek);

            // Validate that all timestamps are in UTC (or at least reasonable)
            // Log any suspicious timestamps for debugging
            foreach (var date in receivedDates.Concat(sentDates))
            {
                if (date.Kind != DateTimeKind.Utc && date.Kind != DateTimeKind.Unspecified)
                {
                    Console.WriteLine($"WARNING: Email timestamp not in UTC for {userEmail}: {date} (Kind: {date.Kind})");
                }
                
                // Check for obviously wrong dates (more than 10 years in the past or future)
                if (date < DateTime.UtcNow.AddYears(-10) || date > DateTime.UtcNow.AddYears(1))
                {
                    Console.WriteLine($"WARNING: Suspicious email timestamp for {userEmail}: {date}");
                }
            }

            // Count emails that arrived THIS WEEK (should count toward current week)
            // All comparisons now in UTC
            var receivedThisWeek = receivedDates.Count(date => date.Date >= startOfWeek && date.Date <= today);
            var sentThisWeek = sentDates.Count(date => date.Date >= startOfWeek && date.Date <= today);

            // Add to current week stats
            analytics.EmailsReceivedWeekly += receivedThisWeek;
            analytics.EmailsSentWeekly += sentThisWeek;

            // For historical data older than this week, we could update historical averages
            // but for now we'll focus on current week accuracy

            if (receivedThisWeek > 0 || sentThisWeek > 0)
            {
                Console.WriteLine($"Added {receivedThisWeek} received and {sentThisWeek} sent emails to current week for {userEmail}");
                Console.WriteLine($"Week range: {startOfWeek:yyyy-MM-dd} to {today:yyyy-MM-dd} (UTC)");
                await SaveAnalyticsAsync(analytics);
            }
            else if (receivedDates.Any() || sentDates.Any())
            {
                // Log when we have emails but none count toward this week (helpful for debugging)
                var oldestReceived = receivedDates.Any() ? receivedDates.Min().ToString("yyyy-MM-dd HH:mm UTC") : "none";
                var newestReceived = receivedDates.Any() ? receivedDates.Max().ToString("yyyy-MM-dd HH:mm UTC") : "none";
                var oldestSent = sentDates.Any() ? sentDates.Min().ToString("yyyy-MM-dd HH:mm UTC") : "none";
                var newestSent = sentDates.Any() ? sentDates.Max().ToString("yyyy-MM-dd HH:mm UTC") : "none";
                
                Console.WriteLine($"No emails counted for current week for {userEmail}:");
                Console.WriteLine($"  Current week: {startOfWeek:yyyy-MM-dd} to {today:yyyy-MM-dd} (UTC)");
                Console.WriteLine($"  Received emails: {oldestReceived} to {newestReceived}");
                Console.WriteLine($"  Sent emails: {oldestSent} to {newestSent}");
            }
        }

        /// <summary>
        /// Convenience method for when we sync a single email and need to check if it's current
        /// </summary>
        public async Task ProcessSyncedEmailAsync(string userEmail, DateTime emailDate, bool isReceived)
        {
            DateTime today = DateTime.UtcNow.Date;
            DateTime startOfWeek = today.AddDays(-(int)today.DayOfWeek);

            // Only count if email is from this week
            if (emailDate.Date >= startOfWeek && emailDate.Date <= today)
            {
                if (isReceived)
                {
                    await OnEmailReceivedTodayAsync(userEmail);
                }
                else
                {
                    await OnEmailSentTodayAsync(userEmail);
                }
            }
            // Ignore emails older than this week for current stats
        }

        // ===========================================
        // ANALYTICS QUERIES
        // ===========================================

        /// <summary>
        /// Gets email category breakdown for the current month as percentages
        /// </summary>
        public async Task<List<object>> GetEmailCategoryBreakdownAsync(string userEmail)
        {
            var currentMonth = DateTime.UtcNow.Date.AddDays(1 - DateTime.UtcNow.Day);
            var nextMonth = currentMonth.AddMonths(1);

            var userMailAccountIds = await _context.MailAccounts
                .Where(ma => ma.AppUserEmail == userEmail)
                .Select(ma => ma.MailAccountId)
                .ToListAsync();

            if (!userMailAccountIds.Any())
            {
                return new List<object>();
            }

            var allEmails = await _context.Mails
                .Where(m => userMailAccountIds.Contains(m.MailAccountId) &&
                           ((m is MailReceived && ((MailReceived)m).TimeReceived >= currentMonth && ((MailReceived)m).TimeReceived < nextMonth) ||
                            (m is MailSent && ((MailSent)m).TimeSent >= currentMonth && ((MailSent)m).TimeSent < nextMonth)))
                .ToListAsync();

            if (!allEmails.Any())
            {
                return new List<object> { new { name = "No Data", value = 100.0 } };
            }

            var totalEmails = allEmails.Count;
            var receivedCount = allEmails.OfType<MailReceived>().Count(m => !m.IsSpam && !m.IsDeleted);
            var sentCount = allEmails.OfType<MailSent>().Count(m => !m.IsDeleted);
            var spamCount = allEmails.Count(m => m.IsSpam && !m.IsDeleted);
            var favoriteCount = allEmails.Count(m => m.IsFavorite && !m.IsDeleted);
            var deletedCount = allEmails.Count(m => m.IsDeleted);

            var categories = new List<object>();

            if (receivedCount > 0)
                categories.Add(new { name = "Received", value = Math.Round((double)receivedCount / totalEmails * 100, 1) });
            
            if (sentCount > 0)
                categories.Add(new { name = "Sent", value = Math.Round((double)sentCount / totalEmails * 100, 1) });
            
            if (spamCount > 0)
                categories.Add(new { name = "Spam", value = Math.Round((double)spamCount / totalEmails * 100, 1) });
            
            if (favoriteCount > 0)
                categories.Add(new { name = "Favorites", value = Math.Round((double)favoriteCount / totalEmails * 100, 1) });
            
            if (deletedCount > 0)
                categories.Add(new { name = "Deleted", value = Math.Round((double)deletedCount / totalEmails * 100, 1) });

            if (!categories.Any())
                categories.Add(new { name = "Other", value = 100.0 });

            return categories;
        }

        /// <summary>
        /// Gets emails by time of day for the current week with averages for 6-hour time blocks
        /// </summary>
        public async Task<List<object>> GetEmailsByTimeOfDayAsync(string userEmail)
        {
            var today = DateTime.UtcNow.Date;
            var weekStart = today.AddDays(-6);

            var userMailAccountIds = await _context.MailAccounts
                .Where(ma => ma.AppUserEmail == userEmail)
                .Select(ma => ma.MailAccountId)
                .ToListAsync();

            if (!userMailAccountIds.Any())
            {
                return new List<object>
                {
                    new { range = "00–06", avg = 0.0 },
                    new { range = "06–09", avg = 0.0 },
                    new { range = "09–12", avg = 0.0 },
                    new { range = "12–15", avg = 0.0 },
                    new { range = "15–18", avg = 0.0 },
                    new { range = "18–24", avg = 0.0 }
                };
            }

            var receivedEmails = await _context.MailReceived
                .Where(m => userMailAccountIds.Contains(m.MailAccountId) &&
                           m.TimeReceived >= weekStart &&
                           m.TimeReceived <= today.AddDays(1) &&
                           !m.IsDeleted)
                .Select(m => m.TimeReceived)
                .ToListAsync();

            var timeRanges = new[]
            {
                new { range = "00–06", start = 0, end = 6 },
                new { range = "06–09", start = 6, end = 9 },
                new { range = "09–12", start = 9, end = 12 },
                new { range = "12–15", start = 12, end = 15 },
                new { range = "15–18", start = 15, end = 18 },
                new { range = "18–24", start = 18, end = 24 }
            };

            var result = new List<object>();

            foreach (var timeRange in timeRanges)
            {
                var emailsInRange = receivedEmails.Where(time => 
                {
                    var hour = time.Hour;
                    return hour >= timeRange.start && hour < timeRange.end;
                }).Count();

                var average = Math.Round(emailsInRange / 7.0, 1);
                result.Add(new { range = timeRange.range, avg = average });
            }

            return result;
        }

        /// <summary>
        /// Gets top senders for the last 7 days
        /// </summary>
        public async Task<List<object>> GetTopSendersAsync(string userEmail)
        {
            var today = DateTime.UtcNow.Date;
            var weekStart = today.AddDays(-6);

            var userMailAccountIds = await _context.MailAccounts
                .Where(ma => ma.AppUserEmail == userEmail)
                .Select(ma => ma.MailAccountId)
                .ToListAsync();

            if (!userMailAccountIds.Any())
            {
                return new List<object>();
            }

            var receivedEmails = await _context.MailReceived
                .Where(m => userMailAccountIds.Contains(m.MailAccountId) &&
                           m.TimeReceived >= weekStart &&
                           m.TimeReceived <= today.AddDays(1) &&
                           !m.IsDeleted)
                .Select(m => m.Sender)
                .ToListAsync();

            if (!receivedEmails.Any())
            {
                return new List<object>();
            }

            var senderCounts = receivedEmails
                .GroupBy(sender => string.IsNullOrWhiteSpace(sender) ? "Unknown Sender" : sender.Trim())
                .Select(group => new { sender = group.Key, count = group.Count() })
                .OrderByDescending(x => x.count)
                .Take(5)
                .ToList();

            var result = senderCounts
                .Select(sc => new { sender = sc.sender, count = sc.count })
                .Cast<object>()
                .ToList();

            return result;
        }

        /// <summary>
        /// Saves analytics changes safely
        /// </summary>
        private async Task SaveAnalyticsAsync(Analytics analytics)
        {
            analytics.LastUpdated = DateTime.UtcNow;
            _context.Analytics.Update(analytics);
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Console.WriteLine($"Concurrency error saving analytics for {analytics.AppUserEmail}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving analytics for {analytics.AppUserEmail}: {ex.Message}");
            }
        }

        // ===========================================
        // LEGACY METHOD COMPATIBILITY (Deprecated)
        // ===========================================
        // These methods are kept for backward compatibility but should be replaced
        
        [Obsolete("Use OnEmailDeletedTodayAsync instead")]
        public async Task IncrementDeletedEmailsWeeklyAsync(string userEmail) => await OnEmailDeletedTodayAsync(userEmail);
        
        [Obsolete("Use OnEmailReceivedTodayAsync instead")]
        public async Task UpdateEmailsReceivedWeeklyAsync(string userEmail) => await OnEmailReceivedTodayAsync(userEmail);
        
        [Obsolete("Use OnEmailSentTodayAsync instead")]
        public async Task UpdateEmailsSentWeeklyAsync(string userEmail) => await OnEmailSentTodayAsync(userEmail);
        
        [Obsolete("Use OnEmailMarkedSpamTodayAsync instead")]
        public async Task UpdateSpamEmailsWeeklyAsync(string userEmail) => await OnEmailMarkedSpamTodayAsync(userEmail);
        
        [Obsolete("Use OnEmailReadTodayAsync instead")]
        public async Task UpdateReadEmailsWeeklyAsync(string userEmail) => await OnEmailReadTodayAsync(userEmail);
    }
}