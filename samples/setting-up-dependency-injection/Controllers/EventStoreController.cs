using Microsoft.AspNetCore.Mvc;

namespace setting_up_dependency_injection.Controllers {
	[ApiController]
	[Route("[controller]")]
	public class EventStoreController : ControllerBase {
		#region using-dependency
		private readonly KurrentClient _KurrentClient;

		public EventStoreController(KurrentClient KurrentClient) {
			_KurrentClient = KurrentClient;
		}

		[HttpGet]
		public IAsyncEnumerable<ResolvedEvent> Get() {
			return _KurrentClient.ReadAllAsync(Direction.Forwards, Position.Start);
		}
		#endregion using-dependency
	}
}
