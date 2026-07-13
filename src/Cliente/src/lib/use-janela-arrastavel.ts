import { useEffect, useRef, useState } from 'react'

/**
 * Arraste de janela modal pela barra de título (estilo sistema operacional). As janelas são
 * centralizadas por `left-1/2 top-1/2` + `transform: translate(-50%, -50%)`; este hook devolve o
 * `estilo` a aplicar no conteúdo (compõe a centralização com o deslocamento acumulado) e os handlers
 * da barra de título. Sem dependências — só pointer events nativos.
 *
 * @param aberto     enquanto false/ao reabrir, a posição volta ao centro.
 * @param chaveReset qualquer valor que, ao mudar, também recentra (ex.: alternar maximizado no drawer).
 */
export function useJanelaArrastavel(aberto: boolean, chaveReset?: unknown) {
  const alvoRef = useRef<HTMLDivElement>(null)
  const [pos, setPos] = useState({ x: 0, y: 0 })
  const arrasto = useRef<{ px: number; py: number; ox: number; oy: number; rect: DOMRect | null } | null>(null)

  // Reseta ao (re)abrir/fechar e quando a chave muda — a janela sempre reabre centralizada.
  useEffect(() => {
    setPos({ x: 0, y: 0 })
  }, [aberto, chaveReset])

  const onPointerDown = (e: React.PointerEvent) => {
    if (e.button !== 0) return
    // Não sequestrar cliques em controles (X, maximizar, restaurar, links, inputs).
    if ((e.target as HTMLElement).closest('button,a,input,select,textarea,[role="button"]')) return
    e.preventDefault()
    // Captura o retângulo UMA vez, no início do arraste (corresponde ao deslocamento ox/oy). Medir de
    // novo a cada movimento realimentaria os limites com a posição já aplicada → a janela tremeria.
    const rect = alvoRef.current?.getBoundingClientRect() ?? null
    arrasto.current = { px: e.clientX, py: e.clientY, ox: pos.x, oy: pos.y, rect }
    document.body.style.userSelect = 'none'

    const mover = (ev: PointerEvent) => {
      const d = arrasto.current
      if (!d) return
      let x = d.ox + (ev.clientX - d.px)
      let y = d.oy + (ev.clientY - d.py)
      // Clamp com base no rect FIXO do início: o rect no deslocamento (x,y) é `rect` transladado por
      // (x-ox, y-oy). Limites derivados disso são estáveis durante todo o arraste (sem tremor).
      const r = d.rect
      if (r) {
        const margem = 80 // px da janela que devem permanecer visíveis lateralmente
        const topo = 8 // o topo (barra de título) nunca some acima do viewport
        const base = 40 // ao menos a barra de título fica acessível acima do rodapé
        const minX = d.ox + (margem - r.right) // arrastando p/ a esquerda: mantém `margem` à direita
        const maxX = d.ox + (window.innerWidth - margem - r.left) // p/ a direita
        const minY = d.oy + (topo - r.top) // p/ cima: topo não passa de `topo`
        const maxY = d.oy + (window.innerHeight - base - r.top) // p/ baixo: barra fica alcançável
        x = Math.min(Math.max(x, minX), maxX)
        y = Math.min(Math.max(y, minY), maxY)
      }
      setPos({ x, y })
    }

    const soltar = () => {
      arrasto.current = null
      document.body.style.userSelect = ''
      window.removeEventListener('pointermove', mover)
      window.removeEventListener('pointerup', soltar)
    }

    window.addEventListener('pointermove', mover)
    window.addEventListener('pointerup', soltar)
  }

  return {
    alvoRef,
    estilo: {
      transform: `translate(calc(-50% + ${pos.x}px), calc(-50% + ${pos.y}px))`,
    } as React.CSSProperties,
    // Duplo-clique na barra recentra a janela (toque OS-like).
    propsBarra: { onPointerDown, onDoubleClick: () => setPos({ x: 0, y: 0 }) },
  }
}
