using System.Linq;
using Wotiso.MusicApp.DAL.Entities;

namespace Wotiso.MusicApp.DAL.Repositories
{
    public class UserRepository
    {
        private MusicPlayerDbContext _ctx;

        public UserRepository(MusicPlayerDbContext context)
        {
            _ctx = context;
        }

        public User? GetByEmail(string email)
        {
            _ctx = new MusicPlayerDbContext();
            return _ctx.Users.FirstOrDefault(u => u.Email == email);
        }

        public User? Login(string email, string passwordHash)
        {
            _ctx = new MusicPlayerDbContext();
            return _ctx.Users.FirstOrDefault(u =>
                    u.Email == email &&
                    u.PasswordHash == passwordHash
            );
        }

        public bool Register(User user)
        {
            _ctx = new MusicPlayerDbContext();
            _ctx.Users.Add(user);
            return _ctx.SaveChanges() > 0;
        }

        public bool Exists(string email)
        {
            _ctx = new MusicPlayerDbContext();
            return _ctx.Users.Any(u => u.Email == email);
        }
    }
}
