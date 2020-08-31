using ISFG.DataBox.Api.Interfaces;
using ISFG.DataBox.Api.Models;
using ISFG.SpisUm.ClientSide.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace ISFG.SpisUm.ClientSide.Services
{
    public class DataBoxService : IDataBoxService
    {
        public readonly IDataBoxHttpClient _dataBoxHttpClient;

        public DataBoxService(IDataBoxHttpClient dataBoxHttpClient)
        {
            _dataBoxHttpClient = dataBoxHttpClient;
        }
        public async Task<DataBoxSendResponse> SendMessage(DataBoxSend input)
        {
            var accounts = await _dataBoxHttpClient.Accounts();

            var account = accounts.FirstOrDefault(x => x.Id == input.SenderId);
            if (account == null)
                return new DataBoxSendResponse(false, "Provided databox was not found in configuration");

            input.SenderId = account.Username;

            return await  _dataBoxHttpClient.Send(input);
        }
    }
}
