using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace setting_up_dependency_injection {
	public class Program {
		public static async Task Main(string[] args) {
			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
			await CreateHostBuilder(args).Build().WaitForShutdownAsync(cts.Token);
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder => {
					webBuilder.UseStartup<Startup>();
				});
	}
}
