// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace FhirServerSamples.FhirImportService
{
    public class FhirImportReferenceConverter
    {
        public static Dictionary<string, IdTypePair> CreateUUIDLookUpTable(JObject bundle)
        {
            Dictionary<string, IdTypePair> table = new Dictionary<string, IdTypePair>();
            JArray entry = (JArray)bundle["entry"];

            if (entry == null)
            {
                throw new Exception("Unable to find bundle entries for creating lookup table");
            }

            try
            {
                foreach (var resource in entry)
                {
                    var fullUrl = (string)resource["fullUrl"];
                    string resourceType = (string)((JObject)resource)["resource"]["resourceType"];
                    string id = (string)((JObject)resource)["resource"]["id"];
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

        public static JToken ConvertUUIDs(JToken tok, Dictionary<string, IdTypePair> idLookupTable = null)
        {
            if (idLookupTable == null)
            {
                idLookupTable = CreateUUIDLookUpTable((JObject)tok);
            }

            switch (tok.Type)
            {
                case JTokenType.Object:
                    JObject retObject = new JObject();

                    foreach (var c in tok.Children())
                    {
                        retObject.Add(ConvertUUIDs(c, idLookupTable));
                    }

                    return retObject;
                case JTokenType.Array:
                    JArray retArray = new JArray();

                    foreach (var c in tok.Children())
                    {
                        retArray.Add(ConvertUUIDs(c, idLookupTable));
                    }

                    return retArray;
                case JTokenType.Property:
                    JProperty prop = (JProperty)tok;

                    if (prop.Name == "reference")
                    {
                        IdTypePair idTypePair;
                        if (idLookupTable.TryGetValue(prop.Value.ToString(), out idTypePair))
                        {
                            prop.Value = idTypePair.ResourceType + "/" + idTypePair.Id;
                        }
                    }
                    else
                    {
                        prop.Value = ConvertUUIDs(prop.Value, idLookupTable);
                    }

                    return prop;
                case JTokenType.String:
                case JTokenType.Boolean:
                case JTokenType.Float:
                case JTokenType.Integer:
                case JTokenType.Date:
                    return tok;
                default:
                    throw new Exception($"Invalid token type {tok.Type} encountered");
            }
        }

        public class IdTypePair
        {
            public string Id { get; set; }

            public string ResourceType { get; set; }
        }
    }
}