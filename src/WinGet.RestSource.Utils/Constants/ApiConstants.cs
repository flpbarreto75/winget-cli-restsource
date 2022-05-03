// -----------------------------------------------------------------------
// <copyright file="ApiConstants.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.WinGet.RestSource.Utils.Constants
{
    using System;
    using Microsoft.WinGet.RestSource.Utils.Models.Arrays;

    /// <summary>
    /// API Constants.
    /// </summary>
    public class ApiConstants
    {
        /// <summary>
        /// Source Identifier.
        /// </summary>
        public static readonly string SourceIdentifier = ApiConstants.ServerIdentifier;

        /// <summary>
        /// Server Supported Versions.
        /// </summary>
        public static readonly ApiVersions ServerSupportedVersions = new ApiVersions()
        {
            "1.0.0",
            "1.1.0",
        };

        /// <summary>
        /// Gets manifest cache endpoint.
        /// </summary>
        public static readonly string ManifestCacheEndpoint = Environment.GetEnvironmentVariable("ManifestCacheEndpoint");

        /// <summary>
        /// Functions host key.
        /// </summary>
        public static readonly string AzureFunctionHostKey = Environment.GetEnvironmentVariable("FunctionHostKey");

        /// <summary>
        /// PackageManifests endpoint.
        /// </summary>
        public static readonly string AzFuncPackageManifestsEndpoint = Environment.GetEnvironmentVariable("AzFuncPackageManifestsEndpoint");

        /// <summary>
        /// Gets the GitHub token used for Github's GraphQL connection.
        /// Typically, this will be the wingetva account.
        /// </summary>
        public static readonly string GitHubServiceAccountToken = Environment.GetEnvironmentVariable("GitHub_ServiceAccountToken");

        /// <summary>
        /// Gets the GitHub repository that this azure functions should be dealing with.
        /// </summary>
        public static readonly string GitHubRepository = Environment.GetEnvironmentVariable("GitHub_Repository");

        /// <summary>
        /// Github commit status removed.
        /// </summary>
        public static readonly string GitHubStatusRemoved = "removed";

        /// <summary>
        /// Server Supported Versions.
        /// Unsupported package match fields.
        /// TODO: NormalizedPackageNameAndPublisher field support is currently not implemented.
        /// GitHub Issue: https://github.com/microsoft/winget-cli-restsource/issues/59.
        /// </summary>
        public static readonly PackageMatchFields UnsupportedPackageMatchFields = new PackageMatchFields()
        {
            // TODO: Currently the winget client sends this field up despite us reporting it as unsupported, so for compatibility
            // we don't currently return it as unsupported. Above issue needs to be resolved.

            // Enumerations.PackageMatchFields.NormalizedPackageNameAndPublisher,
        };

        /// <summary>
        /// Required package match fields.
        /// </summary>
        public static readonly PackageMatchFields RequiredPackageMatchFields = new PackageMatchFields();

        /// <summary>
        /// Unsupported query parameters.
        /// </summary>
        public static readonly QueryParameters UnsupportedQueryParameters = new QueryParameters();

        /// <summary>
        /// Required query paramters.
        /// </summary>
        public static readonly QueryParameters RequiredQueryParameters = new QueryParameters();

        /// <summary>
        /// Gets server Identifier.
        /// </summary>
        public static string ServerIdentifier => Environment.GetEnvironmentVariable("ServerIdentifier");
    }
}
