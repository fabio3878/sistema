import { QueryClient } from '@tanstack/react-query'

/** Cliente único do TanStack Query (dados do servidor). DESIGN_1 §4. */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
})
