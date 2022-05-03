// -----------------------------------------------------------------------
// <copyright file="Update.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.WinGet.Update.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Msix.Utils.Logger;
    using Microsoft.OWCUtils.Diagnostics;
    using Microsoft.OWCUtils.FunctionInputHelpers;
    using Microsoft.OWCUtils.Github.Client;
    using Microsoft.OWCUtils.Helpers.FileHelper;
    using Microsoft.WinGet.RestSource.Helpers;
    using Microsoft.WinGet.RestSource.Util.ToMoveToUtils;
    using Microsoft.WinGet.RestSource.Utils.Common;
    using Microsoft.WinGet.RestSource.Utils.Constants;
    using Microsoft.WinGet.RestSource.Utils.Models.Schemas;
    using Microsoft.WinGet.RestSource.Utils.ToMoveToUtils;
    using Microsoft.WinGetUtil.Helpers;
    using Microsoft.WinGetUtil.Models.V1;

    /// <summary>
    /// Class that contains update operations.
    /// </summary>
    public class Update : IUpdate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Update"/> class.
        /// </summary>
        internal Update()
        {
        }

        /// <inheritdoc/>
        public async Task<SourceResultAndCommitsOutputHelper> ProcessUpdateRequestAsync(
            HttpClient httpClient,
            string operationId,
            List<string> commits,
            LoggingContext loggingContext)
        {
            Logger.Info($"{loggingContext}Starting to process updates request.");
            SourceResultAndCommitsOutputHelper taskResult;

            try
            {
                GitHubClient githubClient = new GitHubClient(
                    ApiConstants.GitHubRepository,
                    ApiConstants.GitHubServiceAccountToken);

                IRepositoryClient mainRepo = githubClient.GetRepositoryClient(
                    httpClient,
                    loggingContext);

                // For each commit:
                //  1. Fetch the github commit
                //  2. Filter files to manifests (Ignore all metadata files)
                //  3. Iterate files in commit to find each folder path touched by the commit
                //  4. For each folder path:
                //    5. Fetch all files under path for that commit
                //    6. create merged manifest
                //    7. if no data, attempt to delete item from rest source
                //    8. else check if we already have same package ID in rest source
                //    9. Put or push new merged manifest data as appropriate.
                taskResult = new SourceResultAndCommitsOutputHelper(SourceResultType.Success);
                foreach (var commitId in commits)
                {
                    try
                    {
                        var commit = await mainRepo.GetCommitAsync(
                            commitId);

                        Dictionary<string, List<string>> pathsAndDeletedItems =
                            this.IdentifyUniquePathsAndDeletedItems(loggingContext, commit);

                        CommitResults result = await this.ProcessPathsInCommit(httpClient, loggingContext, mainRepo, commitId, pathsAndDeletedItems);

                        taskResult.AddCommitResult(result);
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"{loggingContext}Error occurred during processing commit {commitId}: {e}");
                        CommitResults result = new CommitResults(commitId, SourceResultType.Failure, e.Message);
                        taskResult.AddCommitResult(result);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error($"{loggingContext}Error occurred during Update: {e}");

                return new SourceResultAndCommitsOutputHelper(SourceResultType.Error);
            }

            return taskResult;
        }

        private async Task<CommitResults> ProcessPathsInCommit(
            HttpClient httpClient,
            LoggingContext loggingContext,
            IRepositoryClient mainRepo,
            string commitId,
            Dictionary<string, List<string>> pathsAndDeletedItems)
        {
            bool atLeastOneSuccess = false;
            foreach (var pathAndDeletedItems in pathsAndDeletedItems)
            {
                // Read all the data in path.
                // If we find enough data to derive a merged manifest, update the manifest in the rest source.
                // If no valid merged manifest is found, loop on raw urls looking for id and attempt to delete that id.
                bool notFound = false;
                IReadOnlyList<Octokit.RepositoryContent> filesUnderPath = new List<Octokit.RepositoryContent>();
                try
                {
                    filesUnderPath = await mainRepo.GetRepositoryContentsAsync(
                                            pathAndDeletedItems.Key,
                                            commitId);
                }
                catch (Octokit.NotFoundException)
                {
                    Logger.Info($"{loggingContext}: Content not found in repository.");
                    notFound = true;
                }

                if (notFound || filesUnderPath.Count == 0)
                {
                    // Delete this data.
                    Logger.Info($"{loggingContext}: No files found under {pathAndDeletedItems.Key}. Attempting to delete app.");

                    foreach (var deletedItem in pathAndDeletedItems.Value)
                    {
                        // Download the deleted manifest file.
                        string manifest;
                        try
                        {
                            manifest = await httpClient.GetStringAsync(deletedItem);
                        }
                        catch (Exception e)
                        {
                            Logger.Error($"{loggingContext}Failed to download {deletedItem} for processing.m {e.Message}");
                            continue;
                        }

                        MinManifestInfo manifestInfo = MinManifestInfo.CreateManifestInfoFromString(manifest);

                        HttpResponseMessage deleteMessage;
                        deleteMessage = await AzureFunctionUtils.TriggerAzureFunctionAsync(
                            httpClient,
                            HttpMethod.Delete,
                            RestSourceHelpers.GetRestEndpoint(ApiConstants.AzFuncPackageManifestsEndpoint, manifestInfo.Id),
                            ApiConstants.AzureFunctionHostKey);

                        if (!deleteMessage.IsSuccessStatusCode && deleteMessage.StatusCode != HttpStatusCode.NotFound)
                        {
                            Logger.Warning($"{loggingContext} Error deleting {manifestInfo.Id}. {deleteMessage.StatusCode}");
                            continue;
                        }

                        Logger.Info($"{loggingContext} Successfully deleted {manifestInfo.Id}.");
                        atLeastOneSuccess = true;

                        // All other deleted files here should be the same Id, skip them once we have a success.
                        break;
                    }
                }
                else
                {
                    string mergedManifestFilePath = await ManifestHelpers.CreateMergedManifestFromPath(
                        loggingContext,
                        mainRepo,
                        pathAndDeletedItems.Key,
                        filesUnderPath);

                    if (string.IsNullOrWhiteSpace(mergedManifestFilePath))
                    {
                        // Unable to process the manifest contents.
                        // Skip this commit as we will process these best effort for now.
                        continue;
                    }

                    Manifest manifest = Manifest.CreateManifestFromPath(mergedManifestFilePath);

                    PackageManifest packageManifest = new PackageManifest();
                    try
                    {
                        packageManifest = await RestSourceHelpers.AddOrUpdateRestSource(httpClient, loggingContext, manifest.Id, manifest);
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"{loggingContext}Error adding or updating rest source for {manifest.Id}. {e.Message}");
                        continue;
                    }

                    atLeastOneSuccess = true;
                }
            }

            return atLeastOneSuccess ?
                new CommitResults(commitId, SourceResultType.Success)
                : new CommitResults(commitId, SourceResultType.Failure);
        }

        private Dictionary<string, List<string>> IdentifyUniquePathsAndDeletedItems(LoggingContext loggingContext, Octokit.GitHubCommit commit)
        {
            Dictionary<string, List<string>> pathsAndDeletedItems = new Dictionary<string, List<string>>();
            foreach (var file in commit.Files)
            {
                var status = file.Status;
                var filepath = file.Filename;
                var raw = file.RawUrl;

                // Skip any non yaml paths
                if (!Path.GetExtension(filepath).Equals(".yaml", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Info($"{loggingContext}Skipping non yaml file: {filepath}");
                    continue;
                }

                string parentDirectory = Path.GetDirectoryName(filepath);

                if (!pathsAndDeletedItems.ContainsKey(parentDirectory))
                {
                    pathsAndDeletedItems.Add(parentDirectory, new List<string>());
                }

                // If this is a remove, track the removed metadata
                if (string.Equals(file.Status, ApiConstants.GitHubStatusRemoved))
                {
                    pathsAndDeletedItems[parentDirectory].Add(file.RawUrl);
                }
            }

            return pathsAndDeletedItems;
        }
    }
}
