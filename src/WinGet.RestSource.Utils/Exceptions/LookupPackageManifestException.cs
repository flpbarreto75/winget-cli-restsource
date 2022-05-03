// -----------------------------------------------------------------------
// <copyright file="LookupPackageManifestException.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.WinGet.RestSource.Utils.Exceptions
{
    using System;

    /// <summary>
    /// LookupPackageManifestException.
    /// </summary>
    public class LookupPackageManifestException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LookupPackageManifestException"/> class.
        /// </summary>
        public LookupPackageManifestException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LookupPackageManifestException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public LookupPackageManifestException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LookupPackageManifestException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="inner">Inner exception.</param>
        public LookupPackageManifestException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}