import { createRouter, createWebHistory } from 'vue-router';

const routes = [
	{
		path: '/',
		name: 'home',
		component: () => import('/@/pages/Index.vue'),
	},
	{
		path: '/cards',
		name: 'cards',
		component: () => import('/@/pages/Cards.vue'),
	},
	{
		path: '/tokens',
		name: 'tokens',
		component: () => import('/@/pages/Tokens.vue'),
	}
];

export const Router = createRouter({
	scrollBehavior: () => ({ left: 0, top: 0 }),
	history: createWebHistory(),
	routes,
});
