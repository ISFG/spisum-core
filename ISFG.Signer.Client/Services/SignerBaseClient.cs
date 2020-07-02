using System;
using System.Threading.Tasks;
using ISFG.Common.Wcf;
using ISFG.Common.Wcf.Interfaces;
using ISFG.Signer.Client.Exceptions;
using ISFG.Signer.Client.Generated.Signer;
using ISFG.Signer.Client.Interfaces;

namespace ISFG.Signer.Client.Services
{

    public class SignerBaseClient : WcfBaseClient<TSPWebServiceSoap>, ISignerClient
    {
        #region Constructors

        public SignerBaseClient(IChannelConfig<TSPWebServiceSoap> channelConfig) : base(channelConfig)
        {
        }

        #endregion

        #region Implementation of ISignerClient

        public async Task<SealResponse> Seal(byte[] fileData) => await InvokeRequest(() => 
            Channel.SealAsync(new SealRequest
            {
                Input = fileData,
                FileName = $"{Guid.NewGuid()}.pdf",
                FileType = DocType.PDF,
                SignatureType = SignatureType.DEFAULT
            }));

        public async Task<ValidateResponse> Validate(byte[] fileData) => await InvokeRequest(() => 
            Channel.ValidateAsync(new ValidateRequest
            {
                FileData = fileData,
                FileName = $"{Guid.NewGuid()}.pdf",
                FileType = DocType.PDF,
                Properties = new PreservationProperties
                {
                    GetReport = true,
                    GetPDFReport = false,
                    GetXMLReport = true,
                    GetHTMLReport = false
                }
            }));


        public async Task<ValidateCertificateResponse> ValidateCertificate(byte[] certificate) => await InvokeRequest(() => 
            Channel.ValidateCertificateAsync(new ValidateCertificateRequest
            {
                FileData = certificate,
                FileType = DocType.CERTIFICATE,
                Properties = new PreservationProperties
                {
                    GetReport = true,
                    GetPDFReport = false,
                    GetXMLReport = true,
                    GetHTMLReport = false
                }
            }));

        #endregion

        #region Private Methods

        private async Task<T> InvokeRequest<T>(Func<Task<T>> action)
        {
            try
            {
                return await action.Invoke();
            }            
            catch (Exception ex)
            {
                throw new SignerRequestException(ex.Message, ex);
            }
        }

        #endregion
    }
}