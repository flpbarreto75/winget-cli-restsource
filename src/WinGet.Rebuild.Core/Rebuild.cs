// -----------------------------------------------------------------------
// <copyright file="Rebuild.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.WinGet.Rebuild.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Data.Sqlite;
    using Microsoft.Msix.Utils.Logger;
    using Microsoft.OWCUtils.Diagnostics;
    using Microsoft.OWCUtils.FunctionInputHelpers;
    using Microsoft.WinGet.RestSource.Helpers;
    using Microsoft.WinGet.RestSource.PowershellSupport.Helpers;
    using Microsoft.WinGet.RestSource.Utils.Common;
    using Microsoft.WinGet.RestSource.Utils.Constants;
    using Microsoft.WinGet.RestSource.Utils.Exceptions;
    using Microsoft.WinGet.RestSource.Utils.Models.Arrays;
    using Microsoft.WinGet.RestSource.Utils.Models.Core;
    using Microsoft.WinGet.RestSource.Utils.Models.ExtendedSchemas;
    using Microsoft.WinGet.RestSource.Utils.Models.Schemas;
    using Microsoft.WinGet.RestSource.Utils.ToMoveToUtils;
    using Microsoft.WinGetUtil.Models.V1;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Class that contains rebuild operations.
    /// Rebuild reconstructs the rest source backend data to align with the provided sqlite index file.
    /// These are relatively expensive operation that should be used sparingly.
    /// </summary>
    public class Rebuild : IRebuild
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Rebuild"/> class.
        /// </summary>
        internal Rebuild()
        {
        }

        /// <inheritdoc/>
        public async Task<SourceResultOutputHelper> ProcessRebuildRequestAsync(
            HttpClient httpClient,
            string operationId,
            string sasReference,
            RestSource.Utils.ToMoveToUtils.ReferenceType type,
            LoggingContext loggingContext)
        {
            Logger.Info($"{loggingContext}Starting to process rebuild request.");
            SourceResultOutputHelper taskResult;
            PackageManifest packageManifest = new PackageManifest();
            try
            {
                // Download the SQLite index to process.
                string indexPath = await HttpHelpers.DownloadFile(httpClient, sasReference);

                // Load the index
                using (var connection = new SqliteConnection($"Data Source={indexPath}"))
                {
                    connection.Open();

                    var command = connection.CreateCommand();
                    command.CommandText = @"SELECT name, pathpart FROM manifest";

                    using (var reader = command.ExecuteReader())
                    {
                        // Iterate across each package referenced in the index
                        while (reader.Read())
                        {
                            var nameId = reader.GetString(0);
                            var pathPartId = reader.GetString(1);

                            string name = this.ReadNameFromNameId(connection, nameId);
                            string relativePath = this.ReadPathPartFromId(connection, pathPartId, loggingContext);

                            // For each package, download the merged manifest
                            string package;
                            try
                            {
                                package = await httpClient.GetStringAsync(ApiConstants.ManifestCacheEndpoint + relativePath);
                            }
                            catch (Exception e)
                            {
                                // TODO: Better retry/error handling. for now, skip failing apps.
                                // Bug 35680162: [Rest] Rebuild requires retries on error handling and better overall error logging / ICM integration
                                Logger.Error($"{loggingContext}Blob storage failed downloading manifest for {name}. {e.Message} {relativePath}");
                                continue;
                            }

                            Manifest manifest = Manifest.CreateManifestFromString(package);

                            try
                            {
                                packageManifest = await RestSourceHelpers.AddOrUpdateRestSource(httpClient, loggingContext, name, manifest);
                            }
                            catch (Exception e)
                            {
                                Logger.Error($"{loggingContext}Error adding or updating rest source for {name}. {e.Message} {relativePath}");
                                continue;
                            }

                            Logger.Info($"{loggingContext}Updated {packageManifest.PackageIdentifier}.");
                        }
                    }
                }

                taskResult = new SourceResultOutputHelper(SourceResultType.Success);
            }
            catch (Exception e)
            {
                Logger.Error($"{loggingContext}Error occurred during Rebuild: {e} {JsonConvert.SerializeObject(packageManifest)}");

                return new SourceResultOutputHelper(SourceResultType.Error);
            }

            return taskResult;
        }

        private string ReadNameFromNameId(SqliteConnection connection, string nameId)
        {
            var nameCommand = connection.CreateCommand();
            nameCommand.CommandText = @"SELECT name FROM names where rowid = $id";
            nameCommand.Parameters.AddWithValue("$id", nameId);

            using (var reader = nameCommand.ExecuteReader())
            {
                reader.Read();
                var name = reader.GetString(0);
                return name;
            }
        }

        private string ReadPathPartFromId(SqliteConnection connection, string pathPartId, LoggingContext loggingContext)
        {
            var pathPartCommand = connection.CreateCommand();
            pathPartCommand.CommandText = @"SELECT parent, pathpart FROM pathparts where rowid = $id";
            pathPartCommand.Parameters.AddWithValue("$id", pathPartId);

            // There should only be 1 match for a row id.
            // We just grab the first as we wouldn't know what to do with additional matches anyhow.
            using (var reader = pathPartCommand.ExecuteReader())
            {
                if (reader.Read())
                {
                    var parent = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                    var pathPart = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);

                    return (string.IsNullOrWhiteSpace(parent) ? string.Empty : this.ReadPathPartFromId(connection, parent, loggingContext) + @"\") + pathPart;
                }
                else
                {
                    return string.Empty;
                }
            }
        }
    }
}
