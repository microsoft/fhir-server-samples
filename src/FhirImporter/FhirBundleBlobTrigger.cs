// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health
{
    public static class FhirBundleBlobTrigger
    {
        [FunctionName("FhirBundleBlobTrigger")]
        public static async Task Run([BlobTrigger("fhirimport/{name}", Connection = "AzureWebJobsStorage")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            var streamReader = new StreamReader(myBlob);
            var fhirString = await streamReader.ReadToEndAsync();

            JObject bundle;
            JArray entries;
            try
            {
                bundle = JObject.Parse(fhirString);
            }
            catch (JsonReaderException)
            {
                log.LogError("Input file is not a valid JSON document");
                await MoveBlobToRejected(name,log);
                return;
            }

            log.LogInformation("File read");

            try
            {
                bundle = (JObject)FhirImportReferenceConverter.ConvertUUIDs(bundle);
            }
            catch
            {
                log.LogError("Failed to resolve references in doc");
                await MoveBlobToRejected(name, log);
                return;
            }

            try
            {
                entries = (JArray)bundle["entry"];
                if (entries == null)
                {
                    log.LogError("No entries found in bundle");
                    throw new FhirImportException("No entries found in bundle");
                }


                AuthenticationContext authContext;
                ClientCredential clientCredential;
                HttpClient httpClient = new HttpClient();
                AuthenticationResult authResult;

                string authority = System.Environment.GetEnvironmentVariable("Authority");
                string audience = System.Environment.GetEnvironmentVariable("Audience");
                string clientId = System.Environment.GetEnvironmentVariable("ClientId");
                string clientSecret = System.Environment.GetEnvironmentVariable("ClientSecret");
                string fhirServerUrl = System.Environment.GetEnvironmentVariable("fhirServerUrl");

                try
                {
                    authContext = new AuthenticationContext(authority);
                    clientCredential = new ClientCredential(clientId, clientSecret);
                    authResult = authContext.AcquireTokenAsync(audience, clientCredential).Result;
                }
                catch (Exception ee)
                {
                    log.LogCritical(string.Format("Unable to obtain token to access FHIR server in FhirImportService {0}", ee.ToString()));
                    throw;
                }
                
                httpClient.BaseAddress = new Uri(fhirServerUrl);
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + authResult.AccessToken);

                for (int i = 0; i < entries.Count; i++)
                {
                    var entry_json = ((JObject)entries[i])["resource"].ToString();
                    string resource_type = (string)((JObject)entries[i])["resource"]["resourceType"];
                    string id = (string)((JObject)entries[i])["resource"]["id"];

                    if (string.IsNullOrEmpty(entry_json))
                    {
                        log.LogError("No 'resource' section found in JSON document");
                        throw new FhirImportException("'resource' not found or empty");
                    }

                    if (string.IsNullOrEmpty(resource_type))
                    {
                        log.LogError("No resource_type found.");
                        throw new FhirImportException("No resource_type in resource.");
                    }

                    StringContent content = new StringContent(entry_json, Encoding.UTF8, "application/json");
                    HttpResponseMessage uploadResult;
                    if (string.IsNullOrEmpty(id))
                    {
                        uploadResult = await httpClient.PostAsync($"/{resource_type}", content);
                    }
                    else
                    {
                        uploadResult = await httpClient.PutAsync($"/{resource_type}/{id}", content);
                    }

                    if (!uploadResult.IsSuccessStatusCode)
                    {
                        string resultContent = await uploadResult.Content.ReadAsStringAsync();
                        log.LogError(resultContent);
                        throw new FhirImportException("Unable to upload resources to FHIR server");
                    }
                }

                // We are done with this blob, upload was successful, it will be delete
                await GetBlobReference("fhirimport", name, log).DeleteIfExistsAsync();
            }
            catch (FhirImportException)
            {
                await MoveBlobToRejected(name, log);
            }
        }

        private static CloudBlockBlob GetBlobReference(string containerName, string blobName, ILogger log)
        {
            var connectionString = System.Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            CloudStorageAccount storageAccount;
            if (CloudStorageAccount.TryParse(connectionString, out storageAccount))
            {
                try
                {
                    CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
                    var container = cloudBlobClient.GetContainerReference(containerName);
                    var blockBlob = container.GetBlockBlobReference(blobName);
                    return blockBlob;
                }
                catch
                {
                    log.LogCritical("Unable to get blob reference");
                    return null;
                }
            }
            else
            {
                log.LogCritical("Unable to parse connection string and create storage account reference");
                return null;
            }

        }

        private static async Task MoveBlobToRejected(string name, ILogger log)
        {
            CloudBlockBlob srcBlob = GetBlobReference("fhirimport", name, log);
            CloudBlockBlob destBlob = GetBlobReference("fhirrejected", name, log);

            await destBlob.StartCopyAsync(srcBlob);
            await srcBlob.DeleteAsync();
        }
    }
}
