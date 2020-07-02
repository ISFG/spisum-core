using System.Collections.Generic;
using System.Threading.Tasks;
using ISFG.DataBox.Api.Models;

namespace ISFG.DataBox.Api.Interfaces
{
    public interface IDataBoxHttpClient
    {
        #region Public Methods

        Task<List<DataboxAccount>> Accounts();
        Task<int> Refresh();
        Task<DataBoxSendResponse> Send(DataBoxSend input);
        Task<DataBoxStatusResponse> Status(int id);

        #endregion
    }
}