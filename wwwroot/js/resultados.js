// T26 — Resultados (tarefa 5.2). Um cartão por teste da amostra:
// - "Lançar" (ou "Lançar todos") abre o cartão pendente com início = entrega, resultado = hoje e o analista do usuário;
// - numérico × descritivo habilita um grupo e esconde o outro (cds_ResultadosTIPO_RESULTADOChange do legado); trocar o
//   tipo do resultado troca o da especificação junto (RN-07.05), que pode ser mudado depois;
// - prévia ao digitar, no formato do boletim (o texto definitivo é o que o banco monta ao gravar).
(() => {
    const form = document.getElementById('formResultado');
    if (!form) return;
    const { entrega, hoje, analista } = form.dataset;

    const lancar = card => {
        const marca = card.querySelector('[data-lancar]');
        if (marca.value === 'true') return;
        marca.value = 'true';
        card.querySelector('[data-corpo]').classList.remove('d-none');
        card.querySelector('[data-botao-lancar]')?.remove();
        card.querySelector('[data-previa-banco]').replaceChildren();
        const inicio = card.querySelector('[data-inicio]'), fim = card.querySelector('[data-fim]'), quem = card.querySelector('[data-analista]');
        if (!inicio.value) inicio.value = entrega;
        if (!fim.value) fim.value = hoje;
        if (!quem.value && analista) quem.value = analista;
        card.querySelector('[data-grupo="resultado"] [data-valor]')?.focus();
    };

    const sobrescrito = n => [...String(n)].map(c => c === '-' ? '⁻' : '⁰¹²³⁴⁵⁶⁷⁸⁹'[Number(c)] ?? c).join('');

    // mesma regra da FN_FORMATA_RESULTADO, já com "× 10ⁿ" como no boletim
    const previa = grupo => {
        const alvo = grupo.querySelector('[data-previa]');
        if (grupo.querySelector('[data-numerico]').checked) {
            const valor = grupo.querySelector('[data-valor]').value.trim();
            if (!valor) return alvo.textContent = '';
            const exp = Number(grupo.querySelector('[data-expoente]').value || 0);
            const simb = grupo.querySelector('[data-simbolo]').value;
            const un = grupo.querySelector('[data-unidade]');
            const sigla = un.value ? un.selectedOptions[0].text : '';
            alvo.textContent = [simb, valor + (exp ? ` × 10${sobrescrito(exp)}` : ''), sigla].filter(Boolean).join(' ');
        } else {
            const tipo = grupo.querySelector('[data-tipo]');
            alvo.textContent = tipo.value ? tipo.selectedOptions[0].text : '';
        }
    };

    const aplicarTipo = grupo => {
        const numerico = grupo.querySelector('[data-numerico]').checked;
        grupo.querySelector('[data-campos-numerico]').classList.toggle('d-none', !numerico);
        grupo.querySelector('[data-campos-descritivo]').classList.toggle('d-none', numerico);
        previa(grupo);
    };

    form.addEventListener('click', e => {
        const botao = e.target.closest('[data-botao-lancar]');
        if (botao) lancar(botao.closest('[data-linha]'));
    });
    document.getElementById('botaoLancarTodos')?.addEventListener('click', () => {
        form.querySelectorAll('[data-linha]').forEach(card => { if (card.querySelector('[data-botao-lancar]')) lancar(card); });
        form.querySelector('[data-lancar][value="true"]')?.closest('[data-linha]')?.querySelector('[data-grupo="resultado"] [data-valor]')?.focus();
    });

    form.addEventListener('change', e => {
        const grupo = e.target.closest('[data-grupo]');
        if (!grupo) return;
        if (e.target.matches('[data-numerico]')) {
            aplicarTipo(grupo);
            if (grupo.dataset.grupo === 'resultado') {
                const espec = grupo.closest('[data-linha]').querySelector('[data-grupo="espec"]');
                espec.querySelector('[data-numerico]').checked = e.target.checked;
                aplicarTipo(espec);
            }
        } else previa(grupo);
    });
    form.addEventListener('input', e => { const grupo = e.target.closest('[data-grupo]'); if (grupo) previa(grupo); });
    form.querySelectorAll('[data-grupo]').forEach(previa);

    // "Excluir" de cada parâmetro usa um formulário só: a pergunta leva o nome do parâmetro
    document.addEventListener('click', e => {
        const botao = e.target.closest('[data-confirmar-texto]');
        if (botao) document.getElementById(botao.getAttribute('form')).dataset.confirmar = botao.dataset.confirmarTexto;
    });
})();
