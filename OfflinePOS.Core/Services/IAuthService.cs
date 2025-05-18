// OfflinePOS.Core/Services/IAuthService.cs
using OfflinePOS.Core.Models;
using System.Threading.Tasks;

namespace OfflinePOS.Core.Services
{
    public interface IAuthService
    {
        Task<User> AuthenticateAsync(string username, string password);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    }
}