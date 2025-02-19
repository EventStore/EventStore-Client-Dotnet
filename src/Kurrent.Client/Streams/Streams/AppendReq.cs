namespace EventStore.Client.Streams {
	partial class AppendReq {
		public AppendReq WithAnyStreamRevision(StreamState expectedState) {
			if (expectedState == StreamState.Any) {
				Options.Any = new Empty();
			} else if (expectedState == StreamState.NoStream) {
				Options.NoStream = new Empty();
			} else if (expectedState == StreamState.StreamExists) {
				Options.StreamExists = new Empty();
			}

			return this;
		}
	}
}
