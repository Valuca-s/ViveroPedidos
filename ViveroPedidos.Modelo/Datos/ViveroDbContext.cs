using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ViveroPedidos.Modelo.Entidades;

namespace ViveroPedidos.Modelo.Datos
{
    public class ViveroDbContext : DbContext
    {
        public DbSet<Pedido> Pedidos => Set<Pedido>();
        public DbSet<PedidoDetalle> PedidoDetalles => Set<PedidoDetalle>();
        public DbSet<Planta> Plantas => Set<Planta>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                @"Data Source=PC-VALEN\SQLEXPRESS;Integrated Security=True;Persist Security Info=False;Pooling=False;
                Multiple Active Result Sets=False;Encrypt=False;Trust Server Certificate=False;Command Timeout=0"
             );

        }
    }
}
