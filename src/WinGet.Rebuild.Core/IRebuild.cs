// -----------------------------------------------------------------------
// <copyright file="IRebuild.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.WinGet.Rebuild.Core
{
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.OWCUtils.Diagnostics;
    using Microsoft.OWCUtils.FunctionInputHelpers;
    using Microsoft.WinGet.RestSource.Utils.ToMoveToUtils;

    /// <summary>
    /// Rebuild interface.
    /// </summary>
    public interface IRebuild
    {
        /// <summary>
        /// Processes a rebuild request.
        /// </summary>
        /// <param name="httpClient">The function's http client.</param>
        /// <param name="operationId">Operation id.</param>
        /// <param name="sasReference">SAS reference to SQLite file.</param>
        /// <param name="type">type of operation performed on the SQLite file.</param>
        /// <param name="loggingContext">Logging context.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task<SourceResultOutputHelper> ProcessRebuildRequestAsync(
            HttpClient httpClient,
            string operationId,
            string sasReference,
            RestSource.Utils.ToMoveToUtils.ReferenceType type,
            LoggingContext loggingContext);
    }
}
