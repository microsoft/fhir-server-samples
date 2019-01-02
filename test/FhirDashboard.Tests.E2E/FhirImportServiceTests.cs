using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FhirDashboard.Tests.E2E
{
    public class FhirImportServiceTests
    {
        private const string ImportContainerName = "fhirimport";
        private const string RejectContainerName = "fhirrejected";
        private IConfiguration _config;
        public FhirImportServiceTests()
        {
            _config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddUserSecrets<FhirDashboard.Tests.E2E.FhirDashboardTests>()
                .Build();
        }

        [Fact]
        public async Task UploadedTxtFileIsRejected()
        {
            Assert.True(!string.IsNullOrWhiteSpace(_config["DashboardUrl"]));
            Assert.True(!string.IsNullOrWhiteSpace(_config["FhirServerUrl"]));

            Assert.True(await CheckForSiteSuccess(new Uri(_config["DashboardUrl"])));
            Assert.True(await CheckForSiteSuccess(new Uri(_config["FhirServerUrl"].TrimEnd('/') + "/metadata")));

            const string testFileName = "TextFileForRejection.txt";
            await DeleteFileFromRejectContainer(testFileName);
            await UploadTestFileToImport(testFileName);
            Assert.True(await WaitForImportToBeEmpty(30));
            Assert.True(await IsFileInRejectContainer(testFileName));
            await DeleteFileFromRejectContainer(testFileName);
        }

        [Fact]
        public async Task UploadedSingleResourceIsRejected()
        {
            Assert.True(!string.IsNullOrWhiteSpace(_config["DashboardUrl"]));
            Assert.True(!string.IsNullOrWhiteSpace(_config["FhirServerUrl"]));

            Assert.True(await CheckForSiteSuccess(new Uri(_config["DashboardUrl"])));
            Assert.True(await CheckForSiteSuccess(new Uri(_config["FhirServerUrl"].TrimEnd('/') + "/metadata")));

            const string testFileName = "Patient.json";
            await DeleteFileFromRejectContainer(testFileName);
            await UploadTestFileToImport(testFileName);
            Assert.True(await WaitForImportToBeEmpty(30));
            Assert.True(await IsFileInRejectContainer(testFileName));
            await DeleteFileFromRejectContainer(testFileName);
        }

        [Fact]
        public async Task UploadedSyntheaIsNotRejected()
        {
            Assert.True(!string.IsNullOrWhiteSpace(_config["DashboardUrl"]));
            Assert.True(!string.IsNullOrWhiteSpace(_config["FhirServerUrl"]));

            Assert.True(await CheckForSiteSuccess(new Uri(_config["DashboardUrl"])));
            Assert.True(await CheckForSiteSuccess(new Uri(_config["FhirServerUrl"].TrimEnd('/') + "/metadata")));

            const string testFileName = "Patient-Synthea.json";
            await DeleteFileFromRejectContainer(testFileName);
            await UploadTestFileToImport(testFileName);
            Assert.True(await WaitForImportToBeEmpty(30));
            Assert.False(await IsFileInRejectContainer(testFileName));
            await DeleteFileFromRejectContainer(testFileName);
        }

        private static async Task<bool> CheckForSiteSuccess(Uri siteUri, int maxSecondsToWait = 30)
        {
            // We have to make sure the website is up
            // On a fresh deployment it can take time before site is deployed
            var client = new HttpClient();
            var result =  await client.GetAsync(siteUri);
            int waitCount = 0;
            while ((waitCount++ < maxSecondsToWait) && !result.IsSuccessStatusCode)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
                result =  await client.GetAsync(siteUri);
            }

            return result.IsSuccessStatusCode;
        }
        private static string GetEmbeddedStringContent(string embeddedResourceSubNamespace, string fileName)
        {
            string resourceName = $"{typeof(FhirImportServiceTests).Namespace}.{embeddedResourceSubNamespace}.{fileName}";
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private async Task UploadTestFileToImport(string fileName)
        {
            Assert.True(!string.IsNullOrWhiteSpace(_config["StorageAccountConnectionString"]));
            CloudStorageAccount storageAccount;
            Assert.True(CloudStorageAccount.TryParse(_config["StorageAccountConnectionString"], out storageAccount));
            CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer importCloudBlobContainer = cloudBlobClient.GetContainerReference(ImportContainerName);
            CloudBlockBlob destBlob = importCloudBlobContainer.GetBlockBlobReference(fileName);
            await destBlob.UploadTextAsync(GetEmbeddedStringContent("TestFiles", fileName));
        }

        private async Task<bool> WaitForImportToBeEmpty(int maxSecondsToWait)
        {

            Assert.True(!string.IsNullOrWhiteSpace(_config["StorageAccountConnectionString"]));

            CloudStorageAccount storageAccount;
            Assert.True(CloudStorageAccount.TryParse(_config["StorageAccountConnectionString"], out storageAccount));
            CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer importCloudBlobContainer = cloudBlobClient.GetContainerReference(ImportContainerName);
            int secondsWaited = 0;
            while (secondsWaited < maxSecondsToWait)
            {
                var results = await importCloudBlobContainer.ListBlobsSegmentedAsync(null, null);
                if (results.Results.Count() == 0)
                {
                    return true;
                }
                Thread.Sleep(TimeSpan.FromMilliseconds(1000));
                secondsWaited++;
            }
            return false;
        }

        private async Task DeleteFileFromRejectContainer(string fileName)
        {
            Assert.True(!string.IsNullOrWhiteSpace(_config["StorageAccountConnectionString"]));

            CloudStorageAccount storageAccount;
            Assert.True(CloudStorageAccount.TryParse(_config["StorageAccountConnectionString"], out storageAccount));
            CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer rejectCloudBlobContainer = cloudBlobClient.GetContainerReference(RejectContainerName);
            BlobContinuationToken blobContinuationToken = null;
            do
            {
                var results = await rejectCloudBlobContainer.ListBlobsSegmentedAsync(null, blobContinuationToken);

                // Get the value of the continuation token returned by the listing call.
                blobContinuationToken = results.ContinuationToken;
                foreach (IListBlobItem item in results.Results)
                {
                    var blob = item as CloudBlockBlob;
                    Assert.NotNull(blob);
                    if (blob.Name == fileName)
                    {
                        await blob.DeleteAsync();
                    }
                }
            } 
            while (blobContinuationToken != null); 
        }

        private async Task<bool> IsFileInRejectContainer(string fileName)
        {
            Assert.True(!string.IsNullOrWhiteSpace(_config["StorageAccountConnectionString"]));

            CloudStorageAccount storageAccount;
            Assert.True(CloudStorageAccount.TryParse(_config["StorageAccountConnectionString"], out storageAccount));
            CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer rejectCloudBlobContainer = cloudBlobClient.GetContainerReference(RejectContainerName);
            BlobContinuationToken blobContinuationToken = null;
            do
            {
                var results = await rejectCloudBlobContainer.ListBlobsSegmentedAsync(null, blobContinuationToken);

                // Get the value of the continuation token returned by the listing call.
                blobContinuationToken = results.ContinuationToken;
                foreach (IListBlobItem item in results.Results)
                {
                    var blob = item as CloudBlockBlob;
                    Assert.NotNull(blob);
                    if (blob.Name == fileName)
                    {
                        return true;
                    }
                }
            } 
            while (blobContinuationToken != null);

            return false;
        }

    }
}
