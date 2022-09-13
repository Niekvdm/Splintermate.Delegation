import { defineStore } from 'pinia';

import { convertCardLevel } from '../composables/useSplinterlands';

export type DelegationCard = {
	id: number;
	name: string;
	level: number;
	rarity: number;
	gold: boolean;
};

export type RootState = {
	cards: DelegationCard[];
	players: string[];
};

export const useCartStore = defineStore({
	id: 'cart',
	state: () =>
		({
			cards: JSON.parse(sessionStorage.getItem('cards') || '[]'),
			players: JSON.parse(localStorage.getItem('players') || '[]'),
		} as RootState),

	actions: {
		addCard(item: DelegationCard) {
			if (!item) return;

			this.cards.push(item);
			sessionStorage.setItem('cards', JSON.stringify(this.cards));
		},

		addOrDeleteCard(card: any, details: any) {
			const index = this.findIndexByCard(card);

			if (index === -1) {
				this.addCard({
					id: card.card_detail_id,
					name: details.name,
					rarity: details.rarity,
					level: convertCardLevel(null, card, details),
					gold: card.gold,
				});

				return;
			}

			this.deleteByCard(card);
		},

		addPlayer(name: string) {
			if (!name) return;

			const index = this.players.findIndex(x => x == name);

			if (index !== -1) return;

			this.players.push(name);
			localStorage.setItem('players', JSON.stringify(this.players));
		},

		deleteCard(index: number) {
			if (index === -1) return;

			this.cards.splice(index, 1);
			sessionStorage.setItem('cards', JSON.stringify(this.cards));
		},

		deleteByCard(card: any) {
			const index = this.findIndexByCard(card);

			this.deleteCard(index);
		},

		deleteByPlayer(name: string) {
			if (!name) return;

			const index = this.players.findIndex(x => x == name);

			if (index === -1) return;

			this.players.splice(index, 1);
			localStorage.setItem('players', JSON.stringify(this.players));
		},

		findByCard(card: any) {
			return this.cards.find(
				x =>
					x.id == card.card_detail_id &&
					x.gold == card.gold &&
					x.level == card.level,
			);
		},

		findIndexByCard(card: any) {
			return this.cards.findIndex(
				x =>
					x.id == (card.card_detail_id ?? card.id) &&
					x.gold == card.gold &&
					x.level == card.level,
			);
		},

		playerExists(name: string) {
			const index = this.players.findIndex(x => x == name);

			return index !== -1;
		},
	},
	getters: {
		getCardsCount: state => {
			return state.cards?.length ?? 0;
		},

		getPlayersCount: state => {
			return state.players?.length ?? 0;
		},

		getCards: state => state.cards,

		getPlayers: state => state.players,
	},
});
