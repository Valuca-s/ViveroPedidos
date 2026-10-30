using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using ViveroPedidos.Modelo.Entidades;

namespace ViveroPedidos.Modelo.Entidades
{
    public class Pedido
    {
        public int Id { get; set; }
        public int Numero { get; set; }
        public DateTime Fecha { get; set; }
        public string SolicitanteNombre { get; set; } = string.Empty;
        public string SolicitanteDocumento { get; set; }
        public DateTime FechaRetiro { get; set; }
        public int CantidadItems { get; set; }

        public List<PedidoDetalle> Detalles { get; set; } = new();
    }
}
/*
    // PK identity (la pone SQL)
    public int Id { get; set; }

    // Número de pedido visible para el usuario (1, 2, 3...).
    // NO es identity: lo calcula la aplicación al grabar (último + 1).
    public int Numero { get; set; }

    // Fecha del pedido. Se guarda SIN hora (columna "date", se configura en el DbContext)
    public DateTime Fecha { get; set; }

    // Datos del vecino que pide. No hay tabla Solicitantes:
    // se copian directamente en la cabecera.
    public string SolicitanteNombre { get; set; } = string.Empty;     // máx. 120
    public string SolicitanteDocumento { get; set; } = string.Empty;  // máx. 30 (DNI)

    // Cuándo pasa a retirar. No puede ser anterior a Fecha (lo valida la Controladora)
    public DateTime FechaRetiro { get; set; }

    // Suma de las cantidades de todas las líneas.
    // Ejemplo: 3 rosas + 2 jazmines → CantidadItems = 5
    public int CantidadItems { get; set; }

    // Navegación: las líneas de ESTE pedido.
    // Relación Pedido (1) → PedidoDetalle (N).
    // Al grabar, EF guarda la cabecera Y todas estas líneas juntas.
    public List<PedidoDetalle> Detalles { get; set; } = new();
*/


/*
Id(int, identity, PK)
Numero(único; correlativo controlado por la aplicación, no es identity)
Fecha (sin hora)
SolicitanteNombre (longitud 120)
SolicitanteDocumento (texto, longitud 30; DNI o equivalente)
FechaRetiro (sin hora)
CantidadItems (int) — debe coincidir con la suma de cantidades del detalle
*/