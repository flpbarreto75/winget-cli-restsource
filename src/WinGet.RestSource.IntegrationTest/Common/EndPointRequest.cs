﻿// -----------------------------------------------------------------------
// <copyright file="EndPointRequest.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.WinGet.RestSource.IntegrationTest.Common
{
    using Xunit.Abstractions;

    /// <summary>
    /// Represents the endpoint request.
    /// </summary>
    public class EndPointRequest : IXunitSerializable
    {
        /// <summary>
        /// Gets or sets the Endpoint url.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the Json file name.
        /// </summary>
        public string JsonFileName { get; set; }

        /// <inheritdoc/>
        public void Deserialize(IXunitSerializationInfo info)
        {
            this.Url = info.GetValue<string>(nameof(this.Url));
            this.JsonFileName = info.GetValue<string>(nameof(this.JsonFileName));
        }

        /// <inheritdoc/>
        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(this.JsonFileName), this.JsonFileName, typeof(string));
            info.AddValue(nameof(this.Url), this.Url, typeof(string));
        }
    }
}
