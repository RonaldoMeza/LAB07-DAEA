using Biblioteca.Entidades;

namespace Biblioteca.Negocio;

// Punto extra: la interfaz se declara en la capa de Negocio, se implementa en
// la capa de Datos y se recibe por constructor en LibroNegocio.
public interface ILibroRepositorio
{
    Task<List<Libro>> ListarAsync();
    Task<List<Libro>> BuscarPorTituloAsync(string texto);
    Task<List<Libro>> BuscarPorAutorAsync(string texto);
    Task<Libro> ObtenerPorIdAsync(int libroId);
    Task<bool> ExisteIsbnAsync(string isbn, int excluirLibroId);
    Task<int> ContarPrestamosPendientesAsync(int libroId);
    Task InsertarAsync(Libro libro);
    Task ActualizarAsync(Libro libro);
    Task EliminarLogicoAsync(int libroId);
}
