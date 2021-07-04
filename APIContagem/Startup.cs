using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using MassTransit;
using MassTransit.KafkaIntegration;
using APIContagem.Models;

namespace APIContagem
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "APIContagem", Version = "v1" });
            });

            
            services.AddMassTransit(bus =>
            {
                bus.UsingInMemory((context,cfg) => cfg.ConfigureEndpoints(context));

                bus.AddRider(rider =>
                {
                    rider.AddProducer<IResultadoContador>(
                        Configuration["ApacheKafka:Topic"],
                        (_, kafka) =>
                        {
                            kafka.Partitioner = Confluent.Kafka.Partitioner.Murmur2Random;
                        });

                    if (string.IsNullOrWhiteSpace(Configuration["ApacheKafka:Password"]))
                        rider.UsingKafka(
                            (_, kafka) =>
                            {
                                kafka.Host(Configuration["ApacheKafka:Host"]);
                            });
                    else
                        rider.UsingKafka(
                            new ()
                            {
                                SecurityProtocol = Confluent.Kafka.SecurityProtocol.SaslSsl,
                                SaslMechanism = Confluent.Kafka.SaslMechanism.Plain,
                                SaslUsername = Configuration["ApacheKafka:Username"],
                                SaslPassword = Configuration["ApacheKafka:Password"]
                            },
                            (_, kafka) =>
                            {
                                kafka.Host(Configuration["ApacheKafka:Host"]);
                            });
                });
            });

            services.AddMassTransitHostedService();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "APIContagem v1"));

            app.UseCors(builder => builder.AllowAnyMethod()
                                          .AllowAnyOrigin()
                                          .AllowAnyHeader());

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}