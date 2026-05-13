using SubTaskerBackend.Models;

namespace SubTaskerBackend.Interfaces
{
    public interface ITokenService
    {
        public string CreateToken(User user);
    }
}