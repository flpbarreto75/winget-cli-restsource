// -----------------------------------------------------------------------
// <copyright file="ManifestHelpers.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.WinGet.RestSource.Utils.Common
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Msix.Utils.Logger;
    using Microsoft.OWCUtils.Diagnostics;
    using Microsoft.OWCUtils.Github.Client;
    using Microsoft.OWCUtils.Helpers.FileHelper;
    using Microsoft.WinGetUtil.Helpers;

    /// <summary>
    /// Class that contains helpers for working with github multifile manifests.
    /// </summary>
    public class ManifestHelpers
    {
        /// <summary>
        /// This function processes a set of files in a github collection and attempts to format them as a merged manifest object.
        /// It returns a path to a file where the merged data is saved.
        /// </summary>
        /// <returns>Path to merged file.</returns>
        /// <param name="loggingContext">Logging context.</param>
        /// <param name="mainRepo">GitHub repo to work against.</param>
        /// <param name="path">Path in repo we are processing.</param>
        /// <param name="filesUnderPath">Set of files GitHub returned from under the path.</param>
        public static async Task<string> CreateMergedManifestFromPath(
            LoggingContext loggingContext,
            IRepositoryClient mainRepo,
            string path,
            IReadOnlyList<Octokit.RepositoryContent> filesUnderPath)
        {
            string appDataFolder = Path.Combine(
                Path.GetTempPath(),
                Path.GetRandomFileName());

            // Try to construct the merged manifest
            // For each file, save it to a temp locations
            foreach (var repofile in filesUnderPath)
            {
                // The initial github fetch doesn't get the content of each file
                // Fetch the content.
                var yamlFile = await mainRepo.GetSingleFileRepositoryContentAsync(repofile.Path);

                string tempAppDataFilePath = Path.Combine(appDataFolder, repofile.Name);
                FileHelper.CreateFileWithContent(tempAppDataFilePath, yamlFile.Content, loggingContext);
            }

            // Construct merged manifest
            var packageFiles = Directory.GetFiles(appDataFolder);
            string mergedManifestFilePath =
                Path.Combine(
                    Path.GetTempPath(),
                    Path.GetRandomFileName() + ".yaml");

            if (packageFiles.Length > 1)
            {
                // Multi File manifest case
                // The winget client ValidateManifest function
                // merges the manifests into the merged manifest format.
                (bool succeeded, string response) = WinGetUtilWrapperManifest.ValidateManifest(
                    appDataFolder,
                    mergedManifestFilePath);

                // If we can't process a package, log and continue.
                if (!succeeded)
                {
                    Logger.Error($"{loggingContext}: Unable to process {path} due to {response}");
                    mergedManifestFilePath = null;
                }
            }
            else
            {
                mergedManifestFilePath = packageFiles.Single();
            }

            return mergedManifestFilePath;
        }
    }
}
