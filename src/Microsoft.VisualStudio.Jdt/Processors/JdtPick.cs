// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Jdt
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Represents the Pick transformation
    /// </summary>
    internal class JdtPick : JdtProcessor
    {
        /// <inheritdoc/>
        public override string Verb { get; } = "pick";

        /// <inheritdoc/>
        internal override void Process(JObject source, JObject transform, JsonTransformationContextLogger logger)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (transform == null)
            {
                throw new ArgumentNullException(nameof(transform));
            }

            JToken transformValue;
            if (transform.TryGetValue(this.FullVerb, out transformValue))
            {
                if (!this.Transform(source, transformValue, logger))
                {
                    return;
                }
            }

            this.Successor.Process(source, transform, logger);
        }

        private bool Transform(JObject source, JToken transformValue, JsonTransformationContextLogger logger)
        {
            switch (transformValue.Type)
            {
                case JTokenType.String:
                    this.PickWithStrings(source, new[] { transformValue.ToString() }, logger);
                    break;

                case JTokenType.Array:
                    var array = (JArray)transformValue;
                    var first = array.First;

                    switch (first.Type)
                    {
                        case JTokenType.String:
                            this.PickWithStrings(source, array.Select(x => x.ToString()), logger);
                            break;

                        default:
                            throw JdtException.FromLineInfo(string.Format(Resources.ErrorMessage_InvalidPickValue, transformValue.Type.ToString()), ErrorLocation.Transform, transformValue);
                    }

                    break;

                default:
                    throw JdtException.FromLineInfo(string.Format(Resources.ErrorMessage_InvalidPickValue, transformValue.Type.ToString()), ErrorLocation.Transform, transformValue);
            }

            return true;
        }

        private bool PickWithStrings(JObject source, IEnumerable<string> propertiesToKeep, JsonTransformationContextLogger logger)
        {
            source
                .Properties()
                .Select(p => p.Name)
                .Except(propertiesToKeep)
                .ToList()
                .ForEach(p => source.Remove(p));

            return true;
        }
    }
}
