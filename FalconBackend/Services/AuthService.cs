using FalconBackend.Data;
using FalconBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using BCrypt.Net;

namespace FalconBackend.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly string _jwtSecret;
        private readonly AnalyticsService _analyticsService;
        private readonly IConfiguration _configuration;


        public AuthService(AppDbContext context, IConfiguration configuration, AnalyticsService analyticsService)
        {
            _context = context;
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _jwtSecret = configuration["JwtSettings:Key"] ?? throw new Exception("JWT Secret Key not found in configuration.");
            _analyticsService = analyticsService;
        }

        private async Task CreateTestMailAccountForUserAsync(AppUser user)
        {
            var newAccount = new MailAccount
            {
                AppUserEmail = user.Email, 
                EmailAddress = user.Email,
                Token = Guid.NewGuid().ToString(),
                Provider = MailAccount.MailProvider.Gmail, 
                LastMailSync = DateTime.UtcNow,
                IsDefault = true 
            };

            _context.MailAccounts.Add(newAccount);
        }


        public async Task<string> LogInAsync(string email, string password)
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw new Exception("User not found");

            if (!BCrypt.Net.BCrypt.Verify(password, user.HashedPassword))
                throw new Exception("Incorrect password");

            // Ensure stats are up to date before login
            await _analyticsService.CheckAndResetStatsOnLoginAsync(email);

            // Update session time tracking
            if (user.LastLogin.HasValue)
            {
                await _analyticsService.UpdateTimeSpentAsync(email);
            }

            // Update LastLogin time
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return GenerateJwtToken(user);
        }

        public async Task<bool> LogOutAsync(string email)
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw new Exception("User not found");

            // Update time spent in the app before logging out
            await _analyticsService.UpdateTimeSpentAsync(email);

            return true;
        }

        public async Task<string> SignUpAsync(string fullName, string username, string email, string password)
        {
            if (await _context.AppUsers.AnyAsync(u => u.Email == email))
                throw new Exception("Email is already registered");

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            var newUser = new AppUser
            {
                Email = email,
                FullName = fullName,
                Username = username,
                HashedPassword = hashedPassword,
                CreatedAt = DateTime.UtcNow
            };

            _context.AppUsers.Add(newUser);

            await CreateTestMailAccountForUserAsync(newUser);


            await _context.SaveChangesAsync(); 

            await _analyticsService.CreateAnalyticsForUserAsync(email);

            string token = GenerateJwtToken(newUser);
            return token; 
        }


        public async Task<AppUser> AuthenticateUserAsync(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);

            try
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = handler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);
                var emailClaim = principal.FindFirst(ClaimTypes.Email);

                if (emailClaim == null)
                    return null;

                return await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == emailClaim.Value);
            }
            catch
            {
                return null;
            }
        }

        public async Task<AppUser> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                return null;

            return await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == email);
        }


        private string GenerateJwtToken(AppUser user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.Username)
                }),
                Expires = DateTime.UtcNow.AddHours(12),
                Issuer = _configuration["JwtSettings:Issuer"],      // ✅ Add this
                Audience = _configuration["JwtSettings:Audience"],  // ✅ And this
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }


        public async Task<object> GetUserProfileAsync(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);

            try
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = handler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);
                var jwtToken = validatedToken as JwtSecurityToken;

                if (jwtToken == null || !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                    return new { Error = "Invalid token" };

                var emailClaim = principal.FindFirst(ClaimTypes.Email);
                if (emailClaim == null)
                    return new { Error = "Invalid token: Missing email claim" };

                var user = await _context.AppUsers
                    .Where(u => u.Email == emailClaim.Value)
                    .Select(u => new
                    {
                        u.Email,
                        u.Username,
                        u.FullName,
                        u.CreatedAt,
                        u.LastLogin
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                    return new { Error = "User not found" };

                return user;
            }
            catch (SecurityTokenExpiredException)
            {
                return new { Error = "Token expired" };
            }
            catch (SecurityTokenException)
            {
                return new { Error = "Invalid token" };
            }
            catch (Exception ex)
            {
                return new { Error = $"An error occurred: {ex.Message}" };
            }
        }
    }
}
