<script lang="ts" setup>
import { orderBy } from '../composables/useLinq';
import { useCartStore } from '../stores/cart';
import { convertCardLevel } from '../composables/useSplinterlands';
import { startCardDelegation, getState, cancelAction } from '../services/api';

import { useTheme } from '../composables/useTheme';
import { ref, computed, onMounted, onUnmounted, reactive } from 'vue';

const { isDark } = useTheme();
const container = ref<any>(null);

const store = useCartStore();

const input = reactive({
	player: '',
	type: 1,
});

const loaders = reactive({
	delegation: false,
	busy: false,
});

const data = reactive({
	logs: [] as any[],
});

const props = defineProps({
	collection: {
		default: [],
	},
});

const emit = defineEmits(['close']);

const sortedCards = computed(() => store.getCards.slice().sort(orderBy('id')));

function addPlayer() {
	store.addPlayer(input.player.trim());
	input.player = '';
}

function addPlayers() {
	input.player.split(',').forEach(x => store.addPlayer(x.trim()));
}

async function startDelegation() {
	data.logs = [];
	loaders.delegation = true;
	loaders.busy = true;

	const response = await startCardDelegation({
		cards: store.getCards,
		players: store.getPlayers,
	});

	await stateLoader(true);

	loaders.delegation = false;
}

async function stateLoader(init: boolean = false) {
	if (init) await loadState();

	setTimeout(async () => {
		const state = await loadState();

		if (!state.active) loaders.busy = false;

		if (state.active) stateLoader();
	}, 2500);
}

async function loadState() {
	const response = await getState();

	if (response && response.overallResult) {
		const lines = response.result.logs;

		let timeout = 0;
		for (let index in lines) {
			const number = Number(index);

			if (!data.logs[number]) {
				timeout += 100;

				setTimeout(() => {
					data.logs.push(lines[number]);

					setTimeout(() => {
						scrollToNewLine();
					}, 50);
				}, timeout);
			}
		}
	}

	return response.result;
}

async function cancel() {
	await cancelAction();

	loaders.busy = false;
}

function scrollToNewLine() {
	const containerElement = container.value;

	containerElement.scrollTo({
		top: containerElement.scrollHeight,
		behavior: 'smooth',
	});
}

onMounted(() => {
	document.body.classList.add('modal-open');
});

onUnmounted(() => {
	document.body.classList.remove('modal-open');
});
</script>

<template>
	<div class="cart overflow-auto">
		<NavBar title="Cards to delegate" close @close="$emit('close')" />

		<div class="container mt-4 position-relative">
			<div class="row">
				<div class="col-md-4 offset-md-6 mb-3">
					<b-button
						variant="warning"
						:disabled="
							store.getPlayersCount == 0 ||
							store.getCardsCount == 0
						"
						v-if="!loaders.busy"
						@click="startDelegation"
					>
						Start delegation
					</b-button>

					<b-button variant="danger" v-else @click="cancel">
						Stop
					</b-button>
				</div>

				<v-termynal
					:start-delay="0"
					:line-delay="0"
					:class="{ 'light-termynal': !isDark }"
					v-if="data.logs.length || loaders.busy"
				>
					<div ref="container" class="line-container">
						<vt-text
							v-for="(log, index) in data.logs"
							:key="index"
							:class="{
								'text-warning': log.level == 3,
								'text-danger': log.level >= 4,
							}"
						>
							{{ log.message }}
						</vt-text>
						<vt-spinner
							:duration="9999"
							type="dots"
							v-show="loaders.busy"
						/>
					</div>
				</v-termynal>

				<div class="col-md-8" :class="{ busy: loaders.busy }">
					<div class="table-responsive">
						<table class="table table-condensed table-bg">
							<thead>
								<tr>
									<th>#</th>
									<th>Name</th>
									<th>Level</th>
									<th>Available</th>
									<th></th>
								</tr>
							</thead>
							<tbody>
								<tr
									v-for="(item, index) in sortedCards"
									:key="index"
								>
									<td class="align-middle">
										{{ item.id }}
									</td>
									<td class="align-middle">
										<span
											:class="{
												'text-warning': item.gold,
											}"
											>{{ item.name }}</span
										>
									</td>
									<td class="align-middle">
										{{ item.level }}
									</td>
									<td class="align-middle">
										{{
											props.collection.filter(
												x =>
													x.card_detail_id ==
														item.id &&
													x.gold == item.gold &&
													x.level == item.level &&
													!x.delegated_to &&
													!x.market_listing_type
											).length
										}}
									</td>
									<td class="text-right">
										<b-button
											variant="danger"
											size="sm"
											@click="store.deleteByCard(item)"
										>
											Remove
										</b-button>
									</td>
								</tr>
							</tbody>
						</table>
					</div>
				</div>

				<div
					class="col-md-3 offset-md-1"
					:class="{ busy: loaders.busy }"
				>
					<div class="mb-2 d-flex justify-content-between">
						<template v-if="input.type == 1">
							<span>Player</span>
							<a
								href="#"
								@click="input.type = 2"
								class="dark:text-yellow-500"
							>
								Switch to multiple
								<i class="i-bx-transfer-alt" />
							</a>
						</template>

						<template v-else-if="input.type == 2">
							<span>Players</span>
							<a
								href="#"
								@click="input.type = 1"
								class="dark:text-yellow-500"
							>
								Switch to single
								<i class="i-bx-transfer-alt" />
							</a>
						</template>
					</div>

					<b-input-group prepend="@" v-if="input.type == 1">
						<b-form-input
							v-model="input.player"
							@keyup.enter="addPlayer"
						/>
						<template #append>
							<b-button
								variant="warning"
								size="sm"
								@click="addPlayer"
								:disabled="
									!input.player ||
									input.player.length < 2 ||
									store.playerExists(input.player)
								"
							>
								Insert
							</b-button>
						</template>
					</b-input-group>

					<b-input-group prepend="@" v-else-if="input.type == 2">
						<b-form-textarea
							rows="1"
							v-model="input.player"
							placeholder="player1,player2,player3..."
						/>
						<template #append>
							<b-button
								variant="warning"
								size="sm"
								@click="addPlayers"
								:disabled="
									!input.player || input.player.length < 2
								"
							>
								Insert
							</b-button>
						</template>
					</b-input-group>

					<div class="table-responsive mt-2">
						<table class="table table-condensed table-bg">
							<tbody>
								<tr
									v-for="(item, index) in store.getPlayers"
									:key="index"
								>
									<td class="align-middle">@{{ item }}</td>
									<td class="text-right">
										<b-button
											variant="danger"
											size="sm"
											@click="store.deleteByPlayer(item)"
										>
											Remove
										</b-button>
									</td>
								</tr>
							</tbody>
						</table>
					</div>
				</div>
			</div>
		</div>
	</div>
</template>

<style lang="scss">
.cart {
	position: fixed;
	z-index: 2;
	top: 0;
	height: 100%;
	width: 100%;
	background-color: rgba(0, 0, 0, 0.75);

	.busy {
		opacity: 0.25;
	}
}
</style>
