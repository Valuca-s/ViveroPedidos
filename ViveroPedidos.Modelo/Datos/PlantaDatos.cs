using Microsoft.EntityFrameworkCore;                          // NECESARIO: AsNoTracking
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;     // SOBRA: es para generar código desde una base existente (Database First). No se usa
using Microsoft.Identity.Client;                              // SOBRA: es para login con cuentas de Microsoft. Nada que ver. Borrar
using System;                                                 // SOBRA en .NET 8: ya viene incluido
using System.Collections.Generic;                             // SOBRA en .NET 8: ya viene incluido (List<T>)
using System.ComponentModel.DataAnnotations;                  // SOBRA: es para anotaciones tipo [MaxLength] en entidades. Acá no se usa
using System.Linq;                                            // SOBRA en .NET 8: ya viene incluido (Where, Any, OrderBy...)
using System.Reflection.Metadata.Ecma335;                     // SOBRA: herramienta interna de .NET. Nada que ver. Borrar
using System.Text;                                            // SOBRA: plantilla de VS
using System.Threading.Tasks;                                 // SOBRA: plantilla de VS
using ViveroPedidos.Modelo.Entidades;                         // NECESARIO: clase Planta

// Proyecto Modelo, carpeta Datos
namespace ViveroPedidos.Modelo.Datos
{
    // Acceso a datos de plantas. ÚNICA clase que toca la tabla Plantas.
    // NO valida reglas de negocio: solo lee y graba. Las reglas están en PlantaControladora.
    public class PlantaDatos
    {
        // ═══════════════════════════════════════════════════════════
        //  LISTAR con búsqueda (grilla de FormPlantas)
        // ═══════════════════════════════════════════════════════════

        // Devuelve una lista de plantas. "filtro" puede venir null (por el "?").
        // 💡 "String" con mayúscula funciona igual que "string": son lo mismo.
        //    Por convención se usa "string" con minúscula.
        public List<Planta> Listar(String? filtro)
        {
            using var db = new ViveroDbContext();                       // abre conexión; se cierra al terminar

            // Consulta base: "todas las plantas", solo lectura, abierta para sumar filtros. No ejecuta todavía.
            var consulta = db.Plantas.AsNoTracking().AsQueryable();

            // "Si el filtro NO está vacío..." → si está vacío, saltea y trae TODAS (incluidas no disponibles)
            // 💡 Mejor IsNullOrWhiteSpace: también considera vacío un texto con solo espacios
            if (!string.IsNullOrEmpty(filtro))
            {
                var texto = filtro.Trim();                              // saca espacios: " ro " → "ro"

                // Agrega el filtro: la planta entra si el texto aparece en CUALQUIERA de los 3 campos
                consulta = consulta.Where(p =>
                    p.Codigo.Contains(texto) ||                         // en el código, O
                    p.NombreComun.Contains(texto) ||                    // en el nombre común, O
                    p.Especie.Contains(texto)                           // en la especie
                    );                                                  // SQL: WHERE Codigo LIKE '%ro%' OR ...
            }

            // Ordena alfabéticamente por nombre y RECIÉN ACÁ ejecuta el SELECT
            return consulta.OrderBy(p => p.NombreComun).ToList();
        }

        // ═══════════════════════════════════════════════════════════
        //  LISTAR DISPONIBLES (combo de FormRegistrarPedido)
        // ═══════════════════════════════════════════════════════════

        public List<Planta> ListarDisponibles()
        {
            using var db = new ViveroDbContext();

            return db.Plantas.AsNoTracking()                            // tabla Plantas, solo lectura
                .Where(p => p.Disponible)                               // solo las que tienen Disponible = true
                .OrderBy(p => p.NombreComun)                            // ordenadas por nombre (lo pide el enunciado)
                .ToList();                                              // ejecuta: SELECT ... WHERE Disponible = 1 ORDER BY NombreComun
        }

        // ═══════════════════════════════════════════════════════════
        //  OBTENER POR ID (para que la Controladora "mire" una planta)
        // ═══════════════════════════════════════════════════════════

        // Devuelve UNA planta o null. La usa la Controladora para decidir
        // (¿existe? ¿está disponible?) y también PedidoControladora al registrar.
        public Planta? ObtenerPorId(int id)
        {
            using var db = new ViveroDbContext();
            return db.Plantas.AsNoTracking().FirstOrDefault(p => p.Id == id);   // la planta con ese Id, o null
        }

        // ═══════════════════════════════════════════════════════════
        //  ¿EXISTE EL CÓDIGO? (para validar código único)
        // ═══════════════════════════════════════════════════════════

        // Devuelve true/false.
        // excluirId: opcional (= null). En una MODIFICACIÓN se pasa el Id de la planta que se edita,
        // para que no "choque" con su propio código.
        public bool ExisteCodigo(string codigo, int? excluirId = null)
        {
            using var db = new ViveroDbContext();

            var cod = codigo.Trim().ToUpper();                          // " r01 " → "R01"

            // Any = "¿hay al menos una planta que cumpla...?"
            //   p.Codigo.ToUpper() == cod    → mismo código, SIN distinguir mayúsculas
            //   &&                           → Y además...
            //   (excluirId == null           → si es un ALTA (no hay nada que excluir), cualquier planta cuenta
            //    || p.Id != excluirId)       → si es una MODIFICACIÓN, ignora la planta que se está editando
            bool existe = db.Plantas.Any(p => p.Codigo.ToUpper() == cod && (excluirId == null || p.Id != excluirId));

            return existe;                                              // true = el código ya está usado
        }

        // ═══════════════════════════════════════════════════════════
        //  ¿TIENE DETALLES? (para no eliminar una planta usada)
        // ═══════════════════════════════════════════════════════════

        // true si la planta aparece en al menos una línea de algún pedido
        public bool TieneDetalles(int plantaId)
        {
            using var db = new ViveroDbContext();
            return db.PedidoDetalles.Any(d => d.PlantaId == plantaId); // busca en la tabla de LÍNEAS, no en Plantas
        }

        // ═══════════════════════════════════════════════════════════
        //  AGREGAR (INSERT)
        // ═══════════════════════════════════════════════════════════

        // Recibe la planta YA ARMADA y validada por la Controladora (con Disponible = true)
        public void Agregar(Planta planta)
        {
            using var db = new ViveroDbContext();
            db.Plantas.Add(planta);                                     // la marca como "nueva" (todavía no grabó)
            db.SaveChanges();                                           // ejecuta el INSERT; SQL le asigna el Id
        }

        // ═══════════════════════════════════════════════════════════
        //  MODIFICAR (UPDATE de todos los campos)
        // ═══════════════════════════════════════════════════════════

        public void Modificar(int id, string codigo, string nombreComun, string especie, bool disponible)
        {
            using var db = new ViveroDbContext();

            // Find = busca por Id y la deja VIGILADA (por eso acá NO va AsNoTracking).
            // Si no existe → corta con mensaje (red de seguridad).
            var planta = db.Plantas.Find(id)
                ?? throw new InvalidOperationException("la planta no existe");     // 💡 mejor: "La planta no existe."

            // Cambia los valores en memoria. EF anota cada cambio porque la está vigilando.
            planta.Codigo = codigo;
            planta.NombreComun = nombreComun;
            planta.Especie = especie;
            planta.Disponible = disponible;                             // en la modificación SÍ se guarda el checkbox

            db.SaveChanges();                                           // ejecuta el UPDATE con lo que cambió
        }

        // ═══════════════════════════════════════════════════════════
        //  CAMBIAR SOLO DISPONIBLE (botón "No disponible")
        // ═══════════════════════════════════════════════════════════

        // Cambia ÚNICAMENTE Disponible, sin tocar código, nombre ni especie
        public void CambiarDisponible(int id, bool disponible)
        {
            using var db = new ViveroDbContext();

            var planta = db.Plantas.Find(id)                            // busca y vigila
                ?? throw new InvalidOperationException("la planta no existe");

            planta.Disponible = disponible;                             // cambia solo este campo
            db.SaveChanges();                                           // UPDATE Plantas SET Disponible = 0 WHERE Id = ...
        }

        // ═══════════════════════════════════════════════════════════
        //  ELIMINAR (DELETE físico)
        // ═══════════════════════════════════════════════════════════

        // La Controladora ya verificó con TieneDetalles que no esté en ningún pedido.
        // Igual, si lo estuviera, SQL lo rechazaría por el Restrict del DbContext.
        public void Eliminar(int id)
        {
            using var db = new ViveroDbContext();

            var planta = db.Plantas.Find(id)                            // busca y vigila
                ?? throw new InvalidOperationException("no existe la plantita");   // 💡 mejor: "La planta no existe."

            db.Plantas.Remove(planta);                                  // la marca para borrar
            db.SaveChanges();                                           // ejecuta el DELETE
        }
    }
}