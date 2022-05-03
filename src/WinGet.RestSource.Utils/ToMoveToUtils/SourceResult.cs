// -----------------------------------------------------------------------
// <copyright file="SourceResult.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.WinGet.RestSource.Utils.ToMoveToUtils
{
    /// <summary>
    /// Type of source results.
    /// </summary>
    public enum SourceResultType
    {
        /// <summary>
        /// The source operation was a success.
        /// </summary>
        Success,

        /// <summary>
        /// The source operation was a failure.
        /// </summary>
        Failure,

        /// <summary>
        /// The source operation failed and should not be retried.
        /// </summary>
        FailureNoRetry,

        /// <summary>
        /// The source operation resulted in an error.
        /// </summary>
        Error,
    }
}