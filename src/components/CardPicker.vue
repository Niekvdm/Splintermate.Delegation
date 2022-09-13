<script lang="ts" setup>
import { computed } from 'vue-demi';
import { convertCardImageUri } from '../composables/useSplinterlands';
import { orderBy } from '../composables/useLinq';
import { useCartStore } from '../stores/cart';
import { onMounted, onUnmounted } from 'vue';

const props = defineProps({
	type: {
		default: null as boolean | null,
	},
	card: {
		default: null as any,
	},
	collection: {
		default: [] as any[],
	},
});

const emit = defineEmits(['close']);

const store = useCartStore();

const filteredCollection = computed(() => {
	const items = [];

	for (let i = 1; i <= 10; i++) {
		const item = props.collection.find(
			x =>
				x.level == i &&
				(props.type != null ? x.gold == props.type : true) &&
				!x.delegated_to &&
				!x.market_listing_type,
		);

		if (item) items.push(item);
	}

	return items.sort(orderBy('level'));
});

function updateStore(card: any) {
	store.addOrDeleteCard(card, props.card);
}

onMounted(() => {
	document.body.classList.add('modal-open');
});

onUnmounted(() => {
	document.body.classList.remove('modal-open');
});
</script>

<template>
	<div class="card-picker">
		<NavBar title="Card picker" close @close="$emit('close')" />

		<div class="container mt-4 overflow-auto position-relative">
			<div class="d-flex gap-4 justify-content-center mt-4">
				<div
					class="card-details cursor-pointer"
					:class="{ selected: store.findByCard(item) }"
					v-for="item in filteredCollection"
					:key="item.uid"
					@click="updateStore(item)"
				>
					<div class="position-relative">
						<b-img
							class="card-image"
							rounded
							:src="convertCardImageUri(item, card)"
						/>

						<b-badge
							variant="warning"
							class="position-absolute top-3 right-2"
						>
							{{
								props.collection.filter(
									x =>
										x.card_detail_id ==
											item.card_detail_id &&
										x.gold == item.gold &&
										x.level == item.level &&
										!x.delegated_to &&
										!x.market_listing_type,
								).length
							}}
						</b-badge>
					</div>
				</div>
			</div>
		</div>
	</div>
</template>

<style lang="scss">
.card-picker {
	position: fixed;
	z-index: 2;
	top: 0;
	height: 100%;
	width: 100%;
	background-color: rgba(0, 0, 0, 0.75);

	.card-image {
		max-height: 250px;
	}

	.card-details {
		padding: 1px 5px;
		background-color: rgba(255, 255, 255, 0.5);
		border-radius: 4px;

		opacity: 0.6;

		&.selected {
			opacity: 1;
			background-color: rgba(0, 255, 0, 0.5);
		}
	}
}
</style>
