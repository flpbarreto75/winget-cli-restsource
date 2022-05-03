// -----------------------------------------------------------------------
// <copyright file="SourceResultAndCommitsOutputHelper.cs" company="Microsoft Corporation">
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
    /// Class that contains the overall source result and the result of each commit that was processed.
    /// </summary>
    public class SourceResultAndCommitsOutputHelper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SourceResultAndCommitsOutputHelper"/> class.
        /// </summary>
        /// <param name="overallResult">Overall result for this source operation.</param>
        public SourceResultAndCommitsOutputHelper(
            SourceResultType overallResult)
        {
            this.OverallResult = overallResult;
            this.CommitResults = new List<CommitResults>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceResultAndCommitsOutputHelper"/> class.
        /// </summary>
        [JsonConstructor]
        private SourceResultAndCommitsOutputHelper()
        {
        }

        /// <summary>
        /// Gets a value indicating the overall result of this source operation.
        /// </summary>
        [JsonProperty("OverallResult")]
        [JsonConverter(typeof(StringEnumConverter))]
        public SourceResultType OverallResult { get; private set; }

        /// <summary>
        /// Gets a value indicating a list of Commits that failed or have additional metadata to return.
        /// </summary>
        [JsonProperty("CommitResults")]
        public List<CommitResults> CommitResults { get; private set; }

        /// <summary>
        /// Validates adds a commit to the list of commit data to return.
        /// </summary>
        /// <param name="commitResult">CommitResults to add to the output.</param>
        public void AddCommitResult(CommitResults commitResult)
        {
            this.CommitResults.Add(commitResult);
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
                if (string.Equals(info.Name, "CommitResults"))
                {
                    if (this.CommitResults != null)
                    {
                        string result = string.Join(",", this.CommitResults);
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
