// OfflinePOS.DataAccess/Services/AuthService.cs
using Microsoft.EntityFrameworkCore;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.Services;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OfflinePOS.DataAccess.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;

        public AuthService(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<User> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

            if (user == null)
                return null;

            // Verify password
            var salt = Convert.FromBase64String(user.PasswordSalt);
            var hashedPassword = HashPassword(password, salt);

            if (hashedPassword != user.PasswordHash)
                return null;

            return user;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            var salt = Convert.FromBase64String(user.PasswordSalt);
            var currentPasswordHash = HashPassword(currentPassword, salt);

            if (currentPasswordHash != user.PasswordHash)
                return false;

            // Generate new salt and hash for the new password
            var newSalt = GenerateSalt();
            var newPasswordHash = HashPassword(newPassword, newSalt);

            user.PasswordSalt = Convert.ToBase64String(newSalt);
            user.PasswordHash = newPasswordHash;

            await _context.SaveChangesAsync();
            return true;
        }

        private string HashPassword(string password, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(32);
            return Convert.ToBase64String(hash);
        }

        private byte[] GenerateSalt()
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }
    }
}