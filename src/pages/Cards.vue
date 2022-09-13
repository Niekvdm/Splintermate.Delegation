<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { getPlayerCollection, getCards } from '../services/splinterlands';
import { convertCardImageUri } from '../composables/useSplinterlands';
import { orderBy } from '../composables/useLinq';
import { getAccount } from '../services/api';

let player = ref<string>('');

const selected: any = reactive({
	card: null
});

const loaders: any = reactive({
	collection: false,
});

const filter: any = reactive({
	gold: null,
	summoner: null,
	mode: 1,
	rarities: [],
	editions: [],
	types: [],
	name: {
		filter: '',
		value: '',
	},
});

const modes: any[] = [
	{ value: 1, text: 'Modern' },
	{ value: 0, text: 'Wild' },
];

const data: any = reactive({
	cards: [],
	collection: [],
});

watch(
	() => selected.id,
	(value: number) => {
		if (value > maxCardId.value) {
			selected.id = maxCardId;
		} else if (value < 1) {
			selected.id = 1;
		} else {
			selected.id = Number(value ?? 1);
		}

		selected.name = selectedCardName;
	},
);

watch(
	() => filter.name.value,
	(value: string) => {
		filter.name.filter = value.toLowerCase();
	},
);

const maxCardId = computed(() => {
	return Math.max(...data.cards.map((x: any) => x.id));
});

const selectedCardName = computed(() => {
	return (
		data.cards.find((x: any) => x.id == selected.id)?.name ??
		selected.card?.name ??
		'-'
	);
});

const selectedCardRarity = computed(() => {
	return data.cards.find((x: any) => x.id == selected.id)?.rarity ?? 1;
});

const getCardImageUri = (card: any) => {
	return convertCardImageUri(
		{
			edition: card.editions.split(',')[0],
			level: 1,
			gold: false,
		},
		card,
	);
};

// const selectedCardImageUri = computed(() => {
// 	if (!data.collection.length || !data.cards.length) return null;

// 	const card = data.collection.find(x => x.card_detail_id == selected.id);
// 	const cardDetails = data.cards.find(x => x.id == selected.id);
// 	return convertCardImageUri(card, cardDetails);
// });

const availableCards = computed(() => {
	const distinctIds = [
		...new Set<number>(
			data.collection
				.filter(
					(x: any) =>
						(!x.delegated_to || x.delegated_to != player.value) &&
						!x.market_listing_type,
				)
				.map((x: any) => x.card_detail_id),
		),
	];

	let result = data.cards
		.slice()
		.filter((x: any) => distinctIds.some(d => d == x.id));

	if (filter.gold != null) {
		result = result.filter((x: any) =>
			data.collection.some(
				(c: any) => c.card_detail_id == x.id && c.gold == filter.gold,
			),
		);
	}

	if (filter.summoner != null) {
		result = result.filter(
			(x: any) =>
				(x.type.toLowerCase() == 'summoner' && filter.summoner) ||
				(x.type.toLowerCase() == 'monster' && !filter.summoner),
		);
	}

	if (filter.name.filter.length > 0) {
		result = result.filter(
			(x: any) =>
				x.id == filter.name.filter ||
				x.name.toLowerCase().includes(filter.name.filter),
		);
	}

	if (filter.editions.length > 0) {
		result = result.filter((x: any) =>
			filter.editions.some((e: any) =>
				x.editions.split(',').some((ex: number) => ex == e),
			),
		);
	}

	if (filter.rarities.length > 0) {
		result = result.filter((x: any) =>
			filter.rarities.some((e: any) => e == x.rarity),
		);
	}

	if (filter.types.length > 0) {
		result = result.filter((x: any) =>
			filter.types.some((e: any) => e == x.color.toLowerCase()),
		);
	}

	return result.sort(orderBy('id'));
});

const collectionBasedOnSelection = computed(() => {
	if(selected.card == null) return [];

	return data.collection.filter((x: any) =>
		x.card_detail_id == selected.card.id &&
		(filter.gold != null ? x.gold == filter.gold : true) &&
		(!x.delegated_to || x.delegated_to != player.value) &&
		!x.market_listing_type
	);
});

const loadCollection = async () => {
	loaders.collection = true;

	data.collection = [];

	data.collection = await getPlayerCollection(player.value);

	for (let card of data.cards) {
		card.total = data.collection.filter(
			(x: any) =>
				x.card_detail_id == card.id && x.player == player.value,
		).length;

		card.gold = data.collection.filter(
			(x: any) =>
				x.card_detail_id == card.id &&
				x.gold &&
				!x.delegated_to &&
				x.player == player.value,
		).length;

		card.regular = data.collection.filter(
			(x: any) =>
				x.card_detail_id == card.id &&
				!x.gold &&
				!x.delegated_to &&
				x.player == player.value,
		).length;
	}

	loaders.collection = false;
};

function onCardSelected(card: any) {
	selected.card = card;

}

onMounted(async () => {
	data.cards = await getCards();

	player.value = await getAccount();

	await loadCollection();
});
</script>

<template>
	<NavBar :title="`Delegate cards from @${player}`" :collection="data.collection" />

	<CardPicker :type="filter.gold" :card="selected.card" :collection="collectionBasedOnSelection" v-if="selected.card != null" @close="selected.card = null" />

	<div class="container max-w-3xl mx-auto text-center">
		<div class="d-flex gap-2" >
			<div class="w-100 mt-4">
				<div class="table-responsive">
					<table class="table table-condensed">
						<thead>
							<tr>
								<th>#</th>
								<th>Name</th>
								<th>Total</th>
								<th>Available</th>
							</tr>
						</thead>
						<tbody>
							<tr v-for="item in availableCards" :key="item.id">
								<td>
									{{ item.id }}
								</td>
								<td>
									<div class="d-flex align-items-center gap-2">
										<a @click="onCardSelected(item)" class="cursor-pointer text-cyan-400 hover:text-cyan-700">
											{{ item.name }}
										</a>
										<!-- <img :src=" getCardImageUri(item)" :alt="item.name" class="card-image"> -->
									</div>
								</td>
								<td>
									{{ item.total }}
								</td>
								<td>
									<template v-if="filter.gold == null">{{
										item.regular + item.gold
									}}</template>
									<template v-else-if="filter.gold">{{
										item.gold
									}}</template>
									<template v-else>{{ item.regular }}</template>
								</td>
							</tr>
							<tr v-if="loaders.collection">
								<th colspan="5">Calculating data, please wait...</th>
							</tr>
							<tr v-else-if="!availableCards.length">
								<th colspan="5">Nothing available to delegate...</th>
							</tr>
						</tbody>
					</table>
				</div>
			</div>

			<div class="filter">
				<h6 class="mt-2">@{{ player }}</h6>

				<div class="filter-list mt-1">
					<button
						class="btn btn-warning w-100 text-center"
						@click="loadCollection"
						:disabled="loaders.collection"
					>
						<b-spinner v-if="loaders.collection" small></b-spinner>
						<span v-else>Refresh</span>
					</button>
				</div>

				<h6 class="mt-4">Filter</h6>

				<div class="filter-list mt-1">
					<input
						class="mb-3 form-control"
						placeholder="Card Name / ID"
						v-model.trim="filter.name.value"
					/>

					<div class="d-flex flex-gap-1">
						<label>
							<input
								type="checkbox"
								:checked="filter.gold == false"
								@change="
									$event.target.checked == false
										? (filter.gold = null)
										: true
								"
								@click="filter.gold = false"
								class="mb-3"
								plain
							/>
							Regular
						</label>

						<label>
							<input
								type="checkbox"
								:checked="filter.gold == true"
								@change="
									$event.target.checked == false
										? (filter.gold = null)
										: true
								"
								@click="filter.gold = true"
								class="mb-3"
								plain
							/>
							Gold
						</label>
					</div>

					<div class="d-flex flex-gap-1">
						<label>
							<input
								type="checkbox"
								:checked="filter.summoner == true"
								@change="
									$event.target.checked == false
										? (filter.summoner = null)
										: true
								"
								@click="filter.summoner = true"
								class="mb-3"
								plain
							/>
							Summoner
						</label>

						<label>
							<input
								type="checkbox"
								:checked="filter.summoner == false"
								@change="
									$event.target.checked == false
										? (filter.summoner = null)
										: true
								"
								@click="filter.summoner = false"
								class="mb-3"
								plain
							/>
							Monster
						</label>
					</div>
				</div>

				<h6 class="mt-2">Editions</h6>

				<div class="filter-list mt-1">
					<div
						class="d-flex justify-content-center flex-wrap flex-gap-0-5"
					>
						<label
							class="badge btn-edition rounded-pill bg-secondary"
							:class="{
								'bg-soft': !filter.editions.some(x => x == 0),
							}"
							for="e-alpha"
						>
							<span class="position-absolute font-size-18"
								>A</span
							>
							<input
								type="checkbox"
								id="e-alpha"
								:value="0"
								:true-value="[]"
								v-model="filter.editions"
								class="form-check-dark m-0 mt-1 ms-1 invisible"
								plain
							/>
						</label>

						<label
							class="badge btn-edition rounded-pill bg-secondary"
							:class="{
								'bg-soft': !filter.editions.some(x => x == 1),
							}"
							for="e-beta"
						>
							<span class="position-absolute font-size-18"
								>B</span
							>
							<input
								type="checkbox"
								id="e-beta"
								:value="1"
								:true-value="[]"
								v-model="filter.editions"
								class="form-check-dark m-0 mt-1 ms-1 invisible"
								plain
							/>
						</label>

						<label
							class="badge btn-edition rounded-pill bg-secondary"
							:class="{
								'bg-soft': !filter.editions.some(x => x == 2),
							}"
							for="e-promo"
						>
							<span class="position-absolute font-size-18"
								>P</span
							>
							<input
								type="checkbox"
								id="e-promo"
								:value="2"
								:true-value="[]"
								v-model="filter.editions"
								class="form-check-dark m-0 mt-1 ms-1 invisible"
								plain
							/>
						</label>

						<label
							class="badge btn-edition rounded-pill bg-secondary"
							:class="{
								'bg-soft': !filter.editions.some(x => x == 3),
							}"
							for="e-reward"
						>
							<span class="position-absolute font-size-18"
								>R</span
							>
							<input
								type="checkbox"
								id="e-reward"
								:value="3"
								:true-value="[]"
								v-model="filter.editions"
								class="form-check-dark m-0 mt-1 ms-1 invisible"
								plain
							/>
						</label>

						<label
							class="badge btn-edition rounded-pill bg-secondary"
							:class="{
								'bg-soft': !filter.editions.some(x => x == 4),
							}"
							for="e-untamed"
						>
							<span class="position-absolute font-size-18"
								>U</span
							>
							<input
								type="checkbox"
								id="e-untamed"
								:value="4"
								:true-value="[]"
								v-model="filter.editions"
								class="form-check-dark m-0 mt-1 ms-1 invisible"
								plain
							/>
						</label>

						<label
							class="badge btn-edition rounded-pill bg-secondary"
							:class="{
								'bg-soft': !filter.editions.some(x => x == 5),
							}"
							for="e-dice"
						>
							<span class="position-absolute font-size-18"
								>D</span
							>
							<input
								type="checkbox"
								id="e-dice"
								:value="5"
								:true-value="[]"
								v-model="filter.editions"
								class="form-check-dark m-0 mt-1 ms-1 invisible"
								plain
							/>
						</label>

						<label
							class="badge btn-edition rounded-pill bg-secondary"
							:class="{
								'bg-soft': !filter.editions.some(x => x == 6),
							}"
							for="e-gladius"
						>
							<span class="position-absolute font-size-18"
								>G</span
							>
							<input
								type="checkbox"
								id="e-gladius"
								:value="6"
								:true-value="[]"
								v-model="filter.editions"
								class="form-check-dark m-0 mt-1 ms-1 invisible"
								plain
							/>
						</label>

						<label
							class="badge btn-edition rounded-pill bg-secondary"
							:class="{
								'bg-soft': !filter.editions.some(x => x == 7),
							}"
							for="e-chaos"
						>
							<span class="position-absolute font-size-18"
								>C</span
							>
							<input
								type="checkbox"
								id="e-chaos"
								:value="7"
								:true-value="[]"
								v-model="filter.editions"
								class="form-check-dark m-0 mt-1 ms-1 invisible"
								plain
							/>
						</label>
					</div>
				</div>

				<h6 class="mt-4">Rarities</h6>

				<div class="filter-list mt-1">
					<div class="d-flex justify-content-evenly">
						<label
							class="badge btn-rarity bg-secondary"
							:class="{
								'bg-soft': !filter.rarities.some(x => x == 1),
							}"
							for="c-common"
						>
							<input
								type="checkbox"
								id="c-common"
								:value="1"
								:true-value="[]"
								v-model="filter.rarities"
								class="form-check-dark m-0 mt-1 ms-1 invisible"
								plain
							/>
						</label>

						<label
							class="badge btn-rarity bg-info"
							:class="{
								'bg-soft': !filter.rarities.some(x => x == 2),
							}"
							for="c-rare"
						>
							<input
								type="checkbox"
								id="c-rare"
								:value="2"
								:true-value="[]"
								v-model="filter.rarities"
								class="form-check-dark m-0 mt-1 ms-1 invisible"
								plain
							/>
						</label>

						<label
							class="badge btn-rarity bg-purple"
							:class="{
								'bg-soft': !filter.rarities.some(x => x == 3),
							}"
							for="c-epic"
						>
							<input
								type="checkbox"
								id="c-epic"
								:value="3"
								:true-value="[]"
								v-model="filter.rarities"
								class="form-check-dark m-0 mt-1 ms-1 invisible"
								plain
							/>
						</label>

						<label
							class="badge btn-rarity bg-warning"
							:class="{
								'bg-soft': !filter.rarities.some(x => x == 4),
							}"
							for="c-legendary"
						>
							<input
								type="checkbox"
								id="c-legendary"
								:value="4"
								:true-value="[]"
								v-model="filter.rarities"
								class="form-check-dark m-0 mt-1 ms-1 invisible"
								plain
							/>
						</label>
					</div>
				</div>

				<h6 class="mt-4">Type</h6>

				<div class="filter-list mt-1">
					<div class="d-flex justify-content-evenly">
						<label
							class="badge btn-type bg-secondary p-1"
							:class="{
								'element-soft bg-soft': !filter.types.some(
									x => x == 'red',
								),
							}"
							for="c-fire"
						>
							<img :src="`/images/element-fire.svg`" />
							<input
								type="checkbox"
								id="c-fire"
								value="red"
								:true-value="[]"
								v-model="filter.types"
								class="form-check-dark m-0 mt-1 ms-1 hide"
								plain
							/>
						</label>

						<label
							class="badge btn-type bg-secondary p-1"
							:class="{
								'element-soft bg-soft': !filter.types.some(
									x => x == 'blue',
								),
							}"
							for="c-water"
						>
							<img :src="`/images/element-water.svg`" />
							<input
								type="checkbox"
								id="c-water"
								value="blue"
								:true-value="[]"
								v-model="filter.types"
								class="form-check-dark m-0 mt-1 ms-1 hide"
								plain
							/>
						</label>

						<label
							class="badge btn-type bg-secondary p-1"
							:class="{
								'element-soft bg-soft': !filter.types.some(
									x => x == 'green',
								),
							}"
							for="c-earth"
						>
							<img :src="`/images/element-earth.svg`" />
							<input
								type="checkbox"
								id="c-earth"
								value="green"
								:true-value="[]"
								v-model="filter.types"
								class="form-check-dark m-0 mt-1 ms-1 hide"
								plain
							/>
						</label>

						<label
							class="badge btn-type bg-secondary p-1"
							:class="{
								'element-soft bg-soft': !filter.types.some(
									x => x == 'white',
								),
							}"
							for="c-life"
						>
							<img :src="`/images/element-life.svg`" />
							<input
								type="checkbox"
								id="c-life"
								value="white"
								:true-value="[]"
								v-model="filter.types"
								class="form-check-dark m-0 mt-1 ms-1 hide"
								plain
							/>
						</label>

						<label
							class="badge btn-type bg-secondary p-1"
							:class="{
								'element-soft bg-soft': !filter.types.some(
									x => x == 'black',
								),
							}"
							for="c-death"
						>
							<img :src="`/images/element-death.svg`" />
							<input
								type="checkbox"
								id="c-death"
								value="black"
								:true-value="[]"
								v-model="filter.types"
								class="form-check-dark m-0 mt-1 ms-1 hide"
								plain
							/>
						</label>

						<label
							class="badge btn-type bg-secondary p-1"
							:class="{
								'element-soft bg-soft': !filter.types.some(
									x => x == 'gold',
								),
							}"
							for="c-dragon"
						>
							<img :src="`/images/element-dragon.svg`" />
							<input
								type="checkbox"
								id="c-dragon"
								value="gold"
								:true-value="[]"
								v-model="filter.types"
								class="form-check-dark m-0 mt-1 ms-1 hide"
								plain
							/>
						</label>

						<label
							class="badge btn-type bg-secondary p-1"
							:class="{
								'element-soft bg-soft': !filter.types.some(
									x => x == 'gray',
								),
							}"
							for="c-neutral"
						>
							<img :src="`/images/element-neutral.svg`" />
							<input
								type="checkbox"
								id="c-neutral"
								value="gray"
								:true-value="[]"
								v-model="filter.types"
								class="form-check-dark m-0 mt-1 ms-1 hide"
								plain
							/>
						</label>
					</div>
				</div>
			</div>
		</div>

		<FooterBar />

		<router-link
			:to="{ name: 'home' }"
			class="text-xs text-cyan-600 hover:text-cyan-500 text-center d-block"
		>
			Go Home
		</router-link>
	</div>
</template>

<style lang="scss">
.card-image {
	max-height: 75px;
}

.filter {
	width: 100%;
	padding: 20px;
	border-radius: 5px;
	font-size: 14px;

	.filter-list a.active {
		font-weight: 500;
	}
	.filter-list a {
		display: block;
		line-height: 24px;
		padding: 8px 5px;
	}
}

@media (min-width: 768px) {
	.filter {
		width: 236px;
	}
}

.hide {
	display: none;
}

.btn-type {
	&.element-soft {
		img {
			opacity: 0.5;
		}
	}

	img {
		width: 18px;
		height: 18px;
	}
}

.btn-rarity {
	&.bg-soft {
		opacity: 0.5;
	}
}

.btn-edition {
	text-align: center;

	&.bg-soft {
		opacity: 0.5;
	}

	> span {
		font-family: monospace;
		margin-left: 3px;
	}
}

.font-size-18 {
	font-size: 18px;
}
</style>
