using Biblioteca.Datos;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio;

// Adaptador: expone el LibroRepositorio concreto de la capa Datos a través de
// la interfaz ILibroRepositorio declarada en esta capa. Así LibroNegocio depende
// de la abstracción y no de la implementación de SQL Server.
// (Datos no puede implementar la interfaz directamente porque crearía una
// referencia circular: Negocio ya referencia a Datos.)
internal class LibroRepositorioAdaptador : ILibroRepositorio
{
    private readonly LibroRepositorio _repositorio = new();

    public Task<List<Libro>> ListarAsync() => _repositorio.ListarAsync();
    public Task<List<Libro>> BuscarPorTituloAsync(string texto) => _repositorio.BuscarPorTituloAsync(texto);
    public Task<List<Libro>> BuscarPorAutorAsync(string texto) => _repositorio.BuscarPorAutorAsync(texto);
    public Task<Libro> ObtenerPorIdAsync(int libroId) => _repositorio.ObtenerPorIdAsync(libroId);
    public Task<bool> ExisteIsbnAsync(string isbn, int excluirLibroId) => _repositorio.ExisteIsbnAsync(isbn, excluirLibroId);
    public Task<int> ContarPrestamosPendientesAsync(int libroId) => _repositorio.ContarPrestamosPendientesAsync(libroId);
    public Task InsertarAsync(Libro libro) => _repositorio.InsertarAsync(libro);
    public Task ActualizarAsync(Libro libro) => _repositorio.ActualizarAsync(libro);
    public Task EliminarLogicoAsync(int libroId) => _repositorio.EliminarLogicoAsync(libroId);
}
