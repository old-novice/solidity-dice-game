using BCDG;
using Microsoft.EntityFrameworkCore;

var contractAddress = "0x844e84C22b141573Ddc9856cfEBaD5D72048BB8c";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=dicegame.db"));

var scope = builder.Services.BuildServiceProvider().CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
dbContext.Database.EnsureCreated();
builder.Services.AddSingleton<DataSyncer>(new DataSyncer(dbContext));

var app = builder.Build();

app.UseFileServer(new FileServerOptions {
    RequestPath = "",
    FileProvider = new Microsoft.Extensions.FileProviders
                    .ManifestEmbeddedFileProvider(
        typeof(Program).Assembly, "ui"
    ) 
});

if (!string.IsNullOrEmpty(contractAddress))
{
    app.Services.GetRequiredService<DataSyncer>().StartSync(contractAddress);
}


app.MapGet("/", () => "Hello World!");

app.MapGet("/history", (AppDbContext dbContext) =>
{
    return dbContext.BlockTxs.ToList();
}).Produces<List<BlockChainTrans>>();

app.Run();
