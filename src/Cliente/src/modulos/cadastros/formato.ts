/** Formatação (exibição + máscara de digitação) e validação. Backend guarda só dígitos. */

const digitos = (v: string) => (v ?? '').replace(/\D/g, '')

/** Aplica uma máscara com '#' como placeholder de dígito (ex.: "###.###.###-##"). */
function aplicar(mascara: string, valor: string): string {
  const d = digitos(valor)
  let out = ''
  let i = 0
  for (const ch of mascara) {
    if (i >= d.length) break
    if (ch === '#') out += d[i++]
    else out += ch
  }
  return out
}

// ---- Exibição ----
export function formatarDocumento(doc: string): string {
  const d = digitos(doc)
  if (d.length === 11) return aplicar('###.###.###-##', d)
  if (d.length === 14) return aplicar('##.###.###/####-##', d)
  return doc
}

export function formatarTelefone(tel: string | null | undefined): string {
  if (!tel) return ''
  const d = digitos(tel)
  if (d.length === 11) return aplicar('(##) #####-####', d)
  if (d.length === 10) return aplicar('(##) ####-####', d)
  return tel
}

// ---- Máscaras de digitação (progressivas) ----
export function mascararCpf(v: string) {
  return aplicar('###.###.###-##', v)
}
export function mascararCnpj(v: string) {
  return aplicar('##.###.###/####-##', v)
}
export function mascararDocumento(v: string, tipo: 'Fisica' | 'Juridica') {
  return tipo === 'Fisica' ? mascararCpf(v) : mascararCnpj(v)
}
export function mascararTelefone(v: string) {
  const d = digitos(v)
  return d.length > 10 ? aplicar('(##) #####-####', d) : aplicar('(##) ####-####', d)
}
export function mascararCep(v: string) {
  return aplicar('#####-###', v)
}

// ---- Validação (dígito verificador; espelha o domínio) ----
export function validarCpf(v: string): boolean {
  const c = digitos(v)
  if (c.length !== 11 || /^(\d)\1{10}$/.test(c)) return false
  const dv = (ate: number, pesoInicial: number) => {
    let soma = 0
    for (let i = 0; i < ate; i++) soma += Number(c[i]) * (pesoInicial - i)
    const r = soma % 11
    return r < 2 ? 0 : 11 - r
  }
  return dv(9, 10) === Number(c[9]) && dv(10, 11) === Number(c[10])
}

export function validarCnpj(v: string): boolean {
  const c = digitos(v)
  if (c.length !== 14 || /^(\d)\1{13}$/.test(c)) return false
  const p1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]
  const p2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]
  const dv = (pesos: number[]) => {
    let soma = 0
    for (let i = 0; i < pesos.length; i++) soma += Number(c[i]) * pesos[i]
    const r = soma % 11
    return r < 2 ? 0 : 11 - r
  }
  return dv(p1) === Number(c[12]) && dv(p2) === Number(c[13])
}

export const soDigitos = digitos
