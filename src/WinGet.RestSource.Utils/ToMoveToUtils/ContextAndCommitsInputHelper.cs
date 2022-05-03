// -----------------------------------------------------------------------
// <copyright file="ContextAndCommitsInputHelper.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.WinGet.RestSource.Utils.ToMoveToUtils
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using Microsoft.OWCUtils.FunctionInputHelpers;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Class that contains context and commit elements for Azure function input.
    /// </summary>
    public class ContextAndCommitsInputHelper : IInputHelper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContextAndCommitsInputHelper"/> class.
        /// </summary>
        /// <param name="operationId">Operation id.</param>
        /// <param name="commits">List of commits to be processed.</param>
        public ContextAndCommitsInputHelper(
            string operationId,
            List<string> commits)
        {
            this.OperationId = operationId;
            this.Commits = commits;

            this.Validate();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextAndCommitsInputHelper"/> class.
        /// </summary>
        [JsonConstructor]
        private ContextAndCommitsInputHelper()
        {
        }

        /// <summary>
        /// Gets a value indicating the operation id.
        /// </summary>
        [JsonProperty("OperationId")]
        public string OperationId { get; private set; }

        /// <summary>
        /// Gets a value indicating a list of commits to be processed.
        /// </summary>
        [JsonProperty("Commits")]
        public List<string> Commits { get; private set; }

        /// <summary>
        /// Validates all required properties are set.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(this.OperationId))
            {
                throw new ArgumentNullException("Please provide all the required values.");
            }
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
                if (string.Equals(info.Name, "Commits"))
                {
                    if (this.Commits != null)
                    {
                        string result = string.Join(",", this.Commits);
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