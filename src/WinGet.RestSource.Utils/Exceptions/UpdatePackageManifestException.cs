// -----------------------------------------------------------------------
// <copyright file="UpdatePackageManifestException.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.WinGet.RestSource.Utils.Exceptions
{
    using System;

    /// <summary>
    /// UpdatePackageManifestException.
    /// </summary>
    public class UpdatePackageManifestException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdatePackageManifestException"/> class.
        /// </summary>
        public UpdatePackageManifestException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdatePackageManifestException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public UpdatePackageManifestException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdatePackageManifestException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="inner">Inner exception.</param>
        public UpdatePackageManifestException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}