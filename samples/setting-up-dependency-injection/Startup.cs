namespace setting_up_dependency_injection; 

public class Startup {
	public Startup(IConfiguration configuration) => Configuration = configuration;

	public IConfiguration Configuration { get; }

	public void ConfigureServices(IServiceCollection services) {
		services.AddControllers();

		#region setting-up-dependency

		services.AddKurrentClient("esdb://admin:changeit@localhost:2113?tls=false");

		#endregion setting-up-dependency
	}

	public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
		if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

		app.UseHttpsRedirection();
		app.UseRouting();
		app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
	}
}
