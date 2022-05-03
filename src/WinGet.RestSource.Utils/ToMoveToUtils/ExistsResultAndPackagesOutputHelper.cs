// -----------------------------------------------------------------------
// <copyright file="ExistsResultAndPackagesOutputHelper.cs" company="Microsoft Corporation">
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
    /// Type of results.
    /// </summary>
    public enum ExistsResultType
    {
        /// <summary>
        /// The content exists in the repository.
        /// </summary>
        Exists,

        /// <summary>
        /// The content does not exist in the repository.
        /// </summary>
        DoesNotExist,

        /// <summary>
        /// The Exists operation resulted in an error.
        /// </summary>
        Error,
    }

    /// <summary>
    /// Class that contains the overall result and a list of packages for which the requested values exist within.
    /// </summary>
    public class ExistsResultAndPackagesOutputHelper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExistsResultAndPackagesOutputHelper"/> class.
        /// </summary>
        /// <param name="overallResult">Overall result for this validation operation.</param>
        public ExistsResultAndPackagesOutputHelper(
            ExistsResultType overallResult)
        {
            this.OverallResult = overallResult;
            this.PackageIds = new List<string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExistsResultAndPackagesOutputHelper"/> class.
        /// </summary>
        [JsonConstructor]
        private ExistsResultAndPackagesOutputHelper()
        {
        }

        /// <summary>
        /// Gets a value indicating the overall result of this validation operation.
        /// </summary>
        [JsonProperty("OverallResult")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ExistsResultType OverallResult { get; private set; }

        /// <summary>
        /// Gets a value indicating a list of packageIds in which the query exists.
        /// </summary>
        [JsonProperty("PackageIds")]
        public List<string> PackageIds { get; private set; }

        /// <summary>
        /// Adds a packageId to the list of packageIds to return.
        /// </summary>
        /// <param name="packageId">packageId to add to the output.</param>
        public void AddPackageId(string packageId)
        {
            this.PackageIds.Add(packageId);
        }

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

                // Log list in detail
                if (string.Equals(info.Name, "PackageIds"))
                {
                    if (this.PackageIds != null)
                    {
                        string result = string.Join(",", this.PackageIds);
                        stringBuilder.Append(info.Name + ": '" + result + "' ");
                    }
                }
                else
                {
                    stringBuilder.Append(info.Name + ": '" + value.ToString() + "' ");
                }
            }

            return stringBuilder.ToString();
        }
    }
}