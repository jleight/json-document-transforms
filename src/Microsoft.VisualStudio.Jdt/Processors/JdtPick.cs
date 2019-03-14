// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Jdt
{
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Represents the Pick transformation
    /// </summary>
    internal class JdtPick : JdtArrayProcessor
    {
        /// <inheritdoc/>
        public override string Verb { get; } = "pick";

        /// <inheritdoc/>
        protected override bool ProcessCore(JObject source, JToken transformValue, JsonTransformationContextLogger logger)
        {
            var toKeep = new HashSet<string>();

            switch (transformValue.Type)
            {
                case JTokenType.String:
                    toKeep.Add(transformValue.ToString());
                    break;

                case JTokenType.Array:
                    transformValue.ToArray().Select(n => n.ToString()).ToList().ForEach(n => toKeep.Add(n));
                    break;

                default:
                    throw JdtException.FromLineInfo(string.Format(Resources.ErrorMessage_InvalidPickValue, transformValue.Type.ToString()), ErrorLocation.Transform, transformValue);
            }

            source
                .Properties()
                .Select(p => p.Name)
                .Except(toKeep)
                .ToList()
                .ForEach(p => source.Remove(p));

            return true;
        }
    }
}
