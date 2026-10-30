using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;
using ViveroPedidos.Modelo.Entidades;

namespace ViveroPedidos.Modelo.Datos
{
    // Clase de ACCESO A DATOS de pedidos. Es la única que toca las tablas Pedidos y PedidoDetalles.
    // public = la Controladora (otro proyecto) la puede usar.
    public class PedidoDatos
    {
        // ═══════════════════════════════════════════════════════════
        //  LISTAR: pedidos con filtros opcionales (FormConsultarPedidos)
        // ═══════════════════════════════════════════════════════════

        // Devuelve una LISTA de pedidos.
        // Los 3 parámetros llevan "?" → pueden venir null → son filtros opcionales.
        public List<Pedido> Listar(DateTime? desde, DateTime? hasta, string? solicitante)
        {
            // Abre la conexión con la base. "using" = se cierra sola al terminar el método.
            using var db = new ViveroDbContext();

            // Arma la consulta base: "todos los pedidos".
            // AsNoTracking = solo lectura (no vigilar). AsQueryable = dejarla abierta para sumar filtros.
            // TODAVÍA NO va a la base.
            var consulta = db.Pedidos.AsNoTracking().AsQueryable();

            // ---- Filtro "desde" ----
            // HasValue = "¿desde trae una fecha, o es null?". Si es null, no filtra.
            if (desde.HasValue)
            {
                // .Value = saca la fecha de adentro del DateTime?
                // .Date  = le quita la hora (queda 00:00) para comparar solo el día.
                var d = desde.Value.Date;

                // Agrega a la consulta: "fecha MAYOR O IGUAL a d". El "=" incluye el día de inicio.
                consulta = consulta.Where(p => p.Fecha >= d);
            }

            // ---- Filtro "hasta" ----
            if (hasta.HasValue)
            {
                var h = hasta.Value.Date;                       // fecha final, sin hora

                // Agrega: "fecha MENOR O IGUAL a h". El "=" incluye el día final (rango inclusivo).
                consulta = consulta.Where(p => p.Fecha <= h);
            }

            // ---- Filtro por nombre del solicitante ----
            // ⚠️ CORREGIDO: faltaba el "!". Se lee: "si el solicitante NO está vacío, filtrá".
            //    Sin el "!" hacía lo contrario, y con null reventaba en el Trim().
            if (!string.IsNullOrWhiteSpace(solicitante))
            {
                var texto = solicitante.Trim();                 // saca espacios de los extremos: " juan " → "juan"

                // Agrega: "nombre que CONTENGA el texto" → en SQL: LIKE '%juan%'
                consulta = consulta.Where(p => p.SolicitanteNombre.Contains(texto));
            }

            // Ordena por número de pedido y RECIÉN ACÁ ejecuta el SELECT en SQL.
            // Devuelve la lista a la Controladora, que se la pasa a la Vista.
            return consulta.OrderBy(p => p.Numero).ToList();
        }

        // ═══════════════════════════════════════════════════════════
        //  OBTENER CON DETALLE: un pedido con sus líneas (al hacer clic)
        // ═══════════════════════════════════════════════════════════

        // Devuelve UN pedido, o null si no existe (por el "?").
        public Pedido? ObtenerConDetalle(int id)
        {
            using var db = new ViveroDbContext();           // abre la conexión

            return db.Pedidos                               // tabla Pedidos
                .AsNoTracking()                             // solo lectura: los pedidos no se editan
                .Include(p => p.Detalles)                   // "traé TAMBIÉN sus líneas" (JOIN con PedidoDetalles)
                .FirstOrDefault(p => p.Id == id);           // el pedido con ese Id, o null. ACÁ ejecuta el SQL
        }

        // ═══════════════════════════════════════════════════════════
        //  REGISTRAR: grabar cabecera + líneas en una TRANSACCIÓN
        // ═══════════════════════════════════════════════════════════

        // Recibe el pedido YA VALIDADO por la Controladora (con su lista Detalles cargada).
        // Devuelve el mismo pedido, ya grabado y con Numero asignado.
        public Pedido Registrar(Pedido pedido)
        {
            using var db = new ViveroDbContext();               // abre la conexión

            // Abre la TRANSACCIÓN: desde acá todo queda "en borrador" hasta el Commit.
            // Con "using": si algo falla y no se llegó al Commit, se deshace sola al salir.
            using var tx = db.Database.BeginTransaction();

            try                                                 // "intentá todo esto; si algo falla, andá al catch"
            {
                // Busca el número MÁS ALTO que ya existe (NO lo guarda: solo lo MIRA).
                // (int?) + ?? 0 → si la tabla está vacía (primer pedido), usa 0 en vez de dar error.
                var ultimo = db.Pedidos.Max(p => (int?)p.Numero) ?? 0;

                // Le pone al pedido NUEVO el número siguiente: si el último era 2, este es el 3.
                // Es el "correlativo controlado por la aplicación" que pide el enunciado.
                pedido.Numero = ultimo + 1;

                // Marca el pedido para insertar. Como trae su lista Detalles,
                // EF también marca TODAS sus líneas (y les completa el PedidoId solo).
                db.Pedidos.Add(pedido);

                // Ejecuta los INSERT: 1 para la cabecera + 1 por cada línea.
                // Todavía están en borrador (dentro de la transacción).
                db.SaveChanges();

                // CONFIRMA: ahora sí, cabecera y líneas quedan grabadas para siempre.
                tx.Commit();

                // Devuelve el pedido, que ya tiene Id (lo puso SQL) y Numero (lo pusimos nosotros).
                // La Vista lo usa para mostrar "Pedido registrado. Número: 3".
                return pedido;
            }
            catch                                               // si CUALQUIER línea del try falló, viene acá
            {
                // DESHACE todo lo hecho desde BeginTransaction: no queda ni cabecera ni líneas.
                tx.Rollback();

                // RELANZA el mismo error hacia la Controladora → Vista, para que el usuario se entere.
                // Sin esto, el error "desaparecería" y la Vista creería que salió bien.
                throw;
            }
        }
    }
}
