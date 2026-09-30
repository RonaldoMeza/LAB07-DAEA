namespace Biblioteca.Entidades;

public class DetallePrestamo
{
    public int PrestamoId { get; set; }
    public int LibroId { get; set; }
    public DateTime? FechaDevolucion { get; set; }

    // Datos de apoyo que llenan las consultas con JOIN (no son columnas de la tabla).
    public string TituloLibro { get; set; } = string.Empty;
}
