namespace Biblioteca.Entidades;

// Fila del reporte de préstamos por intervalo de fechas.
// La llena un INNER JOIN entre Prestamos, DetallePrestamo, Libros y Socios.
public class PrestamoReporte
{
    public int PrestamoId { get; set; }
    public string Socio { get; set; } = string.Empty;
    public string Libro { get; set; } = string.Empty;
    public DateTime FechaPrestamo { get; set; }
    public DateTime FechaLimite { get; set; }
    public string Estado { get; set; } = string.Empty;
}
