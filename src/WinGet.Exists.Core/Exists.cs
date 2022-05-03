// -----------------------------------------------------------------------
// <copyright file="Exists.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.WinGet.Exists.Core
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
    using Microsoft.OWCUtils.Models.V1;
    using Microsoft.WinGet.RestSource.Utils.ToMoveToUtils;

    /// <summary>
    /// Class that contains exists operations.
    /// </summary>
    public class Exists : IExists
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Exists"/> class.
        /// </summary>
        internal Exists()
        {
        }

        /// <inheritdoc/>
        // async Task<bool>
        public ExistsResultAndPackagesOutputHelper ProcessExistsRequestAsync(
            HttpClient httpClient,
            string operationId,
            ExistsType type,
            string value,
            LoggingContext loggingContext)
        {
            Logger.Info($"{loggingContext}Starting to process exists request.");
            ExistsResultAndPackagesOutputHelper taskResult;

            try
            {
                // TODO: This feature is not yet implemented.
                taskResult = new ExistsResultAndPackagesOutputHelper(ExistsResultType.DoesNotExist);
            }
            catch (Exception e)
            {
                Logger.Error($"{loggingContext}Error occurred during Exists: {e}");

                return new ExistsResultAndPackagesOutputHelper(ExistsResultType.Error);
            }

            return taskResult;
        }
    }
}
