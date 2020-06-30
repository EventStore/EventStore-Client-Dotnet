module.exports =[
	{
		title: "Getting started",
		collapsable: true,
		children: [
			"getting-started/introduction",
			"getting-started/installation",
			"getting-started/quick-tour"
		]
	},
	{
		title: "Changelog",
		collapsable: true,
		children: [
			"changelog/todo"
		]
	},
	{
		title: "Connecting to Event Store",
		collapsable: true,
		children: [
			"connecting-to-eventstore/connecting-to-a-single-node",
			"connecting-to-eventstore/connecting-to-a-cluster",
			"connecting-to-eventstore/connecting-to-eventstore-cloud",
			"connecting-to-eventstore/using-a-custom-httpmessagehandler",
			"connecting-to-eventstore/adding-a-grpc-interceptor",
			"connecting-to-eventstore/tls",
			"connecting-to-eventstore/setting-up-a-connection-with-net-dependency-injection"
		]
	},
	{
		title: "Writing events",
		collapsable: true,
		children: [
			"writing-events/basics",
			"writing-events/event-versioning-strategies"
		]
	},
	{
		title: "Reading events",
		collapsable: true,
		children: [
			"reading-events/basics"
		]
	},
	{
		title: "Subscribing to streams",
		collapsable: true,
		children: [
			"subscribing-to-streams/basics",
			"subscribing-to-streams/filtering",
			"subscribing-to-streams/error-handling"
		]
	},
	{
		title: "Projections",
		collapsable: true,
		children: [
			"projections/creating-a-projection"
		]
	},
	{
		title: "Persistent subscriptions",
		collapsable: true,
		children: [
			"persistent-subscriptions/creating-a-persistent-subscription",
			"persistent-subscriptions/subscribing-to-persistent-subscription",
			"persistent-subscriptions/advanced"
		]
	},
	{
		title: "Examples",
		children: [
			"examples/"
		],
		collapsable: true,
	},
	{
		title: "Source code",
		children: [
			"source-code/"
		],
		collapsable: true,
	},
	{
		title: "Issues and help",
		children: [
			"issues-and-help/"
		],
		collapsable: true,
	}
]
