using ISFG.DataBox.Api.Models;
using System.Threading.Tasks;

namespace ISFG.SpisUm.ClientSide.Interfaces
{
    public interface IDataBoxService
    {
        public Task<DataBoxSendResponse> SendMessage(DataBoxSend input);
    }
}
