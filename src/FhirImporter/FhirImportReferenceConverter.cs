// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Microsoft.Health
{
    public class FhirImportReferenceConverter
    {
        public static void ConvertUUIDs(JObject bundle)
        {
            ConvertUUIDs(bundle, CreateUUIDLookUpTable(bundle));
        }

        private static void ConvertUUIDs(JToken tok, Dictionary<string, IdTypePair> idLookupTable)
        {
            switch (tok.Type)
            {
                case JTokenType.Object:
                case JTokenType.Array:

                    foreach (var c in tok.Children())
                    {
                        ConvertUUIDs(c, idLookupTable);
                    }

                    return;
                case JTokenType.Property:
                    JProperty prop = (JProperty)tok;

                    if (prop.Value.Type == JTokenType.String && 
                        prop.Name == "reference" && 
                        idLookupTable.TryGetValue(prop.Value.ToString(), out var idTypePair))
                    {
                        prop.Value = idTypePair.ResourceType + "/" + idTypePair.Id;
                    }
                    else
                    {
                        ConvertUUIDs(prop.Value, idLookupTable);
                    }

                    return;
                case JTokenType.String:
                case JTokenType.Boolean:
                case JTokenType.Float:
                case JTokenType.Integer:
                case JTokenType.Date:
                    return;
                default:
                    throw new NotSupportedException($"Invalid token type {tok.Type} encountered");
            }
        }

        private static Dictionary<string, IdTypePair> CreateUUIDLookUpTable(JObject bundle)
        {
            Dictionary<string, IdTypePair> table = new Dictionary<string, IdTypePair>();
            JArray entry = (JArray)bundle["entry"];

            if (entry == null)
            {
                throw new ArgumentException("Unable to find bundle entries for creating lookup table");
            }

            try
            {
                foreach (var resourceWrapper in entry)
                {
                    var resource = resourceWrapper["resource"];
                    var fullUrl = (string)resourceWrapper["fullUrl"];
                    var resourceType = (string)resource["resourceType"];
                    var id = (string)resource["id"];

                    table.Add(fullUrl, new IdTypePair { ResourceType = resourceType, Id = id });
                }
            }
            catch
            {
                Console.WriteLine("Error parsing resources in bundle");
                throw;
            }

            return table;
        }

        private class IdTypePair
        {
            public string Id { get; set; }

            public string ResourceType { get; set; }
        }
    }
}