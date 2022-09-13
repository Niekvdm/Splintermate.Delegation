import { Ref } from 'vue';
import { useDark } from '@vueuse/core';

export interface ThemeComposition {
  isDark: Ref<boolean>;
  toggleDark: () => void;
}

export function useTheme(): ThemeComposition {
  const isDark = useDark();
  
  const toggleDark = () => {
      isDark.value = !isDark.value;
  };

  return {
    isDark,
    toggleDark
  };
}
