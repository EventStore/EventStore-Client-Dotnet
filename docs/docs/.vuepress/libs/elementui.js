const container = require('markdown-it-container');

const getAttributes = (info, mark) => {
    return info
        // sanitize input
        .trim()
        .slice(mark.length)
        .trim()
}

const components = [
    'el-collapse-transition', 'el-pagination',      'el-dialog',
    'el-autocomplete',        'el-dropdown',        'el-dropdown-menu',
    'el-dropdown-item',       'el-menu',            'el-submenu',
    'el-menu-item',           'el-menu-item-group', 'el-input',
    'el-input-number',        'el-radio',           'el-radio-group',
    'el-radio-button',        'el-checkbox',        'el-checkbox-button',
    'el-checkbox-group',      'el-switch',          'el-select',
    'el-option',              'el-option-group',    'el-button',
    'el-button-group',        'el-table',           'el-table-column',
    'el-date-picker',         'el-time-select',     'el-time-picker',
    'el-popover',             'el-tooltip',         'el-breadcrumb',
    'el-breadcrumb-item',     'el-form',            'el-form-item',
    'el-tabs',                'el-tab-pane',        'el-tag',
    'el-tree',                'el-alert',           'el-slider',
    'el-icon',                'el-row',             'el-col',
    'el-upload',              'el-progress',        'el-spinner',
    'el-badge',               'el-card',            'el-rate',
    'el-steps',               'el-step',            'el-carousel',
    'el-scrollbar',           'el-carousel-item',   'el-collapse',
    'el-collapse-item',       'el-cascader',        'el-color-picker',
    'el-transfer',            'el-container',       'el-header',
    'el-aside',               'el-main',            'el-footer',
    'el-timeline',            'el-timeline-item',   'el-link',
    'el-divider',             'el-image',           'el-calendar',
    'el-backtop',             'el-page-header',     'el-cascader-panel',
    'el-avatar',              'el-drawer',          'el-popconfirm'
]

module.exports = function tabsPlugin(md, options = {}) {
    options = options || {};

    const registerCtn = mark => {
        md.use(container, mark, {
            render(tokens, idx) {
                const token = tokens[idx];
                const attributes = getAttributes(token.info, mark);
                if (token.nesting === 1) {
                    return `<${mark} ${attributes}>\n`;
                } else {
                    return `</${mark}>\n`;
                }
            }
        })
    }

    components.forEach(mark => registerCtn(mark))
};
