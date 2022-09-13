import { createApp } from 'vue';
import BootstrapVue3 from 'bootstrap-vue-3'
import '@unocss/reset/tailwind.css';
import 'uno.css';
import './styles/base.css';
import { createI18n } from 'vue-i18n';
import { createPinia } from 'pinia'

import VueTermynalPlugin from "@lehoczky/vue-termynal"


import 'bootstrap/dist/css/bootstrap.min.css'
import 'bootstrap-vue-3/dist/bootstrap-vue-3.css'

import App from './App.vue';

// Router
import { Router } from '/@/router';

// i18n
import messages from '@intlify/vite-plugin-vue-i18n/messages';

const i18n = createI18n({
	locale: 'en',
	messages,
});

const pinia = createPinia()
const app = createApp(App);

app.use(VueTermynalPlugin);
app.use(pinia)
app.use(BootstrapVue3)

app.use(i18n);

app.use(Router);

app.mount('#app');