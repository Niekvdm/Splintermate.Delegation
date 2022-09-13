<script lang="ts" setup></script>
<template>
	<div class="h-20 mb-8">
		<transition
			enter-active-class="transition ease-out duration-1000 transform"
			enter-from-class="-translate-y-50 opacity-0"
			enter-to-class="translate-y-0 opacity-100"
			leave-active-class="transition ease-in duration-1000 transform"
			leave-from-class="opacity-100"
			leave-to-class="opacity-0"
		>
			<slot></slot>
		</transition>
	</div>
</template>
