export const orderBy = (property: string) => {
	const collator = new Intl.Collator(undefined, {
		numeric: true,
		sensitivity: 'base',
	});

	return function (a: any, b: any) {
		const valA = a[property];
		const valB = b[property];

		if (typeof valA === 'string') {
			return collator.compare(valA, valB);
		} else {
			return valA > valB ? 1 : valB > valA ? -1 : 0;
		}
	};
};

export const orderByDescending = (property: string) => {
	const collator = new Intl.Collator(undefined, {
		numeric: true,
		sensitivity: 'base',
	});

	return function (a: any, b: any) {
		const valA = a[property];
		const valB = b[property];

		if (typeof valA === 'string') {
			return collator.compare(valB, valA);
		} else {
			return valA > valB ? -1 : valB > valA ? 1 : 0;
		}
	};
};

export const ucFirst = (value: string) => {
	return value?.charAt(0)?.toUpperCase() + value?.slice(1);
};
