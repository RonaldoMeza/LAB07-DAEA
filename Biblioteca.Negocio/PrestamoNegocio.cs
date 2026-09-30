using Biblioteca.Datos;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio;

public class PrestamoNegocio
{
    // Parámetros del negocio.
    public const int MaxLibrosPendientes = 3;
    public const int DiasPrestamo = 14;
    public const decimal MultaPorDia = 1.50m;

    private readonly PrestamoRepositorio _prestamos = new();
    private readonly SocioRepositorio _socios = new();
    private readonly LibroRepositorio _libros = new();

    public Task<List<Socio>> ListarSociosAsync() => _socios.ListarAsync();

    public Task<List<Libro>> ListarLibrosAsync() => _libros.ListarAsync();

    // Registra un préstamo con uno o varios libros validando todas las reglas.
    public async Task<int> RegistrarPrestamoAsync(int socioId, List<int> librosIds)
    {
        if (librosIds == null || librosIds.Count == 0)
            throw new ReglaNegocioException("Debe agregar al menos un libro al préstamo.");

        if (librosIds.Distinct().Count() != librosIds.Count)
            throw new ReglaNegocioException("No se puede agregar el mismo libro dos veces al préstamo.");

        // No se puede prestar a un socio con Activo = 0.
        var socio = await _socios.ObtenerPorIdAsync(socioId);
        if (socio == null)
            throw new ReglaNegocioException("Debe seleccionar un socio válido.");

        if (!socio.Activo)
            throw new ReglaNegocioException($"El socio {socio.Nombre} está dado de baja y no puede recibir préstamos.");

        // Un socio no puede tener más de 3 libros pendientes.
        int pendientes = await _socios.ContarPrestamosPendientesAsync(socioId);
        if (pendientes + librosIds.Count > MaxLibrosPendientes)
            throw new ReglaNegocioException(
                $"El socio {socio.Nombre} ya tiene {pendientes} libro(s) pendiente(s). " +
                $"Con este préstamo superaría el máximo de {MaxLibrosPendientes} libros.");

        // Validar cada libro: activo y con ejemplares disponibles.
        foreach (var libroId in librosIds)
        {
            var libro = await _libros.ObtenerPorIdAsync(libroId);
            if (libro == null)
                throw new ReglaNegocioException($"No se encontró el libro con Id {libroId}.");

            if (!libro.Activo)
                throw new ReglaNegocioException($"El libro \"{libro.Titulo}\" está dado de baja.");

            if (libro.Ejemplares <= 0)
                throw new ReglaNegocioException($"El libro \"{libro.Titulo}\" no tiene ejemplares disponibles.");
        }

        var prestamo = new Prestamo
        {
            SocioId = socioId,
            FechaPrestamo = DateTime.Now,
            FechaLimite = DateTime.Now.AddDays(DiasPrestamo),
            Detalles = librosIds.Select(id => new DetallePrestamo { LibroId = id }).ToList()
        };

        // Cabecera + detalles + descuento de ejemplares: una sola transacción.
        return await _prestamos.RegistrarPrestamoAsync(prestamo);
    }

    // Pendientes de un socio con la multa calculada por la capa de Negocio.
    public async Task<List<DetallePendiente>> ListarPendientesPorSocioAsync(int socioId)
    {
        var pendientes = await _prestamos.ListarPendientesPorSocioAsync(socioId);
        foreach (var p in pendientes)
        {
            p.DiasRetraso = CalcularDiasRetraso(p.FechaLimite, DateTime.Now);
            p.Multa = CalcularMulta(p.FechaLimite, DateTime.Now);
        }
        return pendientes;
    }

    // Registra la devolución: guarda la fecha, devuelve el ejemplar al stock
    // y devuelve la multa calculada (S/ 1.50 por día de retraso).
    public async Task<ResultadoDevolucion> RegistrarDevolucionAsync(int prestamoId, int libroId)
    {
        var fecha = DateTime.Now;

        // Para calcular la multa se necesita la fecha límite de la cabecera.
        var fechaLimite = await _prestamos.ObtenerFechaLimiteAsync(prestamoId);
        if (fechaLimite == null)
            throw new ReglaNegocioException("No se encontró el préstamo seleccionado.");

        await _prestamos.RegistrarDevolucionAsync(prestamoId, libroId, fecha);

        return new ResultadoDevolucion
        {
            DiasRetraso = CalcularDiasRetraso(fechaLimite.Value, fecha),
            Multa = CalcularMulta(fechaLimite.Value, fecha),
            EstadoPrestamo = "Devolución registrada"
        };
    }

    // Reporte de préstamos por intervalo de fechas (INNER JOIN de 4 tablas).
    public async Task<List<PrestamoReporte>> ReportePorFechasAsync(DateTime desde, DateTime hasta)
    {
        if (hasta < desde)
            throw new ReglaNegocioException("La fecha final no puede ser menor que la fecha inicial.");

        return await _prestamos.ReportePorFechasAsync(desde, hasta);
    }

    // Multa por retraso: S/ 1.50 por cada día vencido. El cálculo vive aquí, en Negocio.
    public static decimal CalcularMulta(DateTime fechaLimite, DateTime fechaDevolucion) =>
        MultaPorDia * CalcularDiasRetraso(fechaLimite, fechaDevolucion);

    public static int CalcularDiasRetraso(DateTime fechaLimite, DateTime fechaDevolucion)
    {
        int dias = (fechaDevolucion.Date - fechaLimite.Date).Days;
        return Math.Max(0, dias);
    }
}
