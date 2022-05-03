// -----------------------------------------------------------------------
// <copyright file="RebuildFactory.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.WinGet.Rebuild.Core
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Helps initialize the rebuilder.
    /// </summary>
    public class RebuildFactory
    {
        /// <summary>
        /// Factory method to initialize rebuild object.
        /// </summary>
        /// <returns>An instance of <see cref="Rebuild"/>.</returns>
        public static Rebuild InitializeRebuildInstance()
        {
            return new Rebuild();
        }
    }
}
