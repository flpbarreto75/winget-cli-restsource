// -----------------------------------------------------------------------
// <copyright file="ContextAndReferenceInputHelper.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.WinGet.RestSource.Utils.ToMoveToUtils
{
    using System;
    using System.Reflection;
    using System.Text;
    using Microsoft.OWCUtils.FunctionInputHelpers;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Type of reference provided.
    /// </summary>
    public enum ReferenceType
    {
        /// <summary>
        /// The reference is being added.
        /// </summary>
        Add,

        /// <summary>
        /// The reference is being modified.
        /// </summary>
        Modify,

        /// <summary>
        /// The reference is being deleted.
        /// </summary>
        Delete,
    }

    /// <summary>
    /// Class that contains context and reference elements for Azure function input.
    /// </summary>
    public class ContextAndReferenceInputHelper : IInputHelper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContextAndReferenceInputHelper"/> class.
        /// </summary>
        /// <param name="operationId">Operation id of the VALIDATION pipeline that validated this commit.</param>
        /// <param name="sasReference">sasReference to the content to be processed.</param>
        /// <param name="referenceType">Type of operation being performed on the reference package.</param>
        public ContextAndReferenceInputHelper(
            string operationId,
            string sasReference,
            ReferenceType referenceType)
        {
            if (string.IsNullOrWhiteSpace(operationId)
                || string.IsNullOrWhiteSpace(sasReference))
            {
                throw new ArgumentNullException("ContextAndReference input is missing required fields.");
            }

            this.OperationId = operationId;
            this.SASReference = sasReference;
            this.ReferenceType = referenceType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextAndReferenceInputHelper"/> class.
        /// </summary>
        [JsonConstructor]
        private ContextAndReferenceInputHelper()
        {
        }

        /// <summary>
        /// Gets a value indicating the operation id of the VALIDATION pipeline that validated this content.
        /// </summary>
        [JsonProperty("OperationId")]
        public string OperationId { get; private set; }

        /// <summary>
        /// Gets a value indicating a sasReference to the content to be processed.
        /// </summary>
        [JsonProperty("sasReference")]
        public string SASReference { get; private set; }

        /// <summary>
        /// Gets a value indicating the reference type of the sasReference.
        /// </summary>
        [JsonProperty("ReferenceType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ReferenceType ReferenceType { get; private set; }

        /// <summary>
        /// Validates all required properties are set.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(this.OperationId)
                || string.IsNullOrWhiteSpace(this.SASReference))
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

                // Skip logging secrets.
                if (!string.Equals(info.Name, "SASReference"))
                {
                    stringBuilder.Append(info.Name + ": '" + value.ToString() + "' ");
                }
            }

            return stringBuilder.ToString();
        }
    }
}
