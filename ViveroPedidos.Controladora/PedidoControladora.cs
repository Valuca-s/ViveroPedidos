// ═══════════════════════════════════════════════════════════════════
//  USINGS: importan herramientas de otros lugares
// ═══════════════════════════════════════════════════════════════════

using Microsoft.EntityFrameworkCore.Update.Internal;            // ⚠️ BORRAR: es de Entity Framework y EF NO puede aparecer en la Controladora
using Microsoft.Win32;                                          // BORRAR: sirve para el registro de Windows, no se usa
using System;                                                   // SOBRA en .NET 8: ya viene incluido (DateTime, InvalidOperationException)
using System.Collections.Generic;                               // SOBRA en .NET 8: ya viene incluido (List<T>)
using System.ComponentModel.DataAnnotations;                    // BORRAR: anotaciones tipo [MaxLength], no se usan
using System.Linq;                                              // SOBRA en .NET 8: ya viene incluido (GroupBy, Any, Sum)
using System.Text;                                              // SOBRA: lo pone la plantilla de VS
using System.Threading.Tasks;                                   // SOBRA: lo pone la plantilla de VS
using ViveroPedidos.Modelo.Datos;                               // NECESARIO: para usar PedidoDatos y PlantaDatos
using ViveroPedidos.Modelo.Entidades;                           // NECESARIO: para usar Pedido, PedidoDetalle y Planta
using static System.Runtime.InteropServices.JavaScript.JSType;  // BORRAR: es para JavaScript, nada que ver

// Namespace: proyecto Controladora. Tiene que ser EXACTAMENTE este
// porque la Vista hace "using ViveroPedidos.Controladora;"
namespace ViveroPedidos.Controladora
{
    // Clase de LÓGICA DE NEGOCIO de pedidos.
    // Valida las reglas del enunciado y le pide al Modelo que grabe o busque.
    // NO usa Entity Framework: nunca escribe db, Include ni SaveChanges.
    // public = la Vista (otro proyecto) la puede usar.
    public class PedidoControladora
    {
        // ═══════════════════════════════════════════════════════════
        //  CAMPOS: las clases de datos que esta controladora usa
        // ═══════════════════════════════════════════════════════════

        // Objeto para grabar y consultar PEDIDOS.
        // private  = solo esta clase lo usa (la Vista no lo ve).
        // readonly = se crea una vez y no se puede reemplazar.
        // new()    = es lo mismo que "new PedidoDatos()".
        private readonly PedidoDatos _pedidoDatos = new();

        // Objeto para consultar PLANTAS: al registrar hay que verificar
        // que cada planta exista, esté disponible y copiar su nombre.
        private readonly PlantaDatos _plantaDatos = new();

        // ═══════════════════════════════════════════════════════════
        //  LISTAR: pedidos con filtros (FormConsultarPedidos)
        // ═══════════════════════════════════════════════════════════

        // Devuelve una lista de pedidos.
        // Los 3 parámetros tienen "= null" → son opcionales: la Vista puede no mandarlos.
        // DateTime? y string? → el "?" indica que pueden ser null.
        public List<Pedido> Listar(DateTime? desde = null, DateTime? hasta = null, string? solicitante = null)
        {
            // Valida que el rango de fechas tenga sentido. Se lee:
            //   desde.HasValue                    → ¿vino fecha "desde"?
            //   && hasta.HasValue                 → Y ¿vino fecha "hasta"?
            //   && desde.Value.Date > hasta.Value.Date → Y ¿desde es POSTERIOR a hasta?
            // Si las tres se cumplen, el rango está al revés → error.
            // (Se chequea HasValue primero porque .Value de una fecha null revienta.)
            if (desde.HasValue && hasta.HasValue && desde.Value.Date > hasta.Value.Date)
                throw new InvalidOperationException("La fecha 'desde' no puede ser posterior a la fecha 'hasta'.");   // corta y la Vista muestra el mensaje

            // Si el rango está bien, le pide al Modelo que haga la consulta con los filtros
            // y devuelve la lista que recibe directo a la Vista.
            return _pedidoDatos.Listar(desde, hasta, solicitante);
        }

        // ═══════════════════════════════════════════════════════════
        //  OBTENER CON DETALLE: un pedido con sus líneas
        // ═══════════════════════════════════════════════════════════

        // Devuelve UN pedido con sus líneas, o null si no existe (el "?" lo permite).
        // No hay reglas que validar: se delega directo al Modelo,
        // que ya devuelve null si el id no existe (como pide el enunciado).
        public Pedido? ObtenerConDetalle(int id)
        {
            return _pedidoDatos.ObtenerConDetalle(id);     // el Modelo hace el SELECT con Include
        }

        // ═══════════════════════════════════════════════════════════
        //  REGISTRAR: el caso principal (FormRegistrarPedido)
        // ═══════════════════════════════════════════════════════════

        // Devuelve el Pedido ya grabado (con su Numero asignado).
        // Recibe lo que manda la Vista al tocar "Grabar":
        public Pedido Registrar(
            DateTime fecha,                                 // fecha del pedido
            string solicitanteNombre,                       // nombre del vecino
            string solicitanteDocumento,                    // DNI del vecino
            DateTime fechaRetiro,                           // cuándo retira
            List<(int PlantaId, int Cantidad)> lineas)      // lista de pares (qué planta, cuántas)
        {
            // ─────────────────────────────────────────────
            //  1. VALIDAR LA CABECERA
            // ─────────────────────────────────────────────

            // Si el nombre es null, vacío o solo espacios → error
            if (string.IsNullOrWhiteSpace(solicitanteNombre))
                throw new InvalidOperationException("El nombre del solicitante es obligatorio.");

            // Lo mismo con el documento
            if (string.IsNullOrWhiteSpace(solicitanteDocumento))
                throw new InvalidOperationException("El documento del solicitante es obligatorio.");

            // Saca los espacios de los extremos (ya sabemos que no son null, así que Trim no revienta)
            var nombre = solicitanteNombre.Trim();
            var documento = solicitanteDocumento.Trim();

            // Si el nombre tiene más de 120 caracteres → error.
            // Evita que SQL rechace el INSERT con un error técnico (la columna es nvarchar(120)).
            if (nombre.Length > 120)
                throw new InvalidOperationException("El nombre del solicitante no puede superar los 120 caracteres.");

            // Lo mismo con el documento (columna nvarchar(30))
            if (documento.Length > 30)
                throw new InvalidOperationException("El documento no puede superar los 30 caracteres.");

            // Les saca la hora a las dos fechas (.Date → queda a las 00:00)
            var fechaPedido = fecha.Date;
            var fechaRet = fechaRetiro.Date;

            // Si la fecha de retiro es ANTERIOR a la del pedido → error (lo pide el enunciado)
            if (fechaRet < fechaPedido)
                throw new InvalidOperationException("La fecha de retiro no puede ser anterior a la fecha del pedido.");

            // ─────────────────────────────────────────────
            //  2. VALIDAR LAS LÍNEAS EN CONJUNTO
            // ─────────────────────────────────────────────

            // Si la lista no llegó (is null) O llegó vacía (Count == 0) → error.
            // Con "||", si es null ya entra al if y NO evalúa lineas.Count (que reventaría).
            if (lineas is null || lineas.Count == 0)
                throw new InvalidOperationException("El pedido debe tener al menos una línea.");

            // Detecta plantas repetidas:
            //   GroupBy(l => l.PlantaId)    → junta las líneas en grupos por planta
            //   Any(g => g.Count() > 1)     → ¿algún grupo tiene más de una línea?
            // Ejemplo: (1,3), (2,2), (1,5) → la planta 1 tiene 2 líneas → error.
            if (lineas.GroupBy(l => l.PlantaId).Any(g => g.Count() > 1))
                throw new InvalidOperationException("No se puede incluir la misma planta en dos líneas del pedido.");

            // ─────────────────────────────────────────────
            //  3. VALIDAR CADA LÍNEA Y ARMAR EL DETALLE
            // ─────────────────────────────────────────────

            // Lista vacía donde se van guardando las líneas ya validadas,
            // convertidas en objetos PedidoDetalle (lo que se va a grabar)
            var detalles = new List<PedidoDetalle>();

            // Recorre cada línea. En cada vuelta, "l" es un par (PlantaId, Cantidad)
            foreach (var l in lineas)
            {
                // Si la cantidad es 0 o negativa → error
                if (l.Cantidad <= 0)
                    throw new InvalidOperationException("Cada línea debe tener una cantidad mayor que cero.");

                // Le pide al Modelo la planta con ese Id.
                // ?? throw → si volvió null (no existe), corta con este mensaje.
                // $"...{l.PlantaId}..." mete el número de Id dentro del texto.
                var planta = _plantaDatos.ObtenerPorId(l.PlantaId)
                    ?? throw new InvalidOperationException($"La planta con Id {l.PlantaId} no existe.");

                // Si la planta existe pero NO está disponible (el "!" invierte) → error
                if (!planta.Disponible)
                    throw new InvalidOperationException($"La planta '{planta.NombreComun}' no está disponible para pedidos.");

                // La línea pasó todas las validaciones: se crea el PedidoDetalle y se agrega a la lista.
                // new PedidoDetalle { ... } = crea el objeto y le carga las propiedades en un solo paso.
                detalles.Add(new PedidoDetalle
                {
                    PlantaId = planta.Id,                       // qué planta es (FK a Plantas)
                    NombreHistorico = planta.NombreComun,       // CLAVE: COPIA el nombre de HOY. Si mañana renombran la planta,
                                                                //        este pedido sigue mostrando este nombre
                    Cantidad = l.Cantidad                       // cuántas pidió
                    // PedidoId NO se pone: EF lo completa solo al grabar,
                    // porque esta línea va a viajar dentro de pedido.Detalles
                });
            }   // fin del foreach: vuelve arriba con la siguiente línea, hasta recorrerlas todas

            // ─────────────────────────────────────────────
            //  4. ARMAR LA CABECERA
            // ─────────────────────────────────────────────

            // Crea el objeto Pedido y le carga todos los datos
            var pedido = new Pedido
            {
                // Numero NO se pone acá: lo calcula PedidoDatos DENTRO de la transacción (último + 1)
                Fecha = fechaPedido,                            // fecha sin hora
                SolicitanteNombre = nombre,                     // nombre ya recortado
                SolicitanteDocumento = documento,               // documento ya recortado
                FechaRetiro = fechaRet,                         // fecha de retiro sin hora
                CantidadItems = lineas.Sum(l => l.Cantidad),    // suma de todas las cantidades. Ej: 3 + 2 = 5
                Detalles = detalles                             // le "cuelga" la lista de líneas armada en el paso 3
            };

            // ─────────────────────────────────────────────
            //  5. DELEGAR LA GRABACIÓN AL MODELO
            // ─────────────────────────────────────────────

            // Le pasa el pedido completo a PedidoDatos.Registrar, que:
            //   abre transacción → calcula Numero → INSERT cabecera + líneas → Commit.
            // Devuelve el pedido con Id y Numero asignados, y este método se lo devuelve a la Vista,
            // que muestra "Pedido registrado. Número: X".
            return _pedidoDatos.Registrar(pedido);
        }
    }
}