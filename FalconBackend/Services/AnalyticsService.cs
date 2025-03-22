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


        /// Creates an analytics record when a new user signs up.

        public async Task CreateAnalyticsForUserAsync(string userEmail)
        {

            var existingAnalytics = await _context.Analytics.FirstOrDefaultAsync(a => a.AppUserEmail == userEmail);
            if (existingAnalytics != null)
                throw new Exception("Analytics already exists for this user.");

            var analytics = new Analytics
            {
                AppUserEmail = userEmail,
                LastResetDate = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };

            _context.Analytics.Add(analytics);
            await _context.SaveChangesAsync();
        }


        /// Retrieves analytics data for a user.

        public async Task<Analytics> GetAnalyticsForUserAsync(string userEmail)
        {
            var analytics = await _context.Analytics.FirstOrDefaultAsync(a => a.AppUserEmail == userEmail);
            if (analytics == null)
                throw new Exception("Analytics data not found for this user.");
            return analytics;
        }


        /// Updates the time spent today and this week based on the last login time.
        /// Should be called when the user logs out or their session ends.

        public async Task UpdateTimeSpentTodayAsync(string userEmail)
        {
            await CheckAndResetStatsOnLoginAsync(userEmail);

            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null || !user.LastLogin.HasValue)
                throw new Exception("User not found or missing login time.");

            DateTime loginTime = user.LastLogin.Value;
            DateTime currentTime = DateTime.UtcNow;
            double sessionDuration = (currentTime - loginTime).TotalMinutes;

            var analytics = await GetAnalyticsForUserAsync(userEmail);
            analytics.TimeSpentToday += (float)sessionDuration;
            analytics.TimeSpentThisWeek += (float)sessionDuration;
            analytics.TotalTimeSpent += (float)sessionDuration;

            // Update streak
            UpdateStreaks(analytics);

            await SaveAnalyticsAsync(analytics);
        }


        /// Updates the daily and weekly average time spent.

        public async Task UpdateAvgTimeSpentDailyAsync(string userEmail)
        {
            await CheckAndResetStatsOnLoginAsync(userEmail);

            var analytics = await GetAnalyticsForUserAsync(userEmail);

            // Ensure total days tracked is at least 1
            if (analytics.TotalDaysTracked == 0)
                analytics.TotalDaysTracked = 1;

            // Add today's time to total
            analytics.TotalTimeSpent += analytics.TimeSpentToday;

            // Increase day count if today is a new day
            if (analytics.LastUpdated.Date < DateTime.UtcNow.Date)
                analytics.TotalDaysTracked += 1;

            // Calculate new average
            analytics.AvgTimeSpentDaily = analytics.TotalTimeSpent / analytics.TotalDaysTracked;

            await SaveAnalyticsAsync(analytics);
        }

        public async Task UpdateAvgTimeSpentWeeklyAsync(string userEmail)
        {
            await CheckAndResetStatsOnLoginAsync(userEmail);

            var analytics = await GetAnalyticsForUserAsync(userEmail);
            analytics.AvgTimeSpentWeekly = analytics.TimeSpentThisWeek / Math.Max(1, ((int)DateTime.UtcNow.DayOfWeek + 1));
            await SaveAnalyticsAsync(analytics);
        }


        /// Updates the count of received emails.

        public async Task UpdateEmailsReceivedWeeklyAsync(string userEmail)
        {
            await CheckAndResetStatsOnLoginAsync(userEmail);

            var analytics = await GetAnalyticsForUserAsync(userEmail);
            analytics.EmailsReceivedWeekly += 1;
            await SaveAnalyticsAsync(analytics);
        }


        /// Updates the count of sent emails.

        public async Task UpdateEmailsSentWeeklyAsync(string userEmail)
        {
            await CheckAndResetStatsOnLoginAsync(userEmail);

            var analytics = await GetAnalyticsForUserAsync(userEmail);
            analytics.EmailsSentWeekly += 1;
            await SaveAnalyticsAsync(analytics);
        }


        /// Updates the count of spam emails.

        public async Task UpdateSpamEmailsWeeklyAsync(string userEmail)
        {
            await CheckAndResetStatsOnLoginAsync(userEmail);

            var analytics = await GetAnalyticsForUserAsync(userEmail);
            analytics.SpamEmailsWeekly += 1;
            await SaveAnalyticsAsync(analytics);
        }


        /// Updates the count of read emails.

        public async Task UpdateReadEmailsWeeklyAsync(string userEmail)
        {
            await CheckAndResetStatsOnLoginAsync(userEmail);

            var analytics = await GetAnalyticsForUserAsync(userEmail);
            analytics.ReadEmailsWeekly += 1;
            await SaveAnalyticsAsync(analytics);
        }


        /// Resets daily statistics.

        public async Task ResetDailyStatsAsync(string userEmail)
        {
            var analytics = await GetAnalyticsForUserAsync(userEmail);

            if (analytics.LastUpdated.Date < DateTime.UtcNow.Date)
            {
                // Calculate average directly here to avoid cycle
                if (analytics.TotalDaysTracked == 0)
                    analytics.TotalDaysTracked = 1;

                analytics.TotalTimeSpent += analytics.TimeSpentToday;
                if (analytics.LastUpdated.Date < DateTime.UtcNow.Date)
                    analytics.TotalDaysTracked += 1;

                analytics.AvgTimeSpentDaily = analytics.TotalTimeSpent / analytics.TotalDaysTracked;

                analytics.AvgEmailsPerDay = (float)(analytics.EmailsReceivedWeekly + analytics.EmailsSentWeekly) / Math.Max(1, analytics.TotalDaysTracked);

                analytics.TimeSpentToday = 0;
                analytics.IsActiveToday = false;

                await SaveAnalyticsAsync(analytics);
            }
        }


        /// Resets weekly statistics.
        public async Task ResetWeeklyStatsAsync(string userEmail)
        {
            var analytics = await GetAnalyticsForUserAsync(userEmail);

            if (analytics.LastResetDate.Date < DateTime.UtcNow.Date.AddDays(-7))
            {
                // Update weekly averages before resetting
                await UpdateAvgTimeSpentWeeklyAsync(userEmail);
                analytics.AvgEmailsPerWeek = (float)(analytics.EmailsReceivedWeekly + analytics.EmailsSentWeekly) / 7.0f;

                // Reset weekly values
                analytics.EmailsReceivedWeekly = 0;
                analytics.EmailsSentWeekly = 0;
                analytics.ReadEmailsWeekly = 0;
                analytics.SpamEmailsWeekly = 0;
                analytics.TimeSpentThisWeek = 0;
                analytics.AvgTimeSpentWeekly = 0;
                analytics.LastResetDate = DateTime.UtcNow;

                await SaveAnalyticsAsync(analytics);
            }
        }

        public async Task CheckAndResetStatsOnLoginAsync(string userEmail)
        {
            var analytics = await GetAnalyticsForUserAsync(userEmail);

            DateTime now = DateTime.UtcNow;

            // Reset daily stats if a new day has started
            if (analytics.LastUpdated.Date < now.Date)
            {
                await ResetDailyStatsAsync(userEmail);
            }

            // Reset weekly stats if a new week has started
            if (analytics.LastResetDate.Date < now.Date.AddDays(-7))
            {
                await ResetWeeklyStatsAsync(userEmail);
            }
        }

        /// Updates user streaks for active days.
        private void UpdateStreaks(Analytics analytics)
        {
            if (analytics.LastUpdated.Date == DateTime.UtcNow.Date.AddDays(-1))
            {
                analytics.CurrentStreak++;
                analytics.LongestStreak = Math.Max(analytics.LongestStreak, analytics.CurrentStreak);
            }
            else if (analytics.LastUpdated.Date < DateTime.UtcNow.Date.AddDays(-1))
            {
                analytics.CurrentStreak = 1; // Reset streak if inactive for a day
            }
        }

        /// Saves analytics changes to the database.
        private async Task SaveAnalyticsAsync(Analytics analytics)
        {
            analytics.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
