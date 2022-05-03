// -----------------------------------------------------------------------
// <copyright file="IUpdate.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.WinGet.Update.Core
{
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.OWCUtils.Diagnostics;
    using Microsoft.OWCUtils.FunctionInputHelpers;
    using Microsoft.WinGet.RestSource.Utils.ToMoveToUtils;

    /// <summary>
    /// Update interface.
    /// </summary>
    public interface IUpdate
    {
        /// <summary>
        /// Processes an update request.
        /// </summary>
        /// <param name="httpClient">The Function's http client.</param>
        /// <param name="operationId">Operation id.</param>
        /// <param name="commits">List of commits to process.</param>
        /// <param name="loggingContext">Logging context.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task<SourceResultAndCommitsOutputHelper> ProcessUpdateRequestAsync(
            HttpClient httpClient,
            string operationId,
            List<string> commits,
            LoggingContext loggingContext);
    }
}
