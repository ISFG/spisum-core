using System.Threading.Tasks;
using ISFG.Data.Database;
using ISFG.Data.Interfaces;
using ISFG.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ISFG.Data.Repositories
{
    public class SystemLoginRepository : ISystemLoginRepository
    {
        #region Fields

        private readonly SpisumContext _spisumContext;

        #endregion

        #region Constructors

        public SystemLoginRepository(SpisumContext spisumContext) => _spisumContext = spisumContext;

        #endregion

        #region Implementation of ISystemLoginRepository

        public async Task<SystemLogin> CreateUser(string username, string password)
        {
            var systemLoginModel = new SystemLogin
            {
                Username = username,
                Password = password
            };
            
            _spisumContext.SystemLogin.Add(systemLoginModel);
            await _spisumContext.SaveChangesAsync();

            return systemLoginModel;
        }

        public async Task<SystemLogin> GetUser(string username)
        {
            return await _spisumContext.SystemLogin
                .FirstOrDefaultAsync(x => x.Username == username);
        }

        #endregion
    }
}