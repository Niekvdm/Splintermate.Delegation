const baseUrl = 'https://api2.splinterlands.com';

export async function getCards() {
	const response = await fetch(`${baseUrl}/cards/get_details`);

	return await response.json();
}

export async function getSettings() {
	const response = await fetch(`${baseUrl}/settings`);

	return await response.json();
}

export async function getPlayerCollection(username: string) {
	const response = await fetch(`${baseUrl}/cards/collection/${username}`);

	return (await response.json())?.cards ?? [];
}

export async function getPlayerBalances(username: string) {
	const response = await fetch(
		`${baseUrl}/players/balances?username=${username}`,
	);

	return (await response.json()) ?? [];
}
