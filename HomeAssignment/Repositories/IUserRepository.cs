using BCrypt.Net;
using HomeAssignment.Models;

namespace HomeAssignment.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByIdAsync(int id);
        Task<User> CreateAsync(User user);
        Task<bool> ExistsAsync(string username);
    }

    public class InMemoryUserRepository : IUserRepository
    {
        private readonly List<User> _users;
        private static int _nextId = 1;
        private static readonly object _lockObject = new object();

        public InMemoryUserRepository()
        {
            _users = new List<User>();

            // Create default admin and user for testing
            CreateDefaultUsers();
        }

        private void CreateDefaultUsers()
        {
            // Default Admin user: admin/admin123
            var adminUser = new User
            {
                Id = _nextId++,
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow
            };
            _users.Add(adminUser);

            // Default User: user/user123
            var regularUser = new User
            {
                Id = _nextId++,
                Username = "user",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("user123"),
                Role = UserRole.User,
                CreatedAt = DateTime.UtcNow
            };
            _users.Add(regularUser);
        }

        public Task<User?> GetByUsernameAsync(string username)
        {
            var user = _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(user);
        }

        public Task<User?> GetByIdAsync(int id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            return Task.FromResult(user);
        }

        public Task<User> CreateAsync(User user)
        {
            lock (_lockObject)
            {
                user.Id = _nextId++;
            }

            user.CreatedAt = DateTime.UtcNow;
            _users.Add(user);
            return Task.FromResult(user);
        }

        public Task<bool> ExistsAsync(string username)
        {
            var exists = _users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(exists);
        }
    }
}
