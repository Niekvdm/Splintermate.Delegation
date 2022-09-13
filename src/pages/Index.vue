<script setup lang="ts">
import { ref } from 'vue';

const show = ref(false);

setTimeout(() => {
	show.value = true;
}, 1000);
</script>
<template>
	<div class="container max-w-3xl mx-auto mt-60">
		<Header>
			<img
				v-if="show"
				alt="Splintermate logo"
				class="w-24 mx-auto mb-12"
				src="/logo.svg"
			/>
		</Header>

		<div class="flex gap-4 justify-center mb-8">
			<router-link
				:to="{ name: 'cards' }"
				class="bg-cyan-700 px-7 py-2 text-white w-30 rounded-lg hover:bg-cyan-500 text-center decoration-none"
			>
				Cards
			</router-link>
			<router-link
				:to="{ name: 'tokens' }"
				class="bg-cyan-700 px-7 py-2 text-white w-30 rounded-lg hover:bg-cyan-500 text-center decoration-none"
			>
				Tokens
			</router-link>
		</div>

		<FooterBar />
	</div>
</template>
