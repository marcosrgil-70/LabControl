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
- [x] Profissional com "Assina boletim" exige registro.
- [x] Vincular usuário pela tela do profissional e pela tela de usuários dá o mesmo resultado (mesmo `VinculoUsuarioProfissionalService`).
- [ ] Assinatura aparece só no PDF emitido — **na 5.3/5.4** (a imagem PNG ≤ 300 KB e o SHA-256 já são gravados e exibidos na tela).
- [x] Vários endereços, telefones e e-mails tipados (mesmo componente da T12/T03).

## Implementação (antecipada em 20/10/2026 — pedido do usuário)
- `Controllers/ProfissionaisController.cs` (`/Profissionais`, `Criar`, `Editar/{id}`, `Excluir`, `Assinatura/{id}`; contatos em `/Profissionais/{idEntidade}/…`) · `Services/Entidades/ProfissionalService.cs` · `Views/Profissionais/{Index,Form}.cshtml`.
- Inclusão cria `ENTIDADES` (pessoa física, `TIPO_FUNCIONARIO`) + `ENTIDADES_FUNCIONARIOS` na mesma transação; cargo e tipo de registro pelos auxiliares T10-05/T10-06; UF do registro validada; CPF com dígito verificador.
- **Usuário do sistema**: combo com os usuários da empresa sem profissional (+ o já vinculado); trocar o usuário desfaz o vínculo anterior; **profissional inativo fica sem usuário**. Excluir: só sem movimento e sem usuário vinculado; se também for cliente/fornecedor, só inativar.
- Testes: `CadastrosEntidadesTests.Profissional_registro_assinatura_e_vinculo_com_usuario`, `CadastrosTelasTests`. Processo: [P25](../processos/P25-cadastro-entidades-contatos.md).

**Status:** ✅ concluída em 20/10/2026 (antecipada da tarefa 3.2) — falta a assinatura no PDF do boletim (5.3/5.4).
