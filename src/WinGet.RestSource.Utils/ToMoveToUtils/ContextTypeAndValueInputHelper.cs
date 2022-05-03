// -----------------------------------------------------------------------
// <copyright file="ContextTypeAndValueInputHelper.cs" company="Microsoft Corporation">
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
    /// Type of existence.
    /// </summary>
    public enum ExistsType
    {
        /// <summary>
        /// Check for existence in the url field.
        /// </summary>
        Url,

        /// <summary>
        /// Check for existence in the hash field.
        /// </summary>
        Hash,

        /// <summary>
        /// Check for existence in the packageId field.
        /// </summary>
        PackageId,
    }

    /// <summary>
    /// Class that contains context and commit elements for Azure function input.
    /// </summary>
    public class ContextTypeAndValueInputHelper : IInputHelper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContextTypeAndValueInputHelper"/> class.
        /// </summary>
        /// <param name="operationId">Operation id.</param>
        /// <param name="type">Type of existence to process.</param>
        /// <param name="value">Value to check existence of.</param>
        public ContextTypeAndValueInputHelper(
            string operationId,
            ExistsType type,
            string value)
        {
            this.OperationId = operationId;
            this.Type = type;
            this.Value = value;

            this.Validate();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextTypeAndValueInputHelper"/> class.
        /// </summary>
        [JsonConstructor]
        private ContextTypeAndValueInputHelper()
        {
        }

        /// <summary>
        /// Gets a value indicating the operation id.
        /// </summary>
        [JsonProperty("OperationId")]
        public string OperationId { get; private set; }

        /// <summary>
        /// Gets a value indicating which field we are checking for existence within.
        /// </summary>
        [JsonProperty("Type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ExistsType Type { get; private set; }

        /// <summary>
        /// Gets a value indicating the value we are checking for existence.
        /// </summary>
        [JsonProperty("Value")]
        public string Value { get; private set; }

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

                stringBuilder.Append(info.Name + ": '" + value.ToString() + "' ");
            }

            return stringBuilder.ToString();
        }
    }
}
