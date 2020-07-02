using System;
using AutoMapper;
using EntityFrameworkPaginateCore;
using ISFG.SpisUm.ClientSide.Models;
using Newtonsoft.Json;
using Serilog;

namespace ISFG.SpisUm.ClientSide.Mappings
{
    public class AuditLogMapping : Profile
    {
        #region Constructors

        public AuditLogMapping()
        {
            CreateMap<Data.Models.TransactionHistory, TransactionHistory>()
                .ForMember(x => x.Id, s => s.MapFrom(m => m.Id))
                .ForMember(x => x.NodeId, s => s.MapFrom(m => m.NodeId))
                .ForMember(x => x.SslNodeType, s => s.MapFrom(m => m.SslNodeType))
                .ForMember(x => x.Pid, s => s.MapFrom(m => m.Pid))
                .ForMember(x => x.NodeType, s => s.MapFrom(m => m.FkNodeTypeCodeNavigation.Code))
                .ForMember(x => x.OccuredAt, s => s.MapFrom(m => m.OccuredAt.ToLocalTime()))
                .ForMember(x => x.UserId, s => s.MapFrom(m => m.UserId))
                .ForMember(x => x.UserGroupId, s => s.MapFrom(m => m.UserGroupId))
                .ForMember(x => x.EventType, s => s.MapFrom(m => m.FkEventTypeCodeNavigation.Code))
                .ForMember(x => x.EventParameters, s => s.MapFrom(m => DeserializeJson(m.EventParameters)))
                .ForMember(x => x.Description, s => s.MapFrom(m => GetMessage()))
                .ForMember(x => x.EventSource, s => s.MapFrom(m => m.EventSource));

            CreateMap<Page<Data.Models.TransactionHistory>, TransactionHistoryPage<TransactionHistory>>()
                .ForMember(x => x.Results, s => s.MapFrom(m => m.Results))
                .ForMember(x => x.CurrentPage, s => s.MapFrom(m => m.CurrentPage))
                .ForMember(x => x.PageCount, s => s.MapFrom(m => m.PageCount))
                .ForMember(x => x.PageSize, s => s.MapFrom(m => m.PageSize))
                .ForMember(x => x.RecordCount, s => s.MapFrom(m => m.RecordCount));
        }

        #endregion

        #region Properties

        private bool _eventDeserialized { get; set; }

        private TransactionHistoryParameters _eventParameters { get; set; }

        #endregion

        #region Private Methods

        private TransactionHistoryParameters DeserializeJson(string jsonParameters)
        {
            try
            {
                _eventParameters = JsonConvert.DeserializeObject<TransactionHistoryParameters>(jsonParameters, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Can't parse value {jsonParameters}");
            }

            return _eventParameters;
        }

        private string GetMessage()
        {
            return _eventParameters?.Message;
        }

        #endregion
    }
}