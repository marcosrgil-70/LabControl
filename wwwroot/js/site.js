// LabControl v2 — scripts comuns a todas as telas (carregado pelo _Layout).

// Cadastro rápido "+" (T10-00, tarefa 2.4): <select lc-rapido="TABELA"> ganha um botão que abre o formulário do
// auxiliar (GET /Auxiliares/{tabela}/Incluir), grava por POST /Auxiliares/{tabela}/Rapido → { id, texto } e já deixa
// o item escolhido (também entra nos outros combos da mesma tabela na página). Erro → 422 { erro, campo } no campo.
(() => {
    const ajax = { 'X-Requested-With': 'XMLHttpRequest' };

    // dentro de outro modal (ex.: endereço dos contatos) o "+" abre por cima, dentro do mesmo .modal: assim o foco
    // preso pelo Bootstrap no modal de baixo não briga com o de cima, e o Esc fecha só o de cima
    const modalPara = select => {
        const dono = select.closest('.modal') ?? document.body;
        let el = dono.querySelector(':scope > .modal-rapido');
        if (!el) {
            el = document.createElement('div');
            el.className = 'modal fade modal-rapido' + (dono === document.body ? '' : ' modal-rapido-sobre');
            el.tabIndex = -1;
            el.innerHTML = '<div class="modal-dialog"></div>';
            dono.append(el);
            el.addEventListener('keydown', e => { if (e.key === 'Escape') e.stopPropagation(); });
            // o Bootstrap ignora hide() durante a animação de abertura; data-pronto = já pode fechar (usado nos testes)
            el.addEventListener('shown.bs.modal', () => el.dataset.pronto = '');
            el.addEventListener('hide.bs.modal', () => delete el.dataset.pronto);
            el.addEventListener('hidden.bs.modal', () => {
                // o Bootstrap tira o modal-open do body ao fechar este; o de baixo continua aberto
                if (dono !== document.body) document.body.classList.add('modal-open');
            });
        }
        return { el, modal: bootstrap.Modal.getOrCreateInstance(el, { backdrop: dono === document.body ? true : false }) };
    };

    const incluirOpcao = (select, id, texto) => {
        if ([...select.options].some(o => o.value === String(id))) return;
        const opcao = new Option(texto, id);
        const depois = [...select.options].find(o => o.value !== '' && o.text.localeCompare(texto, 'pt-BR') > 0);
        select.insertBefore(opcao, depois ?? null);
    };

    const mostrarErro = (form, erro, campo) => {
        form.querySelectorAll('.is-invalid').forEach(x => x.classList.remove('is-invalid'));
        form.querySelectorAll('[data-rapido-erro]').forEach(x => x.remove());
        const alvo = campo && form.querySelector(`[name="Valores[${campo}]"]:not([type=hidden])`);
        const msg = document.createElement('div');
        msg.dataset.rapidoErro = '';
        msg.textContent = erro;
        if (alvo) {
            alvo.classList.add('is-invalid');
            msg.className = 'invalid-feedback d-block';
            alvo.closest('.mb-3, .form-check, div')?.append(msg);
            alvo.focus();
        } else {
            msg.className = 'alert alert-danger py-2';
            form.querySelector('.modal-body')?.prepend(msg);
        }
    };

    const abrir = async select => {
        const tabela = select.dataset.rapido;
        const r = await fetch(`/Auxiliares/${tabela}/Incluir`, { headers: ajax });
        if (!r.ok) { alert(r.status === 403 ? 'Você não tem permissão para incluir.' : 'Não foi possível abrir o cadastro.'); return; }
        const { el, modal } = modalPara(select);
        el.querySelector('.modal-dialog').innerHTML = await r.text();
        const form = el.querySelector('form');
        form.querySelector('[name=Ativo]')?.closest('.form-check')?.remove(); // o cadastro rápido sempre inclui ativo
        form.addEventListener('submit', async e => {
            e.preventDefault();
            const botao = form.querySelector('[type=submit]');
            botao.disabled = true;
            try {
                const g = await fetch(`/Auxiliares/${tabela}/Rapido`, { method: 'POST', body: new FormData(form), headers: ajax });
                if (g.status === 422) { const j = await g.json(); mostrarErro(form, j.erro, j.campo); return; }
                if (!g.ok) { mostrarErro(form, g.status === 403 ? 'Você não tem permissão para incluir.' : 'Não foi possível gravar.'); return; }
                const { id, texto } = await g.json();
                document.querySelectorAll(`select[data-rapido="${tabela}"]`).forEach(s => incluirOpcao(s, id, texto));
                select.value = String(id);
                select.dispatchEvent(new Event('change', { bubbles: true }));
                if ('pronto' in el.dataset) modal.hide();
                else el.addEventListener('shown.bs.modal', () => modal.hide(), { once: true });
                select.focus();
            } finally {
                botao.disabled = false;
            }
        });
        el.addEventListener('shown.bs.modal', () => form.querySelector('input:not([type=hidden]),select')?.focus(), { once: true });
        modal.show();
    };

    document.querySelectorAll('select[data-rapido]').forEach(select => {
        const grupo = document.createElement('div');
        grupo.className = 'input-group';
        select.replaceWith(grupo);
        const botao = document.createElement('button');
        botao.type = 'button';
        botao.className = 'btn btn-outline-secondary';
        botao.title = botao.ariaLabel = 'Incluir ' + (select.dataset.rapidoNome ?? 'item');
        botao.dataset.rapidoBotao = select.dataset.rapido;
        botao.innerHTML = '<i class="bi bi-plus-lg"></i>';
        botao.addEventListener('click', () => abrir(select));
        grupo.append(select, botao);
    });
})();
