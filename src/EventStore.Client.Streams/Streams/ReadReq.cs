namespace EventStore.Client.Streams;

partial class ReadReq {
	partial class Types {
		partial class Options {
			partial class Types {
				partial class StreamOptions {
					public static StreamOptions FromSubscriptionPosition(
						string streamName,
						FromStream fromStream
					) {
						if (fromStream == FromStream.End)
							return new StreamOptions {
								StreamIdentifier = streamName,
								End              = new Empty()
							};

						if (fromStream == FromStream.Start)
							return new StreamOptions {
								StreamIdentifier = streamName,
								Start            = new Empty()
							};

						return new StreamOptions {
							StreamIdentifier = streamName,
							Revision         = fromStream.ToUInt64()
						};
					}

					public static StreamOptions FromStreamNameAndRevision(
						string streamName,
						StreamPosition streamRevision
					) {
						if (streamName == null) throw new ArgumentNullException(nameof(streamName));

						if (streamRevision == StreamPosition.End)
							return new StreamOptions {
								StreamIdentifier = streamName,
								End              = new Empty()
							};

						if (streamRevision == StreamPosition.Start)
							return new StreamOptions {
								StreamIdentifier = streamName,
								Start            = new Empty()
							};

						return new StreamOptions {
							StreamIdentifier = streamName,
							Revision         = streamRevision
						};
					}
				}

				partial class AllOptions {
					public static AllOptions FromSubscriptionPosition(FromAll position) {
						if (position == FromAll.End)
							return new AllOptions {
								End = new Empty()
							};

						if (position == FromAll.Start)
							return new AllOptions {
								Start = new Empty()
							};

						var (c, p) = position.ToUInt64();

						return new AllOptions {
							Position = new Position {
								CommitPosition  = c,
								PreparePosition = p
							}
						};
					}
				}
			}
		}
	}
}