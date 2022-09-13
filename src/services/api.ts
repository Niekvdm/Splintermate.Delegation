const baseUrl = 'http://127.0.0.1:5237/api';

export async function getAccount() {
	const response = await fetch(`${baseUrl}/account`);

	return (await response.json())?.result;
}

export async function startCardDelegation(body: any) {
	const response = await fetch(`${baseUrl}/cards/delegate`, {
		method: 'POST',
		body: JSON.stringify(body),
		headers: {
			Accept: 'application/json',
			'Content-Type': 'application/json',
		},
	});

	return await response.json();
}

export async function startTokenTransfer(body: any) {
	const response = await fetch(`${baseUrl}/tokens/transfer`, {
		method: 'POST',
		body: JSON.stringify(body),
		headers: {
			Accept: 'application/json',
			'Content-Type': 'application/json',
		},
	});

	return await response.json();
}

export async function cancelAction() {
	const response = await fetch(`${baseUrl}/cancel`);

	return await response.json();
}


export async function getState() {
	const response = await fetch(`${baseUrl}/state`);

	return await response.json();
}
