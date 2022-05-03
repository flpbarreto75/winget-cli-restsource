// -----------------------------------------------------------------------
// <copyright file="SourceResultOutputHelper.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.WinGet.RestSource.Utils.ToMoveToUtils
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Class that contains the overall source result.
    /// </summary>
    public class SourceResultOutputHelper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SourceResultOutputHelper"/> class.
        /// </summary>
        /// <param name="overallResult">Overall result for this source operation.</param>
        public SourceResultOutputHelper(
            SourceResultType overallResult)
        {
            this.OverallResult = overallResult;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceResultOutputHelper"/> class.
        /// </summary>
        [JsonConstructor]
        private SourceResultOutputHelper()
        {
        }

        /// <summary>
        /// Gets a value indicating the overall result of this source operation.
        /// </summary>
        [JsonProperty("OverallResult")]
        [JsonConverter(typeof(StringEnumConverter))]
        public SourceResultType OverallResult { get; private set; }

        /// <summary>
        /// ToString override.
        /// </summary>
        /// <returns>String with properties and values.</returns>
        public override string ToString()
        {
            PropertyInfo[] propertyInfos = this.GetType().GetProperties();

            var stringBuilder = new StringBuilder();
            foreach (PropertyInfo info in propertyInfos)
            {
                var value = info.GetValue(this, null) ?? "null";

                stringBuilder.Append(info.Name + ": '" + value.ToString() + "' ");
            }

            return stringBuilder.ToString();
        }
    }
}
