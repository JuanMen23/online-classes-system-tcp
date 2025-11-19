using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Services
{
    public class DailyReportService
    {
        private readonly ClassService _classService;
        private readonly string _imageDirectory;

        public DailyReportService(ClassService classService, string imageDirectory)
        {
            _classService = classService;
            _imageDirectory = imageDirectory;
        }

        public async Task<string> GenerateReportAsync(CancellationToken token)
        {
            var classes = _classService.GetAllClasses()
                .Where(c => c.StartDateTime.Date == DateTime.Now.Date)
                .ToList();

            if (classes.Count == 0)
                return "No hay clases programadas para hoy.";

            int totalClases = classes.Count;
            double promedioDuracion = 0;
            int totalInscriptos = 0;
            int clasesConImagen = 0;
            long totalTamañoImagenes = 0;

            await Task.Run(() =>
            {
                Parallel.ForEach(classes, (clase, state) =>
                {
                    if (token.IsCancellationRequested)
                        state.Stop();

                    Interlocked.Add(ref totalInscriptos, clase.EnrolledCount);

                    var imagePath = clase.ImagePath;
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        // Normalizar separadores a estilo del SO
                        imagePath = imagePath.Replace('\\', '/');

                        // Si viene con "Images/..." quitar ese prefijo por si guardaron mal
                        if (imagePath.StartsWith("Images/", StringComparison.OrdinalIgnoreCase))
                            imagePath = imagePath.Substring("Images/".Length).TrimStart('/', '\\');

                        string fullPath;
                        if (Path.IsPathRooted(imagePath))
                        {
                            fullPath = Path.GetFullPath(imagePath);
                        }
                        else
                        {
                            // usar el directorio que se pasó por configuración (por ejemplo "/app/Images")
                            fullPath = Path.Combine(_imageDirectory, imagePath);
                            fullPath = Path.GetFullPath(fullPath);
                        }

                        Console.WriteLine($"DEBUG ORIGINAL IMAGEPATH = '{clase.ImagePath}'");
                        Console.WriteLine($"DEBUG NORMALIZED IMAGEPATH = '{imagePath}'");
                        Console.WriteLine($"DEBUG FULL PATH = '{fullPath}'");
                        Console.WriteLine($"FILE EXISTS? {File.Exists(fullPath)}");

                        if (File.Exists(fullPath))
                        {
                            Interlocked.Increment(ref clasesConImagen);
                            Interlocked.Add(ref totalTamañoImagenes, new FileInfo(fullPath).Length);
                        }
                    }
                });

                promedioDuracion = classes.Average(c => c.DurationMinutes);
            }, token);


            double promedioInscriptos = totalInscriptos / (double)totalClases;
            double promedioTamañoImagenes = clasesConImagen > 0 ? totalTamañoImagenes / (double)clasesConImagen : 0;

            return
                "📅 REPORTE DE CLASES DEL DÍA\n" +
                $"Total de clases: {totalClases}\n" +
                $"Promedio de duración: {promedioDuracion:F2} min\n" +
                $"Total de inscriptos: {totalInscriptos}\n" +
                $"Promedio de inscriptos: {promedioInscriptos:F2}\n" +
                $"Clases con imagen: {clasesConImagen}\n" +
                $"Tamaño total de imágenes: {totalTamañoImagenes / 1024.0:F2} KB\n" +
                $"Promedio de tamaño de imágenes: {promedioTamañoImagenes / 1024.0:F2} KB";
        }
    }
}
