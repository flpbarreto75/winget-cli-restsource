// -----------------------------------------------------------------------
// <copyright file="HttpHelpers.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.WinGet.RestSource.Utils.Common
{
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// Class that contains helpers for working with http client.
    /// </summary>
    public class HttpHelpers
    {
        /// <summary>
        /// This function wraps downloading files using http client.
        /// </summary>
        /// <param name="httpClient">Http Client.</param>
        /// <param name="uri">uri to download.</param>
        /// <param name="path">Path to save file.</param>
        /// <returns>Path where the file was saved.</returns>
        public static async Task<string> DownloadFile(
            HttpClient httpClient,
            string uri,
            string path = null)
        {
            string save = string.IsNullOrWhiteSpace(path) ?
                Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
                : path;

            using (var stream = await httpClient.GetStreamAsync(uri))
            {
                using (var fileStream = new FileStream(save, FileMode.CreateNew))
                {
                    await stream.CopyToAsync(fileStream);
                }
            }

            return save;
        }
    }
}
