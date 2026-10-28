// T21 — modal "Gerar pedido" (aberto pela proposta): busca o formulário no servidor e abre o modal;
// recusa (proposta antiga, já com pedido…) vem como 422 com a mensagem.
(function () {
    'use strict';
    const botao = document.getElementById('botaoGerarPedido');
    const modal = document.getElementById('modalGerarPedido');
    if (!botao || !modal) return;

    botao.addEventListener('click', async () => {
        botao.disabled = true;
        try {
            const resp = await fetch(botao.dataset.url, { headers: { 'X-Requested-With': 'fetch' } });
            const texto = await resp.text();
            if (!resp.ok) { alert(texto || 'Não foi possível abrir a geração do pedido.'); return; }
            modal.querySelector('.modal-dialog').innerHTML = texto;
            bootstrap.Modal.getOrCreateInstance(modal).show();
        } catch {
            alert('Não foi possível abrir a geração do pedido. Verifique a conexão e tente de novo.');
        } finally {
            botao.disabled = false;
        }
    });
})();
