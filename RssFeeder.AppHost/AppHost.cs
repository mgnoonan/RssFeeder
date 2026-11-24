var builder = DistributedApplication.CreateBuilder(args);

var mongo = builder.AddMongoDB("mongo")
    .WithLifetime(ContainerLifetime.Persistent);
    //.WithMongoExpress();

var mongoDb = mongo.AddDatabase("feed-items");

var consoleProjectPath = Path.GetFullPath(
    Path.Combine(builder.AppHostDirectory, "../RssFeeder.Console/RssFeeder.Console.csproj"));
var consoleRepositoryProvider = builder.Configuration["Database:RepositoryProvider"] ?? "MongoDB";
var consoleExportRepositoryProvider =
    builder.Configuration["Database:ExportRepositoryProvider"] ?? "MongoDB";

builder.AddDotnetProject("console", consoleProjectPath, options =>
    options.ExcludeLaunchProfile = true)
    .WithArgs("build", "feed-drudge.json")
    .WithReference(mongoDb)
    .WithEnvironment("Database__RepositoryProvider", consoleRepositoryProvider)
    .WithEnvironment("Database__ExportRepositoryProvider", consoleExportRepositoryProvider)
    .WaitFor(mongoDb)
    .WithExplicitStart();

builder.AddDotnetProject("admin", "../RssFeeder.Admin/RssFeeder.Admin.csproj")
    .WithReference(mongoDb)
    .WaitFor(mongoDb)
    .WithEnvironment("Crawler__ProjectPath", consoleProjectPath)
    .WithEnvironment("ConnectionStrings__feed-items", mongoDb)
    .WithEnvironment("Database__RepositoryProvider", consoleRepositoryProvider)
    .WithEnvironment("Database__ExportRepositoryProvider", consoleExportRepositoryProvider);

builder.Build().Run();
