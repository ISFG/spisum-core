using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models.CodeList;
using ISFG.Alfresco.Api.Services;
using ISFG.Common.Interfaces;
using ISFG.SpisUm.ClientSide.Authentification;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.Interfaces;
using Newtonsoft.Json;
using Serilog;

namespace ISFG.SpisUm.InitialScripts
{
    public class InitialCodeLists : IInicializationScript
    {
        #region Fields

        private readonly IAlfrescoConfiguration _alfrescoConfig;
        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private bool _initiated;

        #endregion

        #region Constructors

        public InitialCodeLists(
            IAlfrescoConfiguration alfrescoConfiguration, 
            ISimpleMemoryCache simpleMemoryCache,
            ISystemLoginService systemLoginService)
        {
            _alfrescoHttpClient = new AlfrescoHttpClient(alfrescoConfiguration, new AdminAuthentification(simpleMemoryCache, alfrescoConfiguration, systemLoginService));
            _alfrescoConfig = alfrescoConfiguration;
        }

        #endregion

        #region Implementation of IInicializationScript

        public async Task Init()
        {
            if (_initiated)
                return;
            
            _initiated = true;

            try
            {
                var lists = await GetAllCodeListsWithValues();
                var fileContents = new List<CodeListFileContent>();

                foreach (var file in Directory.GetFiles(_alfrescoConfig.ConfigurationFiles.CodeLists.FolderName, "*.json"))
                    try
                    {
                        fileContents.Add(JsonConvert.DeserializeObject<CodeListFileContent>(File.ReadAllText(file)));
                    }
                    catch
                    {

                    }

                // Delete lists that are not longer in configuration files
                var deletedLists = await DeleteOldCodeLists(lists.Values, fileContents);
                
                foreach (var list in deletedLists)
                    lists.Values.Remove(list);                

                // Update lists
                await UpdateExistingCodeLists(lists.Values, fileContents);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates a new Code List with provided values and sets proper authority for each value.
        /// </summary>
        /// <param name="codeListCofinguration">Configuration model of the new code list</param>
        /// <returns></returns>
        private async Task<CodeListCreateARM> CodeListCreate(CodeListFileContent codeListCofinguration)
        {
            var newCodeList = new CodeListCreateAM
            {
                Name = codeListCofinguration.Name,
                Title = codeListCofinguration.Title,
                AllowedValues = codeListCofinguration.Values.Select(x => x.Name).ToArray()
            };

            var result = await _alfrescoHttpClient.CodeListCreate(newCodeList);

            await UpdateValueWithAuthority(codeListCofinguration);

            return result;
        }

        /// <summary>
        /// Updates code list title with no changes to the values.
        /// </summary>
        /// <param name="newTitle">New title of the list</param>
        /// <param name="listName">Name of the list to rename</param>
        /// <returns>Code list with all values</returns>
        private async Task<CodeListUpdateARM> CodeListUpdate(string listName, string newTitle)
        {
            var newValues = new CodeListUpdateAM { Title = newTitle };

            return await _alfrescoHttpClient.CodeListUpdate(listName, newValues);
        }

        /// <summary>
        /// Updates Code Lists allowed values. Deletes existing values that are not in provided in the list.
        /// </summary>
        /// <param name="codeListCofinguration">Code List configuration of values</param>
        /// <returns>Updates Code List with values.</returns>
        private async Task<CodeListUpdateValuesARM> CodeListUpdateValues(CodeListFileContent codeListCofinguration)
        {
            var newValues = new CodeListUpdateValuesAM();
            newValues.AllowedValues.AddRange(codeListCofinguration.Values.Select(x => x.Name));

            return await _alfrescoHttpClient.CodeListUpdateValues(codeListCofinguration.Name, newValues);
        }

        private CodeListCaseSensitiveWithValues<CodeListValue> Copy(CodeListCaseSensitiveWithValues<CodeListValue> input)
        {
            var listWithValues = new CodeListCaseSensitiveWithValues<CodeListValue>
            {
                Url = input.Url,
                IsCaseSensitive = input.IsCaseSensitive,
                Name = input.Name,
                Title = input.Title                
            };

            foreach (var value in input.Values)
                listWithValues.Values.Add(new CodeListValue
                {
                    Url = value.Url,
                    ValueName = value.ValueName,
                    ValueTitle = value.ValueTitle
                });

            return listWithValues;
        }

        private async Task<List<CodeListCaseSensitiveWithValues<CodeListValue>>> DeleteOldCodeLists(
            List<CodeListCaseSensitiveWithValues<CodeListValue>> existingLists, 
            List<CodeListFileContent> newLists)
        {
            var deletedLists = new List<CodeListCaseSensitiveWithValues<CodeListValue>>();

            foreach (var list in existingLists.Where(x => !newLists.Exists(y => y.Name == x.Name)))
                try
                {
                    await _alfrescoHttpClient.CodeListDelete(list.Name);
                    deletedLists.Add(list);
                }
                catch
                {
                    // Do nothing, it could not just delete the list. Reason is it has constrains.
                }

            return deletedLists;
        }


        private async Task<CodeListAll> GetAllCodeListsWithValues()
        {
            var completeList = new CodeListAll();

            var alfrescoResponse = await _alfrescoHttpClient.CodeListGetAll();

            foreach (var list in alfrescoResponse.CodeLists)
            {
                var values = await _alfrescoHttpClient.CodeListGetWithValues(list.Name);

                completeList.Values.Add(Copy(values.CodeList));
            }

            return completeList;
        }

        private async Task<bool> UpdateExistingCodeLists(List<CodeListCaseSensitiveWithValues<CodeListValue>> existingLists, List<CodeListFileContent> newLists)
        {
            foreach (var newList in newLists)
            {
                var existingList = existingLists.FirstOrDefault(x => x.Name == newList.Name);

                if (existingList == null)
                {
                    // List doesn't exists
                    await CodeListCreate(newList);
                    continue;
                }
          
                // Updates title of the list if needed
                if (!newList.Title.Equals(existingList.Title))
                    await CodeListUpdate(newList.Name, newList.Title);

                // Update values if some needs to be deleted or there are new values
                var valuesToDelete = existingList.Values.Where(p => !newList.Values.Any(p2 => p2.Name == p.ValueName)); 
                if (valuesToDelete.Any() || existingList.Values.Count() != newList.Values.Count()) await CodeListUpdateValues(newList);

                await UpdateValueWithAuthority(newList);
            }

            return true;
        }

        /// <summary>
        /// Updates Code List allowed values authorities based on provided list.
        /// </summary>
        /// <param name="codeListCofinguration">Code List configuration of values</param>
        /// <returns>Updated Code List with values and authorities</returns>
        private async Task<CodeListUpdateValuesAthoritiesARM> UpdateValueWithAuthority(CodeListFileContent codeListCofinguration)
        {
            var updateModel = new CodeListUpdateValuesAuthorityAM();
            updateModel.Values.AddRange(from content in codeListCofinguration.Values select new CodeListValueWithAuthority
            {
                Value = content.Name,
                Authorities = content.Authorities
            });

            return await _alfrescoHttpClient.CodeListUpdateValuesWithAuthority(codeListCofinguration.Name, updateModel);
        }

        #endregion
    }
}