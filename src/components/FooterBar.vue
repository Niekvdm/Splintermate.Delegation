<script setup lang="ts">
import { useI18n } from 'vue-i18n';

import { computed, ref } from 'vue';
import { useTheme } from '../composables';

const { t, availableLocales, locale } = useI18n();

const toggleLocales = () => {
	const locales = availableLocales;
	locale.value =
		locales[(locales.indexOf(locale.value) + 1) % locales.length];
};

const { toggleDark } = useTheme();

const show = ref(false);

setTimeout(() => {
	show.value = true;
}, 1000);
</script>

<template>
	<footer class="text-center">
		<ul class="flex justify-between w-1/6 mx-auto mb-8 p-0">			
			<li class="cursor-pointer text-2xl">
				<a
					href="#"
					@click="toggleDark"
					class="text-cyan-700 hover:text-cyan-500"
					:title="t('toggle_theme')"
				>
					<i i="ph-sun dark:ph-moon" />
				</a>
			</li>
			<li class="cursor-pointer text-2xl">
				<a
					href="https://github.com/Niekvdm/Splintermate.Delegation"
					rel="noreferrer"
					target="_blank"
					class="footer-link text-cyan-700 hover:text-cyan-500"
					title="Github repo"
				>
					<i class="i-ph-github-logo" />
				</a>
			</li>
		</ul>

		<span class="text-xs">
			{{ t('made_by') }}
			<a
				class="footer-link text-cyan-400 hover:text-cyan-500"
				href="https://github.com/Niekvdm/Splintermate.Delegation"
				rel="noreferrer"
				target="_blank"
			>
				Hackzilla
			</a>
		</span>
	</footer>
</template>