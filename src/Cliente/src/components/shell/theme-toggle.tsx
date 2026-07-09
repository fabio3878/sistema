import { Moon, Sun } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { useTheme } from '@/lib/theme'

export function ThemeToggle() {
  const { tema, alternar } = useTheme()
  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <Button variant="ghost" size="icon" onClick={alternar} aria-label="Alternar tema">
          {tema === 'escuro' ? <Sun /> : <Moon />}
        </Button>
      </TooltipTrigger>
      <TooltipContent>Tema {tema === 'escuro' ? 'claro' : 'escuro'}</TooltipContent>
    </Tooltip>
  )
}
