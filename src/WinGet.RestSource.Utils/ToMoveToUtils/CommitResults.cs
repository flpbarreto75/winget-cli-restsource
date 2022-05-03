// -----------------------------------------------------------------------
// <copyright file="CommitResults.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.WinGet.RestSource.Utils.ToMoveToUtils
{
    using System;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Text;
    using Microsoft.OWCUtils.Helpers.Validation;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Class that contains the commit and result metadata.
    /// </summary>
    public class CommitResults
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommitResults"/> class.
        /// </summary>
        /// <param name="commit">commit in question.</param>
        /// <param name="result">Result of processing the commit.</param>
        /// <param name="metadata">Additional string info about the commit processing.</param>
        public CommitResults(
            string commit,
            SourceResultType result,
            string metadata = "")
        {
            this.Commit = commit;
            this.Result = result;
            this.Metadata = metadata;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommitResults"/> class.
        /// </summary>
        [JsonConstructor]
        private CommitResults()
        {
        }

        /// <summary>
        /// Gets a value indicating the commit in question.
        /// </summary>
        [JsonProperty("Commit")]
        public string Commit { get; private set; }

        /// <summary>
        /// Gets a value indicating the result of processing this commit.
        /// </summary>
        [JsonProperty("Result")]
        [JsonConverter(typeof(StringEnumConverter))]
        public SourceResultType Result { get; private set; }

        /// <summary>
        /// Gets a value indicating metadata associated with the result.
        /// </summary>
        [JsonProperty("Metadata")]
        public string Metadata { get; private set; }

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
