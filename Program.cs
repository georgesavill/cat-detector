using cat_detector.Classes;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

ConfigureConfiguration(builder.Configuration);

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