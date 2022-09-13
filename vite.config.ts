import {defineConfig} from 'vite';
import Unocss from 'unocss/vite'
import {resolve} from 'pathe';
import vue from '@vitejs/plugin-vue';
import VueI18n from '@intlify/vite-plugin-vue-i18n';
import Components from 'unplugin-vue-components/vite'
import ViteFonts from 'vite-plugin-fonts';
import svgLoader from 'vite-svg-loader';

// https://vitejs.dev/config/
export default defineConfig({
    server: {
        port: 7217,
        https: false,
        strictPort: true,
        // proxy: {
        //     '/api': {
        //         target: 'http://127.0.0.1:5237/',
        //         changeOrigin: true,
        //         secure: false
        //     }
        // }
    },
    resolve: {
        alias: {
            '/@': resolve(__dirname, './src'),
        },
    },
    plugins: [
        vue(),
        // https://github.com/jpkleemans/vite-svg-loader
        svgLoader(),
        // https://github.com/antfu/vite-plugin-components
        Components({
            extensions: ['vue'],
            dts: 'src/components.d.ts',
        }),
        // https://github.com/stafyniaksacha/vite-plugin-fonts#readme
        ViteFonts({
            google: {
                families: ['Open Sans', 'Montserrat', 'Fira Sans'],
            },
        }),

        Unocss({ /* options */}),

        // https://github.com/intlify/vite-plugin-vue-i18n
        VueI18n({
            include: [resolve(__dirname, './locales/**')],
        }),
    ],

    optimizeDeps: {
        include: [
            'vue',
            'vue-router',
            '@vueuse/core',
        ],
        exclude: [
            'vue-demi',
        ],
    },
});