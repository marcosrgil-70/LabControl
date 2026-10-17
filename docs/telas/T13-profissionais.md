# T13 — Profissionais (Funcionários) e vínculo com Usuário

**Fase:** 3 · **Rota:** `/Profissionais` · **Permissão:** `PROFISSIONAIS.*`
**Legado:** `un_Entidades` em modo `teFUNC` (aba *Dados Funcionários*: nº do registro, tipo de registro, cargo). `USUARIO.ID_ENTIDADES_FUNC` ligava o usuário ao funcionário. A assinatura digital (`ENTIDADES_FUNC_ASSINATURAS`) nunca foi usada.
**Tabelas:** `ENTIDADES` (`TIPO_FUNCIONARIO = 1`, categoria F), `ENTIDADES_FUNCIONARIOS`, `ENTIDADES_EMAILS/FONES`, `USUARIOS_EMPRESAS`. View: `VW_PROFISSIONAIS`.

## Referências cruzadas
- **Delphi:** `Standards/un_Entidades.pas` em modo `teFUNC` (menu *Cadastros → Funcionários*)
- **Objetos do banco:** [`ENTIDADES`](../banco/objetos/tabelas/ENTIDADES.md), [`ENTIDADES_FUNCIONARIOS`](../banco/objetos/tabelas/ENTIDADES_FUNCIONARIOS.md), [`USUARIOS_EMPRESAS`](../banco/objetos/tabelas/USUARIOS_EMPRESAS.md), [`VW_PROFISSIONAIS`](../banco/objetos/views/VW_PROFISSIONAIS.md), [`TG_ENTFUN_BI`](../banco/objetos/triggers/TG_ENTFUN_BI.md), [`CARGOS_FUNCIONARIOS`](../banco/objetos/tabelas/CARGOS_FUNCIONARIOS.md), [`TIPOS_REG_PROFISSIONAL`](../banco/objetos/tabelas/TIPOS_REG_PROFISSIONAL.md)
- **Processos:** [P02 Autenticação e permissões](../processos/P02-autenticacao-permissoes.md) · [P17 Emissão do boletim](../processos/P17-emissao-boletim.md)
- **Regras de negócio:** [RN-03 Cadastros](../regras/RN-03-cadastros.md) · [RN-02 Segurança](../regras/RN-02-seguranca.md)
- **Plano:** [00-PLANO-EXECUCAO.md](../00-PLANO-EXECUCAO.md)

## Campos
| Campo | Coluna | Regra |
|---|---|---|
| Nome*, Sobrenome | `ENTIDADES.NOME`, `NOME_FANTASIA` | o boletim imprime nome + sobrenome |
| CPF | `CPF_CNPJ` | único por empresa |
| Cargo | `ID_CARGOS_FUNCIONARIOS` | lookup T10 (+); impresso no boletim |
| Tipo de registro / Nº / UF | `ID_TIPOS_REG_PROFISSIONAL`, `NR_REGISTRO_PROFISSIONAL`, `UF_REGISTRO` | obrigatórios se "Assina boletim" |
| **Assina boletim** | `ASSINA_BOLETIM` | só esses aparecem em Responsável 1/2 do boletim |
| **Executa análise** | `EXECUTA_ANALISE` | só esses aparecem como analista nos resultados |
| Assinatura (imagem) | `ASSINATURA`, `ASSINATURA_MIME`, `ASSINATURA_HASH` | PNG fundo transparente, até 300 KB; impressa no PDF do boletim emitido |
| E-mail / telefone | abas reaproveitadas da T12 | — |
| **Usuário do sistema** | `USUARIOS_EMPRESAS.ID_ENTIDADES_FUNC` | combo com usuários da empresa **sem** profissional; um profissional ↔ um usuário por empresa |
| Inativo | `INATIVO` | inativo some dos combos; usuário vinculado perde o "profissional ativo" |

## Regras
- O profissional ativo do usuário logado é o **digitador** de amostras/resultados e o **gerador** do boletim; o recebimento de amostras exige profissional.
- Responsável pelo recebimento (amostra), analista (resultado) e responsáveis do boletim são escolhidos entre os profissionais ativos com as flags acima.

## Aceite
- [ ] Profissional com "Assina boletim" exige registro.
- [ ] Vincular usuário pela tela do profissional e pela tela de usuários dá o mesmo resultado.
- [ ] Assinatura aparece só no PDF emitido.

**Status:** 🟨 v1 "Funcionários" existe — refazer com vínculo de usuário e flags
