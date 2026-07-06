using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace BlobArchiveFunctions;
 
public class BlobArchiveService
{
    private readonly ILogger<BlobArchiveService> _logger;
    private readonly string _connectionString;
 
    public BlobArchiveService(ILogger<BlobArchiveService> logger, IConfiguration config)
    {
        _logger = logger;
        _connectionString = config["BlobStorageConnectionString"]
            ?? throw new InvalidOperationException("BlobStorageConnectionString is not configured.");
    }
 
    public async Task<int> ArchiveOldBlobsAsync(int olderThanDays = 7)
    {
        var serviceClient = new BlobServiceClient(_connectionString);
        var uploads = serviceClient.GetBlobContainerClient("uploads");
        var archive = serviceClient.GetBlobContainerClient("archive");
 
        await archive.CreateIfNotExistsAsync();
 
        int count = 0;
        var cutoff = DateTimeOffset.UtcNow.AddDays(-olderThanDays);
 
        await foreach (var blobItem in uploads.GetBlobsAsync())
        {
            if (blobItem.Properties.LastModified < cutoff)
            {
                var sourceBlob = uploads.GetBlobClient(blobItem.Name);
                var destBlob  = archive.GetBlobClient(blobItem.Name);
 
                await destBlob.StartCopyFromUriAsync(sourceBlob.Uri);
                await sourceBlob.DeleteAsync();
 
                _logger.LogInformation("Archived: {BlobName}", blobItem.Name);
                count++;
            }
        }
 
        _logger.LogInformation("Archive run complete. Files moved: {Count}", count);
        return count;
    }
}
