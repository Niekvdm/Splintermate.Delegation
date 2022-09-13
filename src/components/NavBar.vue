<script lang="ts" setup>
import { ref } from 'vue-demi';
import { useCartStore } from '../stores/cart';

const props = defineProps({
	close: {
		type: Boolean,
		default: false,
	},
	title: {
		type: String,
		default: '',
	},
	noCart: {
		type: Boolean,
		default: false,
	},
});

const emit = defineEmits(['close']);

const store = useCartStore();

const show = ref<boolean>(false);
</script>

<template>
	<div>
		<nav
			class="navbar navbar-expand-lg pt-3 pb-3 border-bottom border-secondary"
		>
			<div class="container">
				<span class="text-black-100 dark:text-yellow-500 text-xl" v-if="!!title" href="#">
					{{ props.title }}
				</span>
	
				<ul class="navbar-nav ms-auto mb-2 mb-lg-0">
					<template v-if="!noCart">
						<li class="nav-item">
							<b-button
								variant="warning"
								size="sm"
								class="position-relative"
								@click="show = !show"
								v-if="!close"
							>
								<i class="i-bi-collection rotate-270" />
								<b-badge variant="danger" text-indicator>
									{{ store.getCardsCount }}
									<span class="visually-hidden">cards</span>
								</b-badge>
							</b-button>
		
							<b-button
								variant="warning"
								size="sm"
								v-else
								@click="$emit('close')"
								>Close</b-button
							>
						</li>
					</template>
				</ul>
			</div>
		</nav>
	
		<Cart v-if="show" @close="show = false" :collection="$attrs.collection" />
	</div>
</template>
