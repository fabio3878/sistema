import type { KeyboardEvent } from 'react'

/** Elementos focáveis, em ordem de documento (mesmo conjunto que o Tab percorre). */
export const FOCAVEIS =
  'input:not([disabled]),select:not([disabled]),textarea:not([disabled]),button:not([disabled]),[tabindex]:not([tabindex="-1"])'

/**
 * Enter num campo avança para o próximo (como Tab), em vez de submeter no meio do preenchimento;
 * no ÚLTIMO campo, Enter salva. Concretiza o DESIGN_1 §3 ("teclado e menos cliques; formulários
 * navegáveis por Tab; Enter salva"). Aplicar como `onKeyDown` no elemento `<form>`.
 *
 * Não interfere em: textarea (Enter quebra linha), botões (Select/Combobox/Seletor/qualquer `<button>`
 * tratam o próprio Enter), e quando há um popup Radix aberto (`aria-expanded`), onde Enter escolhe.
 */
export function enterComoTab(e: KeyboardEvent<HTMLFormElement>) {
  if (e.key !== 'Enter' || e.shiftKey || e.ctrlKey || e.metaKey || e.altKey) return
  const alvo = e.target as HTMLElement
  const tag = alvo.tagName
  if (tag === 'TEXTAREA') return // Enter quebra linha
  if (tag === 'BUTTON') return // aciona o botão (inclui gatilhos de Select/Combobox/SeletorCliente)
  if (alvo.getAttribute('aria-expanded') === 'true') return // popup aberto: Enter seleciona a opção
  if (tag !== 'INPUT' && tag !== 'SELECT') return
  const tipo = (alvo as HTMLInputElement).type
  if (tipo === 'submit' || tipo === 'button' || tipo === 'reset') return

  e.preventDefault() // nunca deixa o Enter submeter no meio do formulário
  const form = e.currentTarget
  const itens = Array.from(form.querySelectorAll<HTMLElement>(FOCAVEIS)).filter(
    (el) => el === alvo || el.offsetParent !== null, // só visíveis (offsetParent null = oculto)
  )
  const restantes = itens.slice(itens.indexOf(alvo) + 1)
  // Só é "último campo" (→ salva) quando não há mais nenhum CAMPO DE DADO adiante; botões de ação
  // (ex.: "Regerar") não contam — assim o Enter no último input salva em vez de cair num botão.
  const haCampoAdiante = restantes.some((el) => el.tagName === 'INPUT' || el.tagName === 'SELECT' || el.tagName === 'TEXTAREA')
  const prox = restantes[0]
  if (prox && haCampoAdiante) {
    prox.focus() // avança para o próximo focável (como Tab; inclui gatilhos de Combobox/Select)
    // Deixa o valor pronto para sobrescrever ao digitar (inputs de texto/número).
    if (prox instanceof HTMLInputElement && /^(text|number|search|tel|url|email|password|)$/.test(prox.type)) prox.select()
  } else {
    form.requestSubmit() // último campo de dado → salva (dispara o onSubmit/handleSubmit do RHF)
  }
}
