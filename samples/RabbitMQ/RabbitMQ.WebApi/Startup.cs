using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using RabbitMQ.MessageBus;
using RabbitMQ.WebApi.MessageHandlers;
using RabbitMQ.WebApi.Services;
using tomware.Microbus.Core;

namespace RabbitMQ.WebApi
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddRouting(o => o.LowercaseUrls = true);

      // Swagger
      services.AddSwaggerGen(c =>
      {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
          Version = "v1",
          Title = "API Documentation",
          Description = "API Documentation"
        });
      });

      // Services
      services.AddSingleton<IRabbitMQPersistentConnection, DefaultRabbitMQPersistentConnection>();
      services.Configure<RabbitMQMessageBusConfiguration>(opt =>
      {
        opt.ClientName = "WebApi";
        opt.QueueName = "tw.WebApi";
        opt.BrokerName = "tw.messages";
        opt.BrokerStrategy = "fanout";
        opt.ConnectionString = "host=localhost;username=guest;password=guest";
        opt.RetryCount = 5;
      });
      services.AddSingleton<IMessageBus, RabbitMQMessageBus>();
      services.AddTransient<DispatchMessageHandler>();

      services.AddTransient<IDispatchService, DispatchService>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(
      IApplicationBuilder app,
      IWebHostEnvironment env,
      IHostApplicationLifetime appLifetime
    )
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }

      // Subscribing MessageHandlers
      app.UseMessageHandlers();
      appLifetime.ApplicationStopping.Register(() => OnShutdown(app));

      app.UseRouting();

      app.UseSwagger();
      app.UseSwaggerUI(c =>
      {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Modular API V1");
      });

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers();
      });
    }

    private void OnShutdown(IApplicationBuilder app)
    {
      var messageBus = app.ApplicationServices.GetRequiredService<IMessageBus>();
      if (messageBus != null)
      {
        ((RabbitMQMessageBus)messageBus).Dispose();
      }
    }
  }
}
