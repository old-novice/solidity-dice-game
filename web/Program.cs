using BCDG;

var reader = new BlockTranRctReader("http://localhost:7545");
var trans = await reader.GetContractTrans("0x844e84C22b141573Ddc9856cfEBaD5D72048BB8c");

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();


app.UseFileServer(new FileServerOptions {
    RequestPath = "",
    FileProvider = new Microsoft.Extensions.FileProviders
                    .ManifestEmbeddedFileProvider(
        typeof(Program).Assembly, "ui"
    ) 
});

app.MapGet("/", () => "Hello World!");

app.Run();
