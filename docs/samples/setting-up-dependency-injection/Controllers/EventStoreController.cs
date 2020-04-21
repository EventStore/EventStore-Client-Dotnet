using System.Collections.Generic;
using EventStore.Client;
using Microsoft.AspNetCore.Mvc;

namespace setting_up_dependency_injection.Controllers {
	[ApiController]
	[Route("[controller]")]
	public class EventStoreController : ControllerBase {

		#region using-dependency
		private readonly EventStoreClient _eventStoreClient;

		public EventStoreController(EventStoreClient eventStoreClient) {
			_eventStoreClient = eventStoreClient;
		}

		[HttpGet]
		public IAsyncEnumerable<ResolvedEvent> Get() {
			return _eventStoreClient.ReadAllAsync(Direction.Forwards, Position.Start);
		}
		#endregion using-dependency
	}
}
