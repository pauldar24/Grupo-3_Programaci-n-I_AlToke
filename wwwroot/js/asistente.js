(() => {
  const root = document.getElementById('chatAlToke');
  if (!root) return;
  const open = document.getElementById('abrirChatAlToke');
  const close = document.getElementById('cerrarChatAlToke');
  const panel = document.getElementById('panelChatAlToke');
  const log = document.getElementById('mensajesChatAlToke');
  const form = document.getElementById('formChatAlToke');
  const field = document.getElementById('preguntaChatAlToke');
  const token = form.querySelector('[name="__RequestVerificationToken"]');
  const toggle = (active) => {
    panel.hidden = !active;
    open.setAttribute('aria-expanded', String(active));
    if (active) field.focus();
  };
  open.addEventListener('click', () => toggle(panel.hidden));
  close.addEventListener('click', () => toggle(false));
  const bubble = (text, own = false) => {
    const block = document.createElement('p');
    block.className = `altoke-chat-bubble${own ? ' altoke-chat-user' : ''}`;
    block.textContent = text;
    log.appendChild(block);
    log.scrollTop = log.scrollHeight;
    return block;
  };
  form.addEventListener('submit', async (event) => {
    event.preventDefault();
    const message = field.value.trim();
    if (!message) return;
    bubble(message, true);
    field.value = '';
    const pending = bubble('Buscando en AlToke...');
    const button = form.querySelector('button[type="submit"]');
    button.disabled = true;
    try {
      const response = await fetch(root.dataset.url, {
        method: 'POST', credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token.value },
        body: JSON.stringify({ mensaje: message })
      });
      if (!response.ok) throw new Error('No se pudo obtener una respuesta.');
      const data = await response.json();
      pending.textContent = data.respuesta;
      for (const objeto of (data.objetos || [])) {
        const link = document.createElement('a');
        link.className = 'altoke-chat-link';
        link.href = objeto.url;
        link.textContent = objeto.titulo;
        log.appendChild(link);
      }
    } catch (_) { pending.textContent = 'No pude responder en este momento. Prueba desde la sección Explorar.'; }
    finally { button.disabled = false; log.scrollTop = log.scrollHeight; field.focus(); }
  });
})();
