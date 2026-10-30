using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace ViveroPedidos.Modelo.Entidades
{
    public class Planta
    {
        // Clave primaria. EF detecta que es PK porque se llama "Id"
        // y la hace IDENTITY (SQL le asigna el número solo: 1, 2, 3...)
        public int Id { get; set; }

        // Código interno de inventario (único, máx. 20 caracteres; eso se configura en el DbContext)
        // "= string.Empty" hace que arranque como texto vacío en vez de null
        public string Codigo { get; set; } = string.Empty;

        // Nombre común, por ejemplo "Rosa" (máx. 120)
        public string NombreComun { get; set; } = string.Empty;

        // Nombre científico, por ejemplo "Rosa gallica" (máx. 120)
        public string Especie { get; set; } = string.Empty;

        // true = se puede pedir; false = no aparece para pedidos nuevos.
        // Arranca en true porque una planta nueva siempre está disponible.
        public bool Disponible { get; set; } = true;

        // Propiedad de NAVEGACIÓN: no es una columna.
        // Representa "todas las líneas de pedido donde aparece esta planta".
        // Es el lado "muchos" de la relación Planta (1) → PedidoDetalle (N).
        // La Vista la oculta en la grilla (dgv.Columns["Detalles"].Visible = false).
        public List<PedidoDetalle> Detalles { get; set; } = new();
    }
}
