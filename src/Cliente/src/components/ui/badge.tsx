import * as React from 'react'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from '@/lib/utils'

/** Badge — usa as cores de estado com a variante tint de fundo (DESIGN_1 §2.1). */
const badgeVariants = cva(
  'inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-caption font-medium',
  {
    variants: {
      tom: {
        neutro: 'bg-black/5 text-fg-muted dark:bg-white/10',
        success: 'bg-success-bg text-success',
        warning: 'bg-warning-bg text-warning',
        danger: 'bg-danger-bg text-danger',
        info: 'bg-info-bg text-info',
      },
    },
    defaultVariants: { tom: 'neutro' },
  },
)

export interface BadgeProps
  extends React.HTMLAttributes<HTMLSpanElement>,
    VariantProps<typeof badgeVariants> {}

export function Badge({ className, tom, ...props }: BadgeProps) {
  return <span className={cn(badgeVariants({ tom }), className)} {...props} />
}
