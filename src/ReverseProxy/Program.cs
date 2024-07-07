var builder = WebApplication.CreateBuilder(args);


//builder.Services.AddDataProtection()
//    .PersistKeysToFileSystem(new DirectoryInfo(@"/keys"))
//    .SetApplicationName("YourAppName");

// Add YARP services
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseRouting();

// Use YARP reverse proxy
app.UseEndpoints(endpoints => {
  endpoints.MapReverseProxy();
});

app.Run();
