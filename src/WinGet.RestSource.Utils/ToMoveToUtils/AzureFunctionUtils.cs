// -----------------------------------------------------------------------
// <copyright file="AzureFunctionUtils.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.WinGet.RestSource.Util.ToMoveToUtils
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Class that contains helper functions for azure functions.
    /// </summary>
    public class AzureFunctionUtils
    {
        /// <summary>
        /// Helper method to manually trigger an http function.
        /// </summary>
        /// <param name="httpClient">HttpClient.</param>
        /// <param name="httpMethod">HttpMethod.</param>
        /// <param name="azureFunctionURL">Azure function endpoint.</param>
        /// <param name="functionKey">Azure function key to access endpoint.</param>
        /// <param name="postRequestBody">Request body.</param>
        /// <returns>Http response message.</returns>
        public static async Task<HttpResponseMessage> TriggerAzureFunctionAsync(
            HttpClient httpClient,
            HttpMethod httpMethod,
            string azureFunctionURL,
            string functionKey,
            string postRequestBody = "")
        {
            // Create Post Request.
            HttpRequestMessage requestMessage = new HttpRequestMessage(httpMethod, azureFunctionURL);
            requestMessage.Headers.Add("x-functions-key", functionKey);
            if (!string.IsNullOrWhiteSpace(postRequestBody))
            {
                requestMessage.Content = new StringContent(postRequestBody, Encoding.UTF8, "application/json");
            }

            // Send Request.
            HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(requestMessage);
            return httpResponseMessage;
        }
    }
}
