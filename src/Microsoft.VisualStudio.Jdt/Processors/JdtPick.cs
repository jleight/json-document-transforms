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
        private readonly JdtAttributeValidator attributeValidator;

        /// <summary>
        /// Initializes a new instance of the <see cref="JdtPick"/> class.
        /// </summary>
        public JdtPick()
        {
            this.attributeValidator = new JdtAttributeValidator(JdtAttributes.Path);
        }

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

                case JTokenType.Object:
                    this.PickWithAttributes(source, new[] { (JObject)transformValue }, logger);
                    break;

                case JTokenType.Array:
                    var array = (JArray)transformValue;
                    var first = array.First;

                    switch (first.Type)
                    {
                        case JTokenType.String:
                            this.PickWithStrings(source, array.Select(x => x.ToString()), logger);
                            break;

                        case JTokenType.Object:
                            this.PickWithAttributes(source, array.Select(x => (JObject)x), logger);
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

        private bool PickWithAttributes(JObject source, IEnumerable<JObject> pickObjects, JsonTransformationContextLogger logger)
        {
            var map = new Dictionary<JToken, List<JToken>>();

            foreach (var pickObject in pickObjects)
            {
                var attributes = this.attributeValidator.ValidateAndReturnAttributes(pickObject);

                if (!attributes.TryGetValue(JdtAttributes.Path, out var pathToken))
                {
                    throw JdtException.FromLineInfo(Resources.ErrorMessage_PickAttributes, ErrorLocation.Transform, pickObject);
                }

                if (pathToken.Type != JTokenType.String)
                {
                    throw JdtException.FromLineInfo(Resources.ErrorMessage_PathContents, ErrorLocation.Transform, pathToken);
                }

                var matches = source
                    .SelectTokens(pathToken.ToString())
                    .ToList();

                foreach (var token in matches)
                {
                    if (token.Equals(source))
                    {
                        throw JdtException.FromLineInfo(Resources.ErrorMessage_PathContents, ErrorLocation.Transform, pathToken);
                    }

                    switch (token.Parent.Type)
                    {
                        case JTokenType.Property:
                            if (!map.ContainsKey(token.Parent.Parent))
                            {
                                map[token.Parent.Parent] = new List<JToken>();
                            }

                            map[token.Parent.Parent].Add(token.Parent);
                            break;

                        case JTokenType.Array:
                            if (!map.ContainsKey(token.Parent))
                            {
                                map[token.Parent] = new List<JToken>();
                            }

                            map[token.Parent].Add(token);
                            break;

                        default:
                            throw JdtException.FromLineInfo(Resources.ErrorMessage_PathContents, ErrorLocation.Transform, pathToken);
                    }
                }
            }

            foreach (var pair in map)
            {
                pair.Key
                    .Children()
                    .Except(pair.Value)
                    .ToList()
                    .ForEach(t => t.Remove());
            }

            return true;
        }
    }
}
