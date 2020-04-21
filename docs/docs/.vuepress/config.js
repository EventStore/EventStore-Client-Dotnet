module.exports = {
    base: "/sdk/dotnet/",
    dest: "public",
    title: "Event Store .NET Client",
    description: "The stream database built for event sourcing",
    plugins: [
        "@vuepress/active-header-links",
        "one-click-copy"
    ],
    themeConfig: {
        logo: "/es-logo.png",
        sidebarDepth: 1,
        searchPlaceholder: "Search...",
        lastUpdated: "Last Updated",
        nav: [
            {text: "Server", link: "https://ecstatic-borg-bc8046.netlify.com/", target: "_self"},
            {text: "Drivers", link: "https://ecstatic-borg-bc8046.netlify.com/drivers/", target: "_self"},
            {text: "Get help", link: "/get-help/"},
        ],
        sidebar: [
            {
                "title": "Getting started",
                collapsable: false,
                children: [
                    "getting-started/introduction",
                    "getting-started/installation",
                    "getting-started/quick-tour"
                ]
            },
            {
                "title": "Changelog",
                "path": "/changelog/",
                collapsable: false
            },
            {
                "title": "Connecting to Event Store",
                collapsable: false,
                children: [
                    "connecting-to-eventstore/basics",
                    "connecting-to-eventstore/connecting-to-a-cluster",
                    "connecting-to-eventstore/tls"
                ]
            },
            {
                "title": "Writing events",
                collapsable: false,
                children: [
                    "writing-events/basics"
                ]
            },
            {
                "title": "Reading events",
                collapsable: false,
                children: [
                    "reading-events/basics"
                ]
            },
            {
                "title": "Subscribing to streams",
                collapsable: false,
                children: [
                    "subscribing-to-streams/basics",
                    "subscribing-to-streams/filtering",
                    "subscribing-to-streams/error-handling"
                ]
            },
            {
                "title": "Projections",
                collapsable: false,
                children: [
                    "projections/creating-a-projection"
                ]
            },
            {
                "title": "Persistent subscriptions",
                collapsable: false,
                children: [
                    "persistent-subscriptions/creating-a-persistent-subscription",
                    "persistent-subscriptions/subscribing-to-persistent-subscription",
                    "persistent-subscriptions/advanced"
                ]
            },
            {
                "title": "Examples",
                "path": "/examples/",
                collapsable: false,
            },
            {
                "title": "Source code",
                "path": "/source-code/",
                collapsable: false,
            },
            {
                "title": "Issues and help",
                "path": "/issues-and-help/",
                collapsable: false,
            }
        ],
    },
    markdown: {
        extendMarkdown: md => {
            md.use(require('markdown-it-vuepress-code-snippet-enhanced'));
            md.use(require("markdown-it-include"));
        }
    }
};
