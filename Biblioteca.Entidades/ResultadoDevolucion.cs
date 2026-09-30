namespace Biblioteca.Entidades;

// Resultado de registrar una devolución: la multa calculada por Negocio
// y el estado en que quedó el préstamo.
public class ResultadoDevolucion
{
    public decimal Multa { get; set; }
    public int DiasRetraso { get; set; }
    public string EstadoPrestamo { get; set; } = string.Empty;
}
