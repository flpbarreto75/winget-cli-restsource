// -----------------------------------------------------------------------
// <copyright file="RestSourceHelpers.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.WinGet.RestSource.Helpers
{
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Msix.Utils.Logger;
    using Microsoft.OWCUtils.Diagnostics;
    using Microsoft.WinGet.RestSource.PowershellSupport.Helpers;
    using Microsoft.WinGet.RestSource.Util.ToMoveToUtils;
    using Microsoft.WinGet.RestSource.Utils.Constants;
    using Microsoft.WinGet.RestSource.Utils.Exceptions;
    using Microsoft.WinGet.RestSource.Utils.Models.Schemas;
    using Microsoft.WinGetUtil.Models.V1;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Wrapper class around PackageManifest object.
    /// Supports converting yaml manifest to json object.
    /// </summary>
    public sealed class RestSourceHelpers
    {
        /// <summary>
        /// Checks for an existing version of the app in the rest source.
        /// Converts yaml manifest into json format and put or posts data to rest source as appropriate.
        /// </summary>
        /// <param name="httpClient">http client.</param>
        /// <param name="loggingContext">Logging context.</param>
        /// <param name="logIdentifier">String to be used in logging to identify the package being add or updated.</param>
        /// <param name="manifest">Merged manifest data.</param>
        /// <returns>A <see cref="PackageManifest"/> representing the rest source json representation of the package.</returns>
        public static async Task<PackageManifest> AddOrUpdateRestSource(
            HttpClient httpClient,
            LoggingContext loggingContext,
            string logIdentifier,
            Manifest manifest)
        {
            // First query source for existing package manifest referencing same package ID.
            //   If found, we append/edit existing data before reposting it to source.
            string lookupExistingManifests = GetRestEndpoint(ApiConstants.AzFuncPackageManifestsEndpoint, manifest.Id);

            HttpResponseMessage getMessage = await AzureFunctionUtils.TriggerAzureFunctionAsync(
                httpClient,
                HttpMethod.Get,
                lookupExistingManifests,
                ApiConstants.AzureFunctionHostKey);

            if (!getMessage.IsSuccessStatusCode && getMessage.StatusCode != HttpStatusCode.NotFound)
            {
                // TODO: Better retry/error handling. for now, skip failing apps.
                // Bug 35680162: [Rest] Rebuild requires retries on error handling and better overall error logging / ICM integration
                Logger.Error($"{loggingContext} error reading existing manifest {logIdentifier}. {lookupExistingManifests}");
                throw new LookupPackageManifestException($"{getMessage.StatusCode}");
            }

            bool priorContent = false;
            string priorManifest = string.Empty;
            if (getMessage.StatusCode != HttpStatusCode.NoContent && getMessage.StatusCode != HttpStatusCode.NotFound)
            {
                priorContent = true;
                priorManifest = await getMessage.Content.ReadAsStringAsync();
            }

            // Convert the manifest into a rest manifestPost format and merge with any existing data.
            if (!string.IsNullOrWhiteSpace(priorManifest))
            {
                var data = JObject.Parse(priorManifest);
                priorManifest = data["Data"].ToString();
            }

            PackageManifest packageManifest = PackageManifestUtils.AddManifestToPackageManifest(
                manifest,
                priorManifest);

            // Post the manifest changes to the rest source.
            string manifestPostBody = JsonConvert.SerializeObject(packageManifest);

            // We must use PUT not POST if we are editing existing content.
            HttpResponseMessage uploadMessage;
            if (priorContent)
            {
                uploadMessage = await AzureFunctionUtils.TriggerAzureFunctionAsync(
                    httpClient,
                    HttpMethod.Put,
                    GetRestEndpoint(ApiConstants.AzFuncPackageManifestsEndpoint, packageManifest.PackageIdentifier),
                    ApiConstants.AzureFunctionHostKey,
                    JsonConvert.SerializeObject(packageManifest));
            }
            else
            {
                uploadMessage = await AzureFunctionUtils.TriggerAzureFunctionAsync(
                    httpClient,
                    HttpMethod.Post,
                    ApiConstants.AzFuncPackageManifestsEndpoint,
                    ApiConstants.AzureFunctionHostKey,
                    JsonConvert.SerializeObject(packageManifest));
            }

            if (!uploadMessage.IsSuccessStatusCode)
            {
                if (uploadMessage.StatusCode == HttpStatusCode.Conflict)
                {
                    Logger.Error($"{loggingContext} Conflict updating {logIdentifier}. {uploadMessage.Content} Post: {manifestPostBody} PriorContent:{priorContent}");
                }
                else
                {
                    // TODO: Better retry/error handling. for now, skip failing apps.
                    // Bug 35680162: [Rest] Rebuild requires retries on error handling and better overall error logging / ICM integration
                    Logger.Error($"{loggingContext} Error updating {logIdentifier}. {uploadMessage.Content} Post: {manifestPostBody} PriorContent: {priorContent}");
                }

                throw new UpdatePackageManifestException($"{uploadMessage.StatusCode}");
            }

            Logger.Info($"{loggingContext} Updated {logIdentifier}. Version: {manifest.Version}");
            return packageManifest;
        }

        /// <summary>
        /// Returns a rest endpoint with resource id.
        /// </summary>
        /// <param name="functionUrl">Function url.</param>
        /// <param name="resourceId">Resouce id.</param>
        /// <returns>Rest endpoint with resource id.</returns>
        public static string GetRestEndpoint(string functionUrl, string resourceId)
        {
            // Note: The endpoint needs to end with / as some resource Ids like twinkstar.browser do not reach the azure function route correctly
            // becuase of having .browser in the end. Adding the / ensures the azure funtion route is reached correctly.
            return functionUrl + "/" + resourceId + "/";
        }
    }
}
