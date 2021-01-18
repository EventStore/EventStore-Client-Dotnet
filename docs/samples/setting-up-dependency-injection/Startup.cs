using System;
using System.Net.Http;
using EventStore.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace setting_up_dependency_injection {
	public class Startup {
		public Startup(IConfiguration configuration) {
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		public void ConfigureServices(IServiceCollection services) {
			services.AddControllers();

			#region setting-up-dependency
			services.AddEventStoreClient(settings => {
				settings.ConnectivitySettings.Address = new Uri("https://localhost:2113");
				settings.DefaultCredentials = new UserCredentials("admin", "changeit");
				settings.CreateHttpMessageHandler = () =>
					new SocketsHttpHandler {
						SslOptions = {
							RemoteCertificateValidationCallback = delegate {
								return true;
							}
						}
					};
			});
			#endregion setting-up-dependency
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
			if (env.IsDevelopment()) {
				app.UseDeveloperExceptionPage();
			}

			app.UseHttpsRedirection();
			app.UseRouting();
			app.UseEndpoints(endpoints => {
				endpoints.MapControllers();
			});
		}
	}
}
