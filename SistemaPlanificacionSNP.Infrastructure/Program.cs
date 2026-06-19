using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Infrastructure.Data;

namespace SistemaPlanificacionSNP.Infrastructure
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Este proyecto es solo de infraestructura y contiene el DbContext
            // No debe ejecutarse como aplicación independiente
            Console.WriteLine("SistemaPlanificacionSNP.Infrastructure - Librería de Infraestructura");
        }
    }
}
