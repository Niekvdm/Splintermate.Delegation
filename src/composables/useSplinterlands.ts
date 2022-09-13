const splSettings = {
	xp_levels: [
		[20, 60, 160, 360, 760, 1560, 2560, 4560, 7560],
		[100, 300, 700, 1500, 2500, 4500, 8500],
		[250, 750, 1750, 3750, 7750],
		[1000, 3000, 7000],
	],
	alpha_xp: [20, 100, 250, 1000],
	gold_xp: [250, 500, 1000, 2500],
	beta_xp: [15, 75, 175, 750],
	beta_gold_xp: [200, 400, 800, 2000],
	combine_rates: [
		[1, 5, 14, 30, 60, 100, 150, 220, 300, 400],
		[1, 5, 14, 25, 40, 60, 85, 115],
		[1, 4, 10, 20, 32, 46],
		[1, 3, 6, 11],
	],
	combine_rates_gold: [
		[0, 0, 1, 2, 5, 9, 14, 20, 27, 38],
		[0, 1, 2, 4, 7, 11, 16, 22],
		[0, 1, 2, 4, 7, 10],
		[0, 1, 2, 4],
	],
};

export const convertCardLevel = (settings: any, card: any, details: any) => {
	if(!settings) 
		settings = splSettings;

	if (isNaN(card.xp))
		card.xp = (card.edition == 4 || (details?.tier ?? 0)) == 4 ? 1 : 0;

	if (card.edition == 4 || details?.tier >= 4) {
		const rates = card.gold
			? settings.combine_rates_gold[details.rarity - 1]
			: settings.combine_rates[details.rarity - 1];
		let level = 0;
		for (let i = 0; i < rates.length; i++) {
			if (rates[i] > card.xp) break;
			level++;
		}
		if (card.xp == 0) level = 1;
		return level;
	}
	const levels = settings.xp_levels[details.rarity - 1];
	let level = 0;
	for (let i = 0; i < levels.length; i++) {
		if (card.xp < levels[i]) {
			level = i + 1;
			break;
		}
	}
	if (level == 0) level = levels.length + 1;

	return level;
};

export const convertCardCollectionPower = (
	settings: any,
	card: any,
	cardDetails: any,
) => {
	const a = settings;
	const t = card;
	const e = cardDetails;

	return Math.floor(
		(function (t, e, a) {
			if (!a || !a.dec) return 0;
			let s = 0;
			const i = Math.max(t.xp - (t.alpha_xp || 0), 0);
			const r =
				4 == t.edition || e.tier >= 4
					? a.dec.untamed_burn_rate[e.rarity - 1]
					: a.dec.burn_rate[e.rarity - 1];
			if (t.alpha_xp) {
				const n = a[t.gold ? 'gold_xp' : 'alpha_xp'][e.rarity - 1];
				(s =
					r *
					Math.max((t.gold, t.alpha_xp / n), 1) *
					a.dec.alpha_burn_bonus),
					t.gold && (s *= a.dec.gold_burn_bonus);
			}
			const l =
				a[
					0 == t.edition || (2 == t.edition && e.id < 100)
						? t.gold
							? 'gold_xp'
							: 'alpha_xp'
						: t.gold
						? 'beta_gold_xp'
						: 'beta_xp'
				][e.rarity - 1];
			let o = Math.max(t.gold ? i / l : (i + l) / l, 1);
			(4 == t.edition || e.tier >= 4) && (o = t.xp), t.alpha_xp && o--;
			let c = r * o;
			if (t.gold) {
				const d = e.tier >= 7 ? 'gold_burn_bonus_2' : 'gold_burn_bonus';
				c *= a.dec[d];
			}
			0 == t.edition
				? (c *= a.dec.alpha_burn_bonus)
				: 2 == t.edition && (c *= a.dec.promo_burn_bonus);
			let u = c + s;

			return (
				t.xp >=
					(function (t, e, a, s) {
						if (!s || !s.dec) return 0;
						const i = t.rarity,
							r = t.tier;
						if (4 == e || r >= 4) {
							const n = a
								? s.combine_rates_gold[i - 1]
								: s.combine_rates[i - 1];
							return n[n.length - 1];
						}
						return s.xp_levels[i - 1][
							s.xp_levels[i - 1].length - 1
						];
					})(e, t.edition, t.gold, a) && (u *= a.dec.max_burn_bonus),
				e.tier >= 7 && (u /= 2),
				u
			);
		})(t, e, a),
	);
};

export const convertEditionToString = (edition: string) => {
	switch (Number(edition)) {
		case 0:
			return 'alpha';
		case 1:
			return 'beta';
		case 2:
			return 'promo';
		case 3:
			return 'reward';
		case 4:
			return 'untamed';
		case 5:
			return 'dice';
		case 6:
			return 'gladius';
		case 7:
			return 'chaos';
	}

	return edition;
};

export const convertCardImageUri = (card: any, cardDetails: any) => {
	return `https://d36mxiodymuqjm.cloudfront.net/cards_by_level/${convertEditionToString(
		card.edition,
	)}/${cardDetails?.name}_lv${card.level}${card.gold ? '_gold' : ''}.png`;
};

export const convertToType = (type: string) => {
	switch (type.toLowerCase()) {
		case 'red':
			return 'fire';
		case 'blue':
			return 'water';
		case 'green':
			return 'earth';
		case 'white':
			return 'life';
		case 'black':
			return 'death';
		case 'gray':
			return 'neutral';
		case 'gold':
			return 'dragon';
	}

	return type;
};
