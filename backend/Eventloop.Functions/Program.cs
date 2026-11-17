using Eventloop.Core.Interfaces;
using Eventloop.Infrastructure.Data;
using Eventloop.Infrastructure.Email;
using Eventloop.Infrastructure.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;

        // Application Insights telemetry for Azure Functions isolated worker
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Database context
        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? config["SqlConnectionString"]
            ?? "Server=localhost,1433;Database=EventloopDb;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;MultipleActiveResultSets=true";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Email Provider (Console for local dev, Azure Communication Services for prod)
        var emailProvider = config["Email:Provider"] ?? "Console";
        if (emailProvider.Equals("AzureCommunicationServices", StringComparison.OrdinalIgnoreCase))
        {
            services.Configure<AzureCommunicationServicesEmailOptions>(
                config.GetSection(AzureCommunicationServicesEmailOptions.SectionName));
            services.AddSingleton<IEmailSender, AzureCommunicationServicesEmailSender>();
        }
        else
        {
            services.AddSingleton<IEmailSender, ConsoleEmailSender>();
        }

        // Registration Email Processor
        services.AddScoped<IRegistrationEmailProcessor, RegistrationEmailProcessor>();
    })
    .Build();

host.Run();
