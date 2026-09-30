namespace Biblioteca.Negocio;

// Excepción propia: la capa de presentación solo conoce esta, nunca SqlException.
public class ReglaNegocioException : Exception
{
    public ReglaNegocioException(string message) : base(message) { }
}
