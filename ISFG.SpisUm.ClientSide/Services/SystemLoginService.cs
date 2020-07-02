using System.Threading.Tasks;
using ISFG.Common.Utils;
using ISFG.Data.Interfaces;
using ISFG.Data.Models;
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.ClientSide.Interfaces;

namespace ISFG.SpisUm.ClientSide.Services
{
    public class SystemLoginService : ISystemLoginService
    {
        #region Fields

        private readonly string _passwordSalt = "spisUm_Salt_2020";
        private readonly ISystemLoginRepository _systemLoginRepository;

        #endregion

        #region Constructors

        public SystemLoginService(ISystemLoginRepository systemLoginRepository) => _systemLoginRepository = systemLoginRepository;

        #endregion

        #region Implementation of ISystemLoginService

        public async Task<string> GetPassword(string username)
        {
            if (string.IsNullOrEmpty(username))
                throw new BadRequestException("Username is empty");
        
            var user = await _systemLoginRepository.GetUser(username);
            
            if (user?.Password != null) 
                return Cipher.Decrypt(user.Password, _passwordSalt);
            
            var password = IdGenerator.ShortGuid();
            await CreateUser(username, password);
            
            return password;
        }

        public async Task<bool> IsUserExists(string username)
        {
            var user = await _systemLoginRepository.GetUser(username);
            
            return user != null;
        }

        #endregion

        #region Private Methods

        private async Task<SystemLogin> CreateUser(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                throw new BadRequestException("Username or password is empty");
            
            var user = await _systemLoginRepository.GetUser(username);
            if (user?.Username != null)
                throw new BadRequestException($"User '{username}' already exists");
            
            return await _systemLoginRepository.CreateUser(username, Cipher.Encrypt(password, _passwordSalt));
        }

        #endregion
    }
}