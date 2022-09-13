<script setup lang="ts">
import { watch, onMounted, reactive, ref } from 'vue';
import { useTheme } from '../composables';
import {
	cancelAction,
	getAccount,
	getState,
	startTokenTransfer,
} from '../services/api';
import { getPlayerBalances } from '../services/splinterlands';
import { useCartStore } from '../stores/cart';
const { isDark } = useTheme();

const store = useCartStore();

let player = ref<string>('');
const container = ref<any>(null);

const input = reactive({
	token: 'DEC',
	threshold: 0,
	quantity: 0,
	mode: 'topup',
	player: '',
	type: 1,
});

const data = reactive({
	logs: [] as any[],
	balances: [] as any[],
});

const loaders = reactive({
	transfer: false,
	busy: false,
});

function addPlayer() {
	store.addPlayer(input.player.trim());
	input.player = '';
}

function addPlayers() {
	input.player.split(',').forEach(x => store.addPlayer(x.trim()));
}

async function startTransfer() {
	data.logs = [];
	loaders.transfer = true;
	loaders.busy = true;

	const response = await startTokenTransfer({
		token: input.token,
		threshold: input.threshold,
		quantity: input.quantity,
		mode: input.mode,
		players: store.getPlayers,
	});

	await stateLoader(true);

	loaders.transfer = false;
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

onMounted(async () => {
	player.value = await getAccount();
	data.balances = await getPlayerBalances(player.value);
});
</script>
<template>
	<NavBar :title="`Transfer tokens from @${player}`" no-cart />

	<div class="container max-w-3xl mx-auto mt-4 tokens">
		<div class="row mb-3">
			<div class="col-md-4 offset-md-4 mb-3">
				<b-button
					variant="warning"
					class="w-100"
					:disabled="
						store.getPlayersCount == 0 ||
						input.token == null ||
						input.quantity <= 0
					"
					v-if="!loaders.busy"
					@click="startTransfer"
				>
					Start transfer
				</b-button>

				<b-button variant="danger" v-else @click="cancel" class="w-100">
					Stop
				</b-button>
			</div>

			<div
				class="col-md-12 mb-3 d-flex justify-content-center"
				v-if="data.logs.length || loaders.busy"
			>
				<v-termynal
					:start-delay="0"
					:line-delay="0"
					:class="{ 'light-termynal': !isDark }"
					class="w-100"
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
							:key="'loader'"
						/>
					</div>
				</v-termynal>
			</div>

			<div class="col-md-4 offset-md-4" :class="{ busy: loaders.busy }">
				<b-form-group label="Mode">
					<b-form-select v-model="input.mode">
						<option value="threshold">Threshold</option>
						<option value="topup">Top-up</option>
					</b-form-select>
				</b-form-group>

				<b-form-group
					:label="`Token (${
						data?.balances?.find(x => x.token == input.token)
							?.balance ?? 0
					} ${input.token})`"
				>
					<b-form-select v-model="input.token">
						<option value="DEC">DEC</option>
						<!-- <option value="SPS">SPS</option> -->
					</b-form-select>
				</b-form-group>

				<b-form-group
					label="Threshold"
					v-if="input.mode == 'threshold'"
				>
					<b-form-input
						type="number"
						min="0"
						v-model="input.threshold"
					/>
				</b-form-group>

				<b-form-group label="Quantity">
					<b-form-input
						type="number"
						min="0"
						v-model="input.quantity"
					/>
				</b-form-group>
			</div>

			<div class="col-md-3 offset-md-1" :class="{ busy: loaders.busy }">
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
							:disabled="!input.player || input.player.length < 2"
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
.tokens {
	.busy {
		opacity: 0.25;
	}
}
</style>
