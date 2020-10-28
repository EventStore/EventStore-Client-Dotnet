module.exports = [
	{
		title: "Getting started",
		collapsable: true,
		path: "getting-started/",
		children: [
			"getting-started/",
			"getting-started/installation",
			// "getting-started/quick-tour"
		]
	},
	// {
	// 	title: "Changelog",
	// 	collapsable: false
	// },
	{
		title: "Connecting to EventStoreDB",
		collapsable: true,
		path: "connecting/",
		children: [
			"connecting/",
			"connecting/single-node",
			"connecting/cluster",
			"connecting/es-cloud",
			"connecting/custom-httpmessagehandler",
			"connecting/grpc-interceptor",
			// "connecting/tls",
			"connecting/di-extensions"
		]
	},
	{
		title: "Authentication",
		collapsable: true,
		path: "authentication/"
		children: [
			"authentication/"
		]
	},
	{
		title: "Writing events",
		collapsable: true,
		path: "writing-events/",
		children: [
			"writing-events/",
			"writing-events/event-versioning-strategies"
		]
	},
	{
		title: "Reading events",
		collapsable: true,
		path: "reading-events/",
		children: [
			"reading-events/",
			"reading-events/reading-from-a-stream",
			"reading-events/reading-from-the-all-stream"
		]
	},
	{
		title: "Subscribing to streams",
		collapsable: true,
		path: "subscribing-to-streams/",
		children: [
			"subscribing-to-streams/",
			"subscribing-to-streams/filtering",
			// "subscribing-to-streams/error-handling"
		]
	},
	// {
	// 	title: "Projections",
	// 	collapsable: true,
	// 	children: [
	// 		"projections/creating-a-projection"
	// 	]
	// },
	// {
	// 	title: "Persistent subscriptions",
	// 	collapsable: true,
	// 	children: [
	// 		"persistent-subscriptions/creating-a-persistent-subscription",
	// 		"persistent-subscriptions/subscribing-to-persistent-subscription",
	// 		"persistent-subscriptions/advanced"
	// 	]
	// },
	// {
	// 	title: "Examples",
	// 	collapsable: false,
	// },
	// {
	// 	title: "Source code",
	// 	collapsable: false,
	// },
	// {
	// 	title: "Issues and help",
	// 	collapsable: true,
	// }
]
