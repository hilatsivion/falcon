using FalconBackend.Data;
using FalconBackend.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

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
        /// Increments weekly deleted email count.
        /// </summary>
        public async Task IncrementDeletedEmailsWeeklyAsync(string userEmail)
        {
            await CheckAndResetStatsOnLoginAsync(userEmail); // Ensure stats are current
            var analytics = await _context.Analytics.FirstOrDefaultAsync(a => a.AppUserEmail == userEmail);
            if (analytics == null) return; // Should not happen if CreateAnalytics called properly

            analytics.DeletedEmailsWeekly++;
            if (!analytics.IsActiveToday) analytics.IsActiveToday = true; // Mark active
            await SaveAnalyticsAsync(analytics);
            Console.WriteLine($"Incremented deleted count for {userEmail}");
        }

        /// <summary>
        /// Creates an initial analytics record for a new user. Initializes reset dates.
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
        /// Retrieves analytics data for a user, ensuring stats are checked/reset first.
        /// This should be the primary method used by controllers to get analytics.
        /// </summary>
        public async Task<Analytics> GetAnalyticsForUserAsync(string userEmail)
        {
            await CheckAndResetStatsOnLoginAsync(userEmail);

            var analytics = await _context.Analytics.FirstOrDefaultAsync(a => a.AppUserEmail == userEmail);

            if (analytics == null)
            {
                Console.WriteLine($"Analytics data not found for user {userEmail} in GetAnalyticsForUserAsync AFTER reset check. This might indicate an issue.");
                throw new Exception($"Analytics data not found for user {userEmail}.");
            }
            return analytics;
        }

        /// <summary>
        /// Central method to check for and apply daily/weekly resets.
        /// Also updates averages and streaks based on concluded periods.
        /// This is now the core logic hub.
        /// </summary>
        public async Task<bool> CheckAndResetStatsOnLoginAsync(string userEmail)
        {
            var analytics = await _context.Analytics.FirstOrDefaultAsync(a => a.AppUserEmail == userEmail);
            if (analytics == null)
            {
                Console.WriteLine($"Analytics not found for {userEmail} in CheckAndResetStats. Attempting creation.");
                await CreateAnalyticsForUserAsync(userEmail);
                analytics = await _context.Analytics.FirstOrDefaultAsync(a => a.AppUserEmail == userEmail);
                if (analytics == null)
                {
                    Console.WriteLine($"Failed to create or find analytics for {userEmail}. Aborting reset check.");
                    return false; // Indicate no reset happened
                }
            }

            DateTime now = DateTime.UtcNow;
            DateTime today = now.Date;
            bool requiresSave = false;
            bool dailyResetPerformed = false; // Flag to return

            // --- Daily Reset Logic ---
            if (analytics.LastDailyReset < today)
            {
                Console.WriteLine($"Performing daily reset for {analytics.AppUserEmail} (Last: {analytics.LastDailyReset}, Today: {today})");
                dailyResetPerformed = true; // Mark that reset occurred

                // Store previous day's value
                analytics.TimeSpentYesterday = analytics.TimeSpentToday;

                if (analytics.IsActiveToday)
                {
                    // Update totals first
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
                    // If not active today, check if the gap broke the streak
                    // (Handle case where LastDailyReset might be much older than yesterday)
                    if (analytics.LastDailyReset < today.AddDays(-1))
                    {
                        analytics.CurrentStreak = 0;
                    }
                    // If LastDailyReset was exactly yesterday, but they weren't active, streak also breaks.
                    else if (analytics.LastDailyReset == today.AddDays(-1))
                    {
                        analytics.CurrentStreak = 0; // Reset streak if inactive yesterday
                    }

                }

                // Reset daily counters for the NEW day
                analytics.TimeSpentToday = 0; // Reset today's counter
                analytics.IsActiveToday = false;
                analytics.LastDailyReset = today;
                requiresSave = true;
            }

            // --- Weekly Reset Logic --- (No changes needed here for this issue)
            DateTime startOfWeek = today.AddDays(-(int)today.DayOfWeek); // Assuming Sunday start
            if (analytics.LastWeeklyReset < startOfWeek)
            {
                Console.WriteLine($"Performing weekly reset for {analytics.AppUserEmail} (Last: {analytics.LastWeeklyReset}, StartOfWeek: {startOfWeek})");

                // Calculate averages *before* resetting weekly counters if week has data
                if (analytics.TimeSpentThisWeek > 0 || analytics.EmailsReceivedWeekly > 0 || analytics.EmailsSentWeekly > 0)
                {
                    // Avoid division by zero, perhaps calculate based on days passed in week?
                    // Simple approach: just average over 7 days for now.
                    analytics.AvgTimeSpentWeekly = analytics.TimeSpentThisWeek / 7.0f;
                    analytics.AvgEmailsPerWeek = (float)(analytics.EmailsReceivedWeekly + analytics.EmailsSentWeekly) / 7.0f;
                }
                else
                {
                    analytics.AvgTimeSpentWeekly = 0;
                    analytics.AvgEmailsPerWeek = 0;
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
                analytics.DeletedEmailsWeekly = 0; // Reset deleted counter too
                analytics.LastWeeklyReset = startOfWeek;
                requiresSave = true;
            }


            if (requiresSave)
            {
                await SaveAnalyticsAsync(analytics);
            }

            return dailyResetPerformed; // Return the flag
        }


        // Modify the UpdateTimeSpentAsync method:
        public async Task UpdateTimeSpentAsync(string userEmail)
        {
            DateTime currentTime = DateTime.UtcNow;
            DateTime today = currentTime.Date;

            // Check for resets *before* calculating time spent and get reset status
            // *** Capture the return value ***
            bool dailyResetJustPerformed = await CheckAndResetStatsOnLoginAsync(userEmail);

            // Fetch potentially updated analytics and user
            var analytics = await _context.Analytics.FirstOrDefaultAsync(a => a.AppUserEmail == userEmail);
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null || !user.LastLogin.HasValue || analytics == null)
            {
                Console.WriteLine($"Cannot update time spent for {userEmail}. User, LastLogin, or Analytics missing.");
                // If user exists but LastLogin is null (first ever action?), set LastLogin now.
                if (user != null && !user.LastLogin.HasValue)
                {
                    user.LastLogin = currentTime;
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Initialized LastLogin for {userEmail}.");
                }
                return;
            }

            DateTime loginTime = user.LastLogin.Value; // This is the *previous* last seen time

            // *** --- Adjustment Logic --- ***
            DateTime effectiveStartTimeForCalc = loginTime;

            // If a daily reset just happened and the last login was *before* today,
            // then calculate time spent only from the beginning of *today*.
            if (dailyResetJustPerformed && loginTime < today)
            {
                effectiveStartTimeForCalc = today;
                Console.WriteLine($"Daily reset occurred. Calculating time for {userEmail} from start of today ({today}).");
            }
            // *** --- End Adjustment Logic --- ***

            double sessionDurationMinutes = (currentTime - effectiveStartTimeForCalc).TotalMinutes;

            // Only add positive duration
            if (sessionDurationMinutes > 0)
            {
                analytics.TimeSpentToday += (float)sessionDurationMinutes;
                analytics.TimeSpentThisWeek += (float)sessionDurationMinutes;
                Console.WriteLine($"Added {sessionDurationMinutes:F2} minutes to today's/week's time for {userEmail}. New TimeSpentToday: {analytics.TimeSpentToday:F2}");
            }
            else
            {
                // Duration might be zero or negative if calls are very close or slight clock skew
                Console.WriteLine($"Calculated session duration is not positive ({sessionDurationMinutes:F2}) for {userEmail}. No time added.");
            }


            // Mark user as active today if this is their first action today
            bool needsAnalyticsSave = false;
            if (!analytics.IsActiveToday)
            {
                analytics.IsActiveToday = true;
                needsAnalyticsSave = true;
                Console.WriteLine($"Marked {userEmail} as active today.");
            }

            // Always update LastLogin to the current time for the next calculation
            user.LastLogin = currentTime;
            _context.AppUsers.Update(user);

            // Update analytics LastUpdated timestamp regardless
            analytics.LastUpdated = currentTime;
            _context.Analytics.Update(analytics);


            // Save changes (user definitely changed, analytics might have)
            await _context.SaveChangesAsync();
            if (needsAnalyticsSave)
            {
                Console.WriteLine($"Saved analytics update for {userEmail}.");
            }
            else
            {
                Console.WriteLine($"Saved LastLogin update for {userEmail}.");
            }
        }

        
        /// <summary>
        /// Increments weekly emails received count.
        /// </summary>
        public async Task UpdateEmailsReceivedWeeklyAsync(string userEmail)
        {
            await CheckAndResetStatsOnLoginAsync(userEmail);
            var analytics = await _context.Analytics.FirstOrDefaultAsync(a => a.AppUserEmail == userEmail);
            if (analytics == null) return;

            analytics.EmailsReceivedWeekly++;
            if (!analytics.IsActiveToday) analytics.IsActiveToday = true;
            await SaveAnalyticsAsync(analytics);
        }

        /// <summary>
        /// Increments weekly emails sent count.
        /// </summary>
        public async Task UpdateEmailsSentWeeklyAsync(string userEmail) 
        {
            await CheckAndResetStatsOnLoginAsync(userEmail);
            var analytics = await _context.Analytics.FirstOrDefaultAsync(a => a.AppUserEmail == userEmail);
            if (analytics == null) return;

            analytics.EmailsSentWeekly++;
            if (!analytics.IsActiveToday) analytics.IsActiveToday = true;
            await SaveAnalyticsAsync(analytics);
        }

        /// <summary>
        /// Increments weekly spam email count.
        /// </summary>
        public async Task UpdateSpamEmailsWeeklyAsync(string userEmail) // Renamed for clarity
        {
            await CheckAndResetStatsOnLoginAsync(userEmail);
            var analytics = await _context.Analytics.FirstOrDefaultAsync(a => a.AppUserEmail == userEmail);
            if (analytics == null) return;

            analytics.SpamEmailsWeekly++;
            if (!analytics.IsActiveToday) analytics.IsActiveToday = true;
            await SaveAnalyticsAsync(analytics);
        }

        /// <summary>
        /// Increments weekly read email count.
        /// </summary>
        public async Task UpdateReadEmailsWeeklyAsync(string userEmail) 
        {
            await CheckAndResetStatsOnLoginAsync(userEmail);
            var analytics = await _context.Analytics.FirstOrDefaultAsync(a => a.AppUserEmail == userEmail);
            if (analytics == null) return;

            analytics.ReadEmailsWeekly++;
            if (!analytics.IsActiveToday) analytics.IsActiveToday = true;
            await SaveAnalyticsAsync(analytics);
        }

        /// <summary>
        /// Gets email category breakdown for the current month as percentages
        /// </summary>
        public async Task<List<object>> GetEmailCategoryBreakdownAsync(string userEmail)
        {
            var currentMonth = DateTime.UtcNow.Date.AddDays(1 - DateTime.UtcNow.Day); // First day of current month
            var nextMonth = currentMonth.AddMonths(1);

            // Get user's mail account IDs
            var userMailAccountIds = await _context.MailAccounts
                .Where(ma => ma.AppUserEmail == userEmail)
                .Select(ma => ma.MailAccountId)
                .ToListAsync();

            if (!userMailAccountIds.Any())
            {
                return new List<object>();
            }

            // Get all emails for the current month for this user
            var allEmails = await _context.Mails
                .Where(m => userMailAccountIds.Contains(m.MailAccountId) &&
                           ((m is MailReceived && ((MailReceived)m).TimeReceived >= currentMonth && ((MailReceived)m).TimeReceived < nextMonth) ||
                            (m is MailSent && ((MailSent)m).TimeSent >= currentMonth && ((MailSent)m).TimeSent < nextMonth)))
                .ToListAsync();

            if (!allEmails.Any())
            {
                return new List<object>
                {
                    new { name = "No Data", value = 100.0 }
                };
            }

            var totalEmails = allEmails.Count;

            // Calculate category counts
            var receivedCount = allEmails.OfType<MailReceived>().Count(m => !m.IsSpam && !m.IsDeleted);
            var sentCount = allEmails.OfType<MailSent>().Count(m => !m.IsDeleted);
            var spamCount = allEmails.Count(m => m.IsSpam && !m.IsDeleted);
            var favoriteCount = allEmails.Count(m => m.IsFavorite && !m.IsDeleted);
            var deletedCount = allEmails.Count(m => m.IsDeleted);

            // Convert to percentages
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

            // If no categories, return a default
            if (!categories.Any())
            {
                categories.Add(new { name = "Other", value = 100.0 });
            }

            return categories;
        }


        /// <summary>
        /// Saves analytics changes using DbContext.Update to handle potentially detached entities.
        /// </summary>
        private async Task SaveAnalyticsAsync(Analytics analytics)
        {
            analytics.LastUpdated = DateTime.UtcNow;
            _context.Analytics.Update(analytics); // Use Update for safety
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Handle potential concurrency issues if multiple requests modify the same record
                Console.WriteLine($"Concurrency error saving analytics for {analytics.AppUserEmail}: {ex.Message}");
                // Consider reloading the entity and reapplying changes or notifying user
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving analytics for {analytics.AppUserEmail}: {ex.Message}");
                // Handle other potential save errors
            }
        }
    }
}