using cat_detector.Classes;
using cat_detector.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

ConfigureConfiguration(builder.Configuration);
ConfigureServices(builder.Services);

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();


void ConfigureConfiguration(ConfigurationManager configuration)
{
    Console.WriteLine("Configuring configuration");
    ConfigurationOptions configurationOptions = new ConfigurationOptions();
    configuration.GetSection(ConfigurationOptions.Config).Bind(configurationOptions);
}
void ConfigureServices(IServiceCollection services)
{
    Console.WriteLine("Configuring services");
    services.AddTransient<TelegramService>();
    services.AddTransient<ImageService>();
    services.AddSingleton<PredictionService>();
}