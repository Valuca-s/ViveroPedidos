using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using ViveroPedidos.Modelo.Entidades;

namespace ViveroPedidos.Modelo.Entidades
{
    public class PedidoDetalle
    {
        public int Id { get; set; }
        public int PedidoId { get; set; }
        public Pedido pedido { get; set; } = new Pedido();
        public int PlantaId { get; set; }
        public Planta planta { get; set; } = new Planta();
        public string NombreHistorico { get; set; } = string.Empty;
        public int Cantidad { get; set; }

    }
}
/*
    // PK identity
    public int Id { get; set; }

    // ---- Relación con el PEDIDO ----
    // FK: guarda el Id del pedido al que pertenece esta línea (es una columna real)
    public int PedidoId { get; set; }

    // Navegación al pedido "padre" (no es columna; EF lo completa cuando hace falta).
    // "= null!" le dice al compilador: "arranca vacío, pero EF lo va a llenar".
    public Pedido Pedido { get; set; } = null!;

    // ---- Relación con la PLANTA ----
    // FK: guarda el Id de la planta pedida
    public int PlantaId { get; set; }

    // Navegación a la planta
    public Planta Planta { get; set; } = null!;

    // ---- Datos propios de la línea ----
    // Copia del NombreComun de la planta EN EL MOMENTO de pedir.
    // Si mañana renombran la planta, esta línea conserva el nombre viejo.
    public string NombreHistorico { get; set; } = string.Empty;   // máx. 120

    // Cuántas unidades de esa planta (tiene que ser > 0)
    public int Cantidad { get; set; }
*/
/*
Id(int, identity, PK)
PedidoId(FK → Pedidos, borrado en cascada)
PlantaId(FK → Plantas; no borrar la planta si tiene detalles: borrado restringido)
NombreHistorico (texto, longitud 120) — copia de NombreComun al pedir
Cantidad (int, > 0)
*/