using System.Text.RegularExpressions;
using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.SpisUm.ClientSide.Models;

namespace ISFG.SpisUm.ClientSide.Validators
{
    public class GroupValidator : AbstractValidator<CreateGroup>
    {
        #region Constructors

        public GroupValidator(IAlfrescoHttpClient alfrescoHttpClient)
        {
            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(o => new Regex(@"^([a-zA-Z0-9_]+)$").IsMatch(o.Id))
                .MustAsync(async (context, cancellationToken) =>
                {
                    try
                    {
                        await alfrescoHttpClient.GetGroup($"{SpisumNames.Prefixes.Group}{context.Id}");
                        return false;
                    }
                    catch
                    {
                        return true;
                    }
                });
        }

        #endregion
    }
}
