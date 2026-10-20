// LabControl v2 — componente de contatos (endereços, telefones, e-mails) das telas Empresa (T03), Clientes (T12)
// e Profissionais (T13). Protocolo (docs/processos/P25-cadastro-entidades-contatos.md):
//   GET  {url}/Contatos                          → HTML das abas (_ContatosAbas)
//   POST {url}/ContatoEndereco|ContatoFone|ContatoEmail → { ok, id } ou 422 { erro, campo }
//   POST {url}/ContatoExcluir (tipo, id)         → { ok } ou 422 { erro }
//   GET  /Geografia/Cidades?idEstado=  ·  GET /Geografia/Cep/{cep}
(() => {
    const config = document.getElementById('contatos');
    if (!config) return;
    const url = config.dataset.url;
    const token = () => config.querySelector('input[name="__RequestVerificationToken"]').value;
    const ajax = { 'X-Requested-With': 'XMLHttpRequest' };
    const abas = ['aba-enderecos', 'aba-telefones', 'aba-emails'];
    const modais = { endereco: 'modal-endereco', fone: 'modal-fone', email: 'modal-email' };
    const titulos = { endereco: 'endereço', fone: 'telefone', email: 'e-mail' };

    // ---- abas: recarrega o miolo dos três painéis e as contagens dos botões das abas
    async function recarregar() {
        const r = await fetch(url + '/Contatos', { headers: ajax });
        if (!r.ok) return;
        const doc = new DOMParser().parseFromString(await r.text(), 'text/html');
        for (const id of abas) document.getElementById(id).innerHTML = doc.getElementById(id).innerHTML;
        contagens(doc);
    }
    function contagens(doc = document) {
        for (const nome of ['enderecos', 'telefones', 'emails']) {
            const n = doc.getElementById('contagem-' + nome)?.textContent ?? '';
            document.querySelectorAll(`[data-contagem="${nome}"]`).forEach(b => b.textContent = n);
            if (doc !== document) { const alvo = document.getElementById('contagem-' + nome); if (alvo) alvo.textContent = n; }
        }
    }
    contagens();

    // ---- cidades da UF
    async function carregarCidades(form, idEstado, selecionada) {
        const cidade = form.querySelector('[data-cidade]');
        cidade.innerHTML = '<option value="">' + (idEstado ? '—' : '— selecione a UF —') + '</option>';
        if (!idEstado) return;
        const r = await fetch('/Geografia/Cidades?idEstado=' + encodeURIComponent(idEstado), { headers: ajax });
        if (!r.ok) return;
        for (const c of await r.json()) cidade.add(new Option(c.nome, c.id));
        if (selecionada) cidade.value = String(selecionada);
    }

    // ---- abrir modal (novo ou alteração)
    function abrir(tipo, registro) {
        const modalEl = document.getElementById(modais[tipo]);
        const form = modalEl.querySelector('form');
        form.reset();
        form.querySelector('[data-erro]').hidden = true;
        form.querySelectorAll('.is-invalid').forEach(e => e.classList.remove('is-invalid'));
        const cepMsg = form.querySelector('[data-cep-msg]'); if (cepMsg) cepMsg.textContent = '';
        form.querySelector('[name="Id"]').value = registro?.Id ?? 0;
        form.querySelector('[data-titulo]').textContent = (registro ? 'Alterar ' : 'Incluir ') + titulos[tipo];
        if (registro) {
            for (const [chave, valor] of Object.entries(registro)) {
                const campo = form.querySelector(`[name="${chave}"]`);
                if (!campo || chave === 'IdCidades') continue;
                if (campo.type === 'checkbox') campo.checked = valor === true;
                else campo.value = valor ?? '';
            }
        }
        if (tipo === 'endereco') carregarCidades(form, registro?.IdEstado, registro?.IdCidades);
        bootstrap.Modal.getOrCreateInstance(modalEl).show();
    }

    document.addEventListener('click', e => {
        const novo = e.target.closest('[data-contato-novo]');
        if (novo) return abrir(novo.dataset.contatoNovo, null);
        const editar = e.target.closest('[data-contato-editar]');
        if (editar) return abrir(editar.dataset.contatoEditar, JSON.parse(editar.dataset.registro));
        const excluir = e.target.closest('[data-contato-excluir]');
        if (excluir) return excluirContato(excluir);
    });

    // ---- gravar
    document.querySelectorAll('form[data-contato-form]').forEach(form => {
        form.addEventListener('submit', async e => {
            e.preventDefault();
            const dados = new FormData(form);
            dados.append('__RequestVerificationToken', token());
            const botao = form.querySelector('[type=submit]');
            botao.disabled = true;
            try {
                const r = await fetch(url + '/' + form.dataset.acao, { method: 'POST', body: dados, headers: ajax });
                if (r.ok) {
                    bootstrap.Modal.getInstance(form.closest('.modal')).hide();
                    await recarregar();
                    return;
                }
                const j = r.status === 422 ? await r.json() : { erro: r.status === 403 ? 'Você não tem permissão para esta ação.' : 'Não foi possível gravar. Atualize a tela e tente de novo.' };
                mostrarErro(form, j.erro, j.campo);
            } finally {
                botao.disabled = false;
            }
        });
        form.querySelector('[data-uf]')?.addEventListener('change', ev => carregarCidades(form, ev.target.value));
        form.querySelector('[data-buscar-cep]')?.addEventListener('click', () => buscarCep(form));
        form.querySelector('[name="Cep"]')?.addEventListener('blur', ev => {
            if (ev.target.value.replace(/\D/g, '').length === 8 && !form.querySelector('[name="Logradouro"]').value) buscarCep(form);
        });
    });

    function mostrarErro(form, mensagem, campo) {
        const caixa = form.querySelector('[data-erro]');
        caixa.textContent = mensagem;
        caixa.hidden = false;
        if (campo) form.querySelector(`[name="${campo}"]`)?.classList.add('is-invalid');
    }

    // ---- CEP (ViaCEP pelo servidor): preenche e deixa o usuário conferir
    async function buscarCep(form) {
        const cep = form.querySelector('[name="Cep"]').value.replace(/\D/g, '');
        const msg = form.querySelector('[data-cep-msg]');
        if (cep.length !== 8) { msg.textContent = 'Informe os 8 dígitos do CEP.'; return; }
        msg.textContent = 'Buscando…';
        const r = await fetch('/Geografia/Cep/' + cep, { headers: ajax });
        const j = await r.json().catch(() => ({ erro: 'Serviço de CEP indisponível.' }));
        if (!r.ok) { msg.textContent = j.erro; return; }
        form.querySelector('[name="Cep"]').value = j.cep;
        if (j.logradouro) form.querySelector('[name="Logradouro"]').value = j.logradouro;
        if (j.idTiposLogradouro) form.querySelector('[name="IdTiposLogradouro"]').value = String(j.idTiposLogradouro);
        if (j.bairro) form.querySelector('[name="Bairro"]').value = j.bairro;
        if (j.complemento && !form.querySelector('[name="Complemento"]').value) form.querySelector('[name="Complemento"]').value = j.complemento;
        form.querySelector('[name="IdEstado"]').value = String(j.idEstado);
        await carregarCidades(form, j.idEstado, j.idCidade);
        msg.textContent = `${j.cidade}/${j.uf} — confira e informe o número.`;
        form.querySelector('[name="Numero"]').focus();
    }

    // ---- excluir (em uso → mensagem do banco sugerindo inativar)
    async function excluirContato(botao) {
        if (!confirm(`Deseja realmente excluir "${botao.dataset.descricao}"?`)) return;
        const dados = new FormData();
        dados.append('tipo', botao.dataset.contatoExcluir);
        dados.append('idContato', botao.dataset.id);
        dados.append('__RequestVerificationToken', token());
        const r = await fetch(url + '/ContatoExcluir', { method: 'POST', body: dados, headers: ajax });
        if (r.ok) return recarregar();
        const j = r.status === 422 ? await r.json() : { erro: 'Não foi possível excluir.' };
        alert(j.erro);
    }
})();
