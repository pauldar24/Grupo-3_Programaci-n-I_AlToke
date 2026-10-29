(() => {
  const badge = document.getElementById('contadorRecuperaciones');
  if (!badge) return;
  async function verificar() {
    try {
      const r = await fetch('/Recuperaciones/Pendientes', { credentials: 'same-origin', cache: 'no-store' });
      if (!r.ok) return;
      const { cantidad } = await r.json();
      badge.hidden = cantidad <= 0;
      badge.textContent = String(cantidad);
    } catch (_) { badge.hidden = true; }
  }
  verificar();
  setInterval(verificar, 60000);
})();
