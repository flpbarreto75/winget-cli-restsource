// -----------------------------------------------------------------------
// <copyright file="ExistsFactory.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.WinGet.Exists.Core
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Helps initialize the Existence checker.
    /// </summary>
    public class ExistsFactory
    {
        /// <summary>
        /// Factory method to initialize existence checker.
        /// </summary>
        /// <returns>An instance of <see cref="Exists"/>.</returns>
        public static Exists InitializeExistsInstance()
        {
            return new Exists();
        }
    }
}
