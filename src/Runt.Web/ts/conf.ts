require.config({
    paths: {
        react: '//cdnjs.cloudflare.com/ajax/libs/react/0.10.0/react-with-addons',
        'orion': '/lib/orion.client/bundles/org.eclipse.orion.client.core/web/orion',
        'orion/editor': '/lib/orion.client/bundles/org.eclipse.orion.client.editor/web/orion/editor',
        'webtools': '/lib/orion.client/bundles/org.eclipse.orion.client.webtools/web/webtools',
        'orion/webui': '/lib/orion.client/bundles/org.eclipse.orion.client.ui/web/orion/webui',

        'i18n': '/lib/orion.client/bundles/org.eclipse.orion.client.core/web/requirejs/i18n'
    },

    deps: ['app']
});