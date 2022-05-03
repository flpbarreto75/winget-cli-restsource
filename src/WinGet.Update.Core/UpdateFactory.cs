// -----------------------------------------------------------------------
// <copyright file="UpdateFactory.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.WinGet.Update.Core
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Helps initialize the updater.
    /// </summary>
    public class UpdateFactory
    {
        /// <summary>
        /// Factory method to initialize update object.
        /// </summary>
        /// <returns>An instance of <see cref="Update"/>.</returns>
        public static Update InitializeUpdateInstance()
        {
            return new Update();
        }
    }
}
