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
                "title": "Introduction",
                "path": "/introduction/",
                collapsable: false
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
