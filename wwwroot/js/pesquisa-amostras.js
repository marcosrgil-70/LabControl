// T29 — Pesquisa de amostras (componente, tarefa 4.4). Qualquer botão
//   <button type="button" data-pesquisa-amostras
//           data-filtros="ApenasComResultado=true"          (opcional: filtros fixos da tela que abre)
//           data-alvo-id="#IdAmostra" data-alvo-nr="#NrAmostra"   (opcional: campos a preencher)
//           data-abrir="/Amostras/Editar/{id}"                 (opcional: navegar depois de escolher)
//           data-url="/Resultados/Pesquisa">                  (opcional: action da tela que abre, com a permissão dela)
// abre o modal, pesquisa no servidor (/Amostras/Pesquisa — sempre na empresa ativa) e, ao escolher (duplo clique ou
// "Selecionar"), preenche os campos, dispara o evento 'amostra-selecionada' { id, nrAmostra } no botão e/ou navega.
(() => {
    const ajax = { headers: { 'X-Requested-With': 'XMLHttpRequest' } };
    let modal, el, corpo, origem;

    const criar = () => {
        if (el) return;
        el = document.createElement('div');
        el.className = 'modal fade';
        el.id = 'modalPesquisaAmostras';
        el.tabIndex = -1;
        el.setAttribute('aria-labelledby', 'tituloPesquisaAmostras');
        el.innerHTML = `<div class="modal-dialog modal-xl modal-dialog-scrollable"><div class="modal-content">
            <div class="modal-header py-2"><h6 class="modal-title" id="tituloPesquisaAmostras"><i class="bi bi-search me-1"></i>Pesquisa de amostras</h6>
            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Fechar"></button></div>
            <div class="modal-body" data-pesquisa-corpo></div></div></div>`;
        document.body.append(el);
        corpo = el.querySelector('[data-pesquisa-corpo]');
        modal = bootstrap.Modal.getOrCreateInstance(el);

        corpo.addEventListener('submit', e => {
            if (!e.target.matches('[data-pesquisa-filtros]')) return;
            e.preventDefault();
            carregar(parametros());
        });
        corpo.addEventListener('click', e => {
            const pagina = e.target.closest('[data-pagina]');
            if (pagina) return carregar(parametros(pagina.dataset.pagina));
            const botao = e.target.closest('[data-selecionar]');
            if (botao) escolher(botao.closest('tr'));
        });
        corpo.addEventListener('dblclick', e => {
            const linha = e.target.closest('tr[data-amostra-id]');
            if (linha) escolher(linha);
        });
    };

    const parametros = pagina => {
        const p = new URLSearchParams(new FormData(corpo.querySelector('[data-pesquisa-filtros]')));
        if (pagina) p.set('Pagina', pagina);
        return p;
    };

    const carregar = async p => {
        const r = await fetch(`${origem.dataset.url ?? '/Amostras/Pesquisa'}?${p}`, ajax);
        corpo.innerHTML = r.ok ? await r.text()
            : `<div class="alert alert-danger py-2">${r.status === 403 ? 'Você não tem permissão para pesquisar amostras.' : 'Não foi possível pesquisar.'}</div>`;
        corpo.querySelector('[name=Numero]')?.focus();
    };

    const escolher = linha => {
        const amostra = { id: Number(linha.dataset.amostraId), nrAmostra: linha.dataset.amostraNr };
        if (origem.dataset.alvoId) document.querySelector(origem.dataset.alvoId).value = amostra.id;
        if (origem.dataset.alvoNr) document.querySelector(origem.dataset.alvoNr).value = amostra.nrAmostra;
        origem.dispatchEvent(new CustomEvent('amostra-selecionada', { bubbles: true, detail: amostra }));
        modal.hide();
        if (origem.dataset.abrir) location.href = origem.dataset.abrir.replace('{id}', amostra.id);
    };

    document.addEventListener('click', e => {
        const botao = e.target.closest('[data-pesquisa-amostras]');
        if (!botao) return;
        origem = botao;
        criar();
        corpo.innerHTML = '<div class="text-center text-body-secondary py-4"><span class="spinner-border spinner-border-sm me-2"></span>Carregando…</div>';
        modal.show();
        const p = new URLSearchParams(botao.dataset.filtros ?? '');
        p.set('inicial', 'true');
        carregar(p);
    });
})();
