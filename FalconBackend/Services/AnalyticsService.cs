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
        public async Task CheckAndResetStatsOnLoginAsync(string userEmail)
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
                    return;
                }
            }

            DateTime now = DateTime.UtcNow;
            DateTime today = now.Date;
            bool requiresSave = false;

            // --- Daily Reset Logic ---
            if (analytics.LastDailyReset < today)
            {
                Console.WriteLine($"Performing daily reset for {analytics.AppUserEmail} (Last: {analytics.LastDailyReset}, Today: {today})");

                // Store previous day's value
                analytics.TimeSpentYesterday = analytics.TimeSpentToday;

                if (analytics.IsActiveToday) 
                {
                    // Update totals first
                    analytics.TotalTimeSpent += analytics.TimeSpentToday;
                    analytics.TotalDaysTracked += 1;

                    // Update Daily Average Time Spent
                    analytics.AvgTimeSpentDaily = analytics.TotalTimeSpent / Math.Max(1, analytics.TotalDaysTracked); // Avoid division by zero

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
                    if (analytics.LastDailyReset < today.AddDays(-1))
                    {
                        analytics.CurrentStreak = 0;
                    }
                }

                // Reset daily counters for the NEW day
                analytics.TimeSpentToday = 0;
                analytics.IsActiveToday = false;
                analytics.LastDailyReset = today;
                requiresSave = true;
            }

            // --- Weekly Reset Logic ---
            DateTime startOfWeek = today.AddDays(-(int)today.DayOfWeek); // Assuming Sunday start
            if (analytics.LastWeeklyReset < startOfWeek)
            {
                Console.WriteLine($"Performing weekly reset for {analytics.AppUserEmail} (Last: {analytics.LastWeeklyReset}, StartOfWeek: {startOfWeek})");

                analytics.AvgTimeSpentWeekly = analytics.TimeSpentThisWeek / 7.0f;
                analytics.AvgEmailsPerWeek = (float)(analytics.EmailsReceivedWeekly + analytics.EmailsSentWeekly) / 7.0f;

                // Store previous week's values
                analytics.TimeSpentLastWeek = analytics.TimeSpentThisWeek;
                analytics.EmailsReceivedLastWeek = analytics.EmailsReceivedWeekly;
                analytics.EmailsSentLastWeek = analytics.EmailsSentWeekly;
                analytics.SpamEmailsLastWeek = analytics.SpamEmailsWeekly;
                analytics.ReadEmailsLastWeek = analytics.ReadEmailsWeekly;

                // Reset weekly counters for the NEW week
                analytics.TimeSpentThisWeek = 0;
                analytics.EmailsReceivedWeekly = 0;
                analytics.EmailsSentWeekly = 0;
                analytics.SpamEmailsWeekly = 0;
                analytics.ReadEmailsWeekly = 0;
                analytics.LastWeeklyReset = startOfWeek; 
                requiresSave = true;
            }

            if (requiresSave)
            {
                await SaveAnalyticsAsync(analytics);
            }
        }


        /// <summary>
        /// Updates time spent based on user's LastLogin. Should be called periodically or on logout.
        /// Assumes LastLogin is updated elsewhere when a session starts/resumes.
        /// </summary>
        public async Task UpdateTimeSpentAsync(string userEmail)
        {
            // Check for resets *before* calculating time spent
            await CheckAndResetStatsOnLoginAsync(userEmail);

            // Fetch potentially updated analytics and user
            var analytics = await _context.Analytics.FirstOrDefaultAsync(a => a.AppUserEmail == userEmail);
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null || !user.LastLogin.HasValue || analytics == null)
            {
                Console.WriteLine($"Cannot update time spent for {userEmail}. User, LastLogin, or Analytics missing.");
                return;
            }

            DateTime loginTime = user.LastLogin.Value;
            DateTime currentTime = DateTime.UtcNow;
            double sessionDurationMinutes = (currentTime - loginTime).TotalMinutes;

            if (sessionDurationMinutes <= 0) return; 

            // Add calculated duration to relevant counters
            analytics.TimeSpentToday += (float)sessionDurationMinutes;
            analytics.TimeSpentThisWeek += (float)sessionDurationMinutes;

            // Mark user as active today if this is their first action today
            bool needsSave = false;
            if (!analytics.IsActiveToday)
            {
                analytics.IsActiveToday = true;
                needsSave = true;
            }

            user.LastLogin = currentTime;
            _context.AppUsers.Update(user);

            // Save changes (only save analytics if activity flag changed, otherwise only user)
            if (needsSave)
            {
                await SaveAnalyticsAsync(analytics); // Updates LastUpdated timestamp
            }
            else
            {
                analytics.LastUpdated = currentTime; // Still update timestamp even if only time changed
                _context.Analytics.Update(analytics);
            }
            await _context.SaveChangesAsync(); // Save user and potentially analytics changes
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