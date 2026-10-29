(() => {
  const input = document.getElementById('buscarObjetos');
  const suggestions = document.getElementById('sugerenciasObjetos');
  if (!input || !suggestions) return;
  let request;
  let sequence = 0;
  const clear = () => suggestions.replaceChildren();
  input.addEventListener('input', async () => {
    const term = input.value.trim();
    if (request) request.abort();
    clear();
    if (term.length < 2) return;
    request = new AbortController();
    const current = ++sequence;
    try {
      const res = await fetch(`/Explorar/Sugerencias?termino=${encodeURIComponent(term)}`, { signal: request.signal });
      if (!res.ok) return;
      const data = await res.json();
      if (current !== sequence || input.value.trim() !== term) return;
      for (const item of data) {
        const link = document.createElement('a');
        link.href = `/Explorar/Detalle/${item.id}`;
        link.className = 'altoke-suggestion';
        const title = document.createElement('strong'); title.textContent = item.titulo;
        const extra = document.createElement('small'); extra.textContent = `${item.categoria} · ${item.ubicacion}`;
        link.append(title, extra);
        suggestions.appendChild(link);
      }
    } catch (e) { if (e.name !== 'AbortError') clear(); }
  });
  document.addEventListener('click', e => { if (e.target !== input && !suggestions.contains(e.target)) clear(); });
})();
