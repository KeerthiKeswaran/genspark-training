using Azure.Identity;
using Azure.Storage.Blobs;


var builder = WebApplication.CreateBuilder(args);

// Force the application to bind to Azure's container port environment variable
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

 
var keyVaultUri = new Uri($"https://{builder.Configuration["KeyVaultName"]}.vault.azure.net/");
builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
 
builder.Services.AddSingleton(x =>
{
    var connStr = builder.Configuration["BlobStorageConnectionString"];
    return new BlobServiceClient(connStr);
});

 
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
 
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthorization();
app.MapControllers();
app.Run();
