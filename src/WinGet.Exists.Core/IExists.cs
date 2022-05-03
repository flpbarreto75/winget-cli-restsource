// -----------------------------------------------------------------------
// <copyright file="IExists.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.WinGet.Exists.Core
{
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.OWCUtils.Diagnostics;
    using Microsoft.OWCUtils.FunctionInputHelpers;
    using Microsoft.WinGet.RestSource.Utils.ToMoveToUtils;

    /// <summary>
    /// Exists interface.
    /// </summary>
    public interface IExists
    {
        /// <summary>
        /// Processes an exists request.
        /// </summary>
        /// <param name="httpClient">http client.</param>
        /// <param name="operationId">Operation id.</param>
        /// <param name="type">field to check.</param>
        /// <param name="value">value to check for.</param>
        /// <param name="loggingContext">Logging context.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        ExistsResultAndPackagesOutputHelper ProcessExistsRequestAsync(
            HttpClient httpClient,
            string operationId,
            ExistsType type,
            string value,
            LoggingContext loggingContext);
    }
}
