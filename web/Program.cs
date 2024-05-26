using BCDG;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.VisualBasic;

var builder = WebApplication.CreateBuilder(args);

var dataFolderPath = Path.Combine(builder.Environment.ContentRootPath, "data");
var dbPath = Path.Combine(dataFolderPath, "dicegame.db");
AppConstants.Load(builder.Configuration, dataFolderPath);
// if (!AppConstants.ContractSet)
// {
//     AppConstants.Set("0x643ed5b879f3B346422cDDc5460E491bb09d4055", "0x844e84C22b141573Ddc9856cfEBaD5D72048BB8c");
// }

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

var scope = builder.Services.BuildServiceProvider().CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
dbContext.Database.EnsureCreated();
builder.Services.AddSingleton<DataSyncer>(new DataSyncer(dbContext));

var app = builder.Build();

//app.UseFileServer(new FileServerOptions {
//    RequestPath = "",
//    FileProvider = new Microsoft.Extensions.FileProviders
//                    .ManifestEmbeddedFileProvider(
//        typeof(Program).Assembly, "ui"
//    ) 
//});
app.UseFileServer(new FileServerOptions
{
    RequestPath = "",
    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "ui"))
});

if (AppConstants.ContractSet)
{
    app.Services.GetRequiredService<DataSyncer>().StartSync();
}

app.MapGet("/", () => Results.Redirect("/index.html"));

app.MapGet("/contract", () => new { 
    AppConstants.ContractAddress, 
    AppConstants.DealerAddress,
    providerUrl = AppConstants.RpcUrl
});
app.MapPost("/contract", ([FromBody]System.Text.Json.JsonElement param) =>
{
    var contractAddress = param.GetProperty("contractAddress").GetString();
    var dealerAddress = param.GetProperty("dealerAddress").GetString();
    AppConstants.Set(dealerAddress, contractAddress);
    app.Services.GetRequiredService<DataSyncer>().StartSync();
    return Results.Ok();
}).DisableAntiforgery();
app.MapGet("/history", (AppDbContext dbContext) =>
{
    return dbContext.BlockTxs.ToList();
});
app.MapGet("/sync", (DataSyncer dataSyncer) =>
{
    dataSyncer.Sync(null!);
    return Results.Ok();
});
//server sent events
app.MapGet("/sse", async (HttpContext ctx, DataSyncer dataSyncer) =>
{
    var response = ctx.Response!;
    response.Headers.Append("Content-Type", "text/event-stream");
    response.Headers.Append("Cache-Control", "no-cache");
    response.Headers.Append("Connection", "keep-alive");
    var writer = new StreamWriter(response.Body);
    int i = 0;
    await writer.FlushAsync();
    while (!response.HasStarted)
    {
        await Task.Delay(100);
    }
    while (true) 
    {
        await writer.WriteLineAsync($"data: {dataSyncer.MaxBlockNumber.Replace("0x", "")}\n\n");
        await writer.FlushAsync();
        await Task.Delay(1000);
    }
});

app.Run();
