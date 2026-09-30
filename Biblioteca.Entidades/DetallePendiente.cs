namespace Biblioteca.Entidades;

// Libro pendiente de devolución de un socio.
// Multa y DiasRetraso los calcula la capa de Negocio (S/ 1.50 por día de retraso).
public class DetallePendiente
{
    public int PrestamoId { get; set; }
    public int LibroId { get; set; }
    public string TituloLibro { get; set; } = string.Empty;
    public DateTime FechaPrestamo { get; set; }
    public DateTime FechaLimite { get; set; }
    public int DiasRetraso { get; set; }
    public decimal Multa { get; set; }
}
