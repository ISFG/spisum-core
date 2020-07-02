using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.SpisUm.ClientSide.Extensions;
using ISFG.SpisUm.ClientSide.Models;

namespace ISFG.SpisUm.ClientSide.Validators
{
    public class UserCreateValidator : AbstractValidator<UserCreate>
    {
        #region Constructors

        public UserCreateValidator(IAlfrescoHttpClient alfrescoHttpClient)
        {
            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(o => new Regex(@"^([a-zA-Z0-9_]+)$").IsMatch(o.Id))
                .MustAsync(async (context, cancellationToken) =>
                {
                    try
                    {
                        await alfrescoHttpClient.GetPerson(context.Id);
                        return false;
                    }
                    catch
                    {
                        return true;
                    }
                })
                .MustAsync(async (context, cancellationToken) =>
                {
                    var groups = new List<string>();
                    groups.AddRangeUnique(context.Groups);
                    groups.AddRangeUnique(context.SignGroups);
                    groups.Add(context.MainGroup);

                    foreach (var group in groups)
                        if (!await groupExist(alfrescoHttpClient, group))
                            return false;

                    return true;
                });
        }

        #endregion

        #region Private Methods

        private async Task<bool> groupExist(IAlfrescoHttpClient alfrescoHttpClient, string groupId)
        {
            try
            {
                await alfrescoHttpClient.GetGroup(groupId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
