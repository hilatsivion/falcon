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

        public AuthService(AppDbContext context, IConfiguration configuration) // Inject IConfiguration
        {
            _context = context;
            _jwtSecret = configuration["JwtSettings:SecretKey"] ?? throw new Exception("JWT Secret Key not found in configuration.");
        }

        public async Task<string> LogInAsync(string email, string password)
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw new Exception("User not found");

            if (!BCrypt.Net.BCrypt.Verify(password, user.HashedPassword))
                throw new Exception("Incorrect password");

            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return GenerateJwtToken(user);
        }

        public async Task<bool> SignUpAsync(string fullName, string username, string email, string password)
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
            await _context.SaveChangesAsync();

            return true;
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
                Expires = DateTime.UtcNow.AddHours(1),
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

                return user; // Returning only necessary fields
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
