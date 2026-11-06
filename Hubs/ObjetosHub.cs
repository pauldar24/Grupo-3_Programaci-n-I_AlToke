using Microsoft.AspNetCore.SignalR;

namespace GRUPAL.Hubs;

// El hub solo publica avisos visibles para todos; los clientes no pueden invocar acciones.
public sealed class ObjetosHub : Hub
{
}
