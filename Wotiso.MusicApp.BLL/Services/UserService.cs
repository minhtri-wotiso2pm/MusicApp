using System.Security.Cryptography;
using System.Text;
using Wotiso.MusicApp.DAL.Entities;
using Wotiso.MusicApp.DAL.Repositories;

namespace Wotiso.MusicApp.BLL.Services
{
    public class UserService
    {
        private UserRepository _repo;

        public UserService(UserRepository userRepository)
        {
            _repo = userRepository;
        }

        public User? GetByEmail(string email)
        {
            return _repo.GetByEmail(email);
        }

        public User? Login(string email, string password)
        {
            string hashed = HashPassword(password);

            return _repo.Login(email, hashed);
        }


        public bool Register(string name, string email, string password)
        {
            if (_repo.Exists(email))
                return false;

            string hashed = HashPassword(password);

            var user = new User
            {
                UserName = name,
                Email = email,
                PasswordHash = hashed,
                CreatedAt = DateTime.Now
            };

            return _repo.Register(user);
        }
        public string HashPassword(string password)
        {
            SHA256 sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder sb = new StringBuilder();

            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));

            return sb.ToString();

        }
    }
}
