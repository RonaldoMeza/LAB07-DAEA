namespace Biblioteca.Entidades;

public class Prestamo
{
    public int PrestamoId { get; set; }
    public int SocioId { get; set; }
    public DateTime FechaPrestamo { get; set; }
    public DateTime FechaLimite { get; set; }
    public string Estado { get; set; } = "Prestado";

    // Libros que componen el préstamo (se llenan al registrar o consultar).
    public List<DetallePrestamo> Detalles { get; set; } = new();
}
