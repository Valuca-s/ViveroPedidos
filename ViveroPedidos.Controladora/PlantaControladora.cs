// ═══════════════════════════════════════════════════════════════════
//  USINGS: importan herramientas de otros lugares
// ═══════════════════════════════════════════════════════════════════

using Microsoft.EntityFrameworkCore;                // ⚠️ BORRAR: es Entity Framework, y EF NO puede aparecer en la Controladora
using System;                                       // SOBRA en .NET 8: ya viene incluido (InvalidOperationException)
using System.Collections.Generic;                   // SOBRA en .NET 8: ya viene incluido (List<T>)
using System.ComponentModel.DataAnnotations;        // BORRAR: anotaciones tipo [MaxLength], no se usan
using System.Linq;                                  // SOBRA en .NET 8 (y acá no se usa LINQ)
using System.Text;                                  // SOBRA: plantilla de VS
using System.Threading.Tasks;                       // SOBRA: plantilla de VS
using ViveroPedidos.Modelo.Datos;                   // NECESARIO: para usar PlantaDatos
using ViveroPedidos.Modelo.Entidades;               // NECESARIO: para usar Planta

// Namespace: proyecto Controladora. Tiene que ser EXACTAMENTE este
// porque la Vista hace "using ViveroPedidos.Controladora;"
namespace ViveroPedidos.Controladora
{
    // Clase de LÓGICA DE NEGOCIO de plantas.
    // Valida las reglas del enunciado (sección 5.1) y le pide al Modelo que grabe o busque.
    // NO usa Entity Framework. public = la Vista la puede usar.
    public class PlantaControladora
    {
        // ═══════════════════════════════════════════════════════════
        //  CAMPO: la clase de datos que usa esta controladora
        // ═══════════════════════════════════════════════════════════

        // Objeto de PlantaDatos: a él le pregunta y le pide todo lo que tenga que ver con la base.
        // private  = solo esta clase lo usa.
        // readonly = se crea una vez y no se puede reemplazar.
        // new()    = es lo mismo que "new PlantaDatos()".
        private readonly PlantaDatos _datos = new();

        // ═══════════════════════════════════════════════════════════
        //  LISTAR con búsqueda (grilla de FormPlantas)
        // ═══════════════════════════════════════════════════════════

        // Devuelve una lista de plantas.
        // string? filtro = null → el filtro es opcional y puede venir null (así lo exige el contrato).
        public List<Planta> Listar(string? filtro = null)
        {
            // No hay reglas que validar: le pasa el filtro al Modelo
            // y devuelve directo a la Vista la lista que recibe.
            return _datos.Listar(filtro);
        }

        // ═══════════════════════════════════════════════════════════
        //  LISTAR DISPONIBLES (combo de FormRegistrarPedido)
        // ═══════════════════════════════════════════════════════════

        public List<Planta> ListarDisponibles()
        {
            // Tampoco hay reglas: delega al Modelo, que filtra Disponible = true y ordena por nombre
            return _datos.ListarDisponibles();
        }

        // ═══════════════════════════════════════════════════════════
        //  ALTA (botón "Agregar")
        // ═══════════════════════════════════════════════════════════

        // void = no devuelve nada: graba, o corta con un error.
        // Recibe los 3 textos tal como los escribió el usuario (pueden traer espacios).
        public void GuardarNuevo(string codigo, string nombreComun, string especie)
        {
            // 1. Llama al método privado de abajo: verifica que no estén vacíos y que no sean muy largos.
            //    Si algo está mal, ValidarCampos tira el error y este método se corta acá.
            ValidarCampos(codigo, nombreComun, especie);

            // 2. Saca los espacios de los extremos de cada texto: "  R01 " → "R01"
            var cod = codigo.Trim();
            var nom = nombreComun.Trim();
            var esp = especie.Trim();

            // 3. Le PREGUNTA al Modelo: "¿ya hay una planta con este código?" (sin distinguir mayúsculas).
            //    Si devuelve true → corta con el error. $"...{cod}..." mete el código en el mensaje.
            if (_datos.ExisteCodigo(cod))
                throw new InvalidOperationException($"Ya existe una planta con el código '{cod}'.");

            // 4. Todo está bien: crea el objeto Planta y le carga los datos en un solo paso
            var planta = new Planta
            {
                Codigo = cod,                   // código ya limpio
                NombreComun = nom,              // nombre ya limpio
                Especie = esp,                  // especie ya limpia
                Disponible = true               // en el ALTA siempre true (el enunciado dice que el checkbox no aplica)
                // Id no se pone: lo asigna SQL al insertar (identity)
            };

            // 5. Le PIDE al Modelo que la grabe (Add + SaveChanges → INSERT)
            _datos.Agregar(planta);
        }

        // ═══════════════════════════════════════════════════════════
        //  MODIFICACIÓN (botón "Modificar")
        // ═══════════════════════════════════════════════════════════

        // Recibe el Id de la planta seleccionada, los textos nuevos y el valor del checkbox
        public void Modificar(int id, string codigo, string nombreComun, string especie, bool disponible)
        {
            // Le pide la planta al Modelo. Si devuelve null (is null), no existe → error.
            // (La sección 6 pide un mensaje claro si el id no existe.)
            if (_datos.ObtenerPorId(id) is null)
                throw new InvalidOperationException("La planta seleccionada no existe.");

            // Mismas validaciones que en el alta: vacíos y longitudes
            ValidarCampos(codigo, nombreComun, especie);

            // Limpia espacios
            var cod = codigo.Trim();
            var nom = nombreComun.Trim();
            var esp = especie.Trim();

            // Pregunta si el código está repetido, pero pasándole el id para EXCLUIR a esta misma planta.
            // Sin eso, si el usuario no cambia el código, "chocaría" consigo misma y daría error.
            if (_datos.ExisteCodigo(cod, id))
                throw new InvalidOperationException($"Ya existe otra planta con el código '{cod}'.");

            // Le pide al Modelo que actualice. Acá SÍ se manda el valor del checkbox (disponible),
            // como pide el enunciado para la modificación.
            _datos.Modificar(id, cod, nom, esp, disponible);
        }

        // ═══════════════════════════════════════════════════════════
        //  NO DISPONIBLE (botón "No disponible")
        // ═══════════════════════════════════════════════════════════

        public void MarcarNoDisponible(int id)
        {
            // Le pide la planta al Modelo para MIRAR cómo está.
            // ?? throw → si volvió null (no existe), corta con el mensaje.
            var planta = _datos.ObtenerPorId(id)
                ?? throw new InvalidOperationException("La planta seleccionada no existe.");

            // Si ya está en false (el "!" invierte: !false = true → entra) → informa con un error.
            // Lo pide el enunciado: "Si ya estaba en false, informar con un mensaje claro".
            if (!planta.Disponible)
                throw new InvalidOperationException($"La planta '{planta.NombreComun}' ya estaba marcada como no disponible.");

            // Existe y está disponible: le pide al Modelo que cambie SOLO Disponible a false.
            // No toca código, nombre ni especie (no guarda los otros cuadros de texto).
            _datos.CambiarDisponible(id, false);
        }

        // ═══════════════════════════════════════════════════════════
        //  ELIMINAR (botón "Eliminar")
        // ═══════════════════════════════════════════════════════════

        public void Eliminar(int id)
        {
            // Le pide la planta al Modelo. Si no existe → error.
            // (Se guarda en "planta" para poder usar su nombre en el mensaje de abajo.)
            var planta = _datos.ObtenerPorId(id)
                ?? throw new InvalidOperationException("La planta seleccionada no existe.");

            // Le PREGUNTA al Modelo: "¿esta planta aparece en alguna línea de algún pedido?"
            // Si devuelve true → NO se puede borrar (eliminación física solo si no está referenciada).
            if (_datos.TieneDetalles(id))
                throw new InvalidOperationException(
                    $"No se puede eliminar '{planta.NombreComun}' porque figura en pedidos registrados. " +   // el "+" une los dos textos
                    "Puede marcarla como no disponible.");                                                     // en un solo mensaje

            // No está en ningún pedido: le pide al Modelo que la borre (Remove + SaveChanges → DELETE)
            _datos.Eliminar(id);
        }

        // ═══════════════════════════════════════════════════════════
        //  VALIDACIÓN COMÚN (la usan GuardarNuevo y Modificar)
        // ═══════════════════════════════════════════════════════════

        // private = solo se usa dentro de esta clase (la Vista no lo ve; no es parte del contrato).
        // static  = no usa nada del objeto (no usa _datos): solo mira los textos que recibe.
        // void    = no devuelve nada: si algo está mal tira el error; si todo está bien, no hace nada.
        // Existe para NO repetir las mismas 6 validaciones en el alta y en la modificación.
        private static void ValidarCampos(string codigo, string nombreComun, string especie)
        {
            // ---- Campos obligatorios ----
            // IsNullOrWhiteSpace → true si es null, "" o solo espacios "   "
            if (string.IsNullOrWhiteSpace(codigo))
                throw new InvalidOperationException("El código es obligatorio.");
            if (string.IsNullOrWhiteSpace(nombreComun))
                throw new InvalidOperationException("El nombre común es obligatorio.");
            if (string.IsNullOrWhiteSpace(especie))
                throw new InvalidOperationException("La especie es obligatoria.");

            // ---- Longitudes máximas ----
            // Las mismas que se configuraron en el DbContext (HasMaxLength).
            // Se mide DESPUÉS de Trim, porque lo que se graba es el texto sin espacios.
            // Sin esto, un texto largo haría fallar el INSERT con un error técnico de SQL en inglés.
            if (codigo.Trim().Length > 20)
                throw new InvalidOperationException("El código no puede superar los 20 caracteres.");
            if (nombreComun.Trim().Length > 120)
                throw new InvalidOperationException("El nombre común no puede superar los 120 caracteres.");
            if (especie.Trim().Length > 120)
                throw new InvalidOperationException("La especie no puede superar los 120 caracteres.");
        }
    }
}