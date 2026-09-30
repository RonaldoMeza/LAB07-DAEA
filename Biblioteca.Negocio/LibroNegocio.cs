using Biblioteca.Datos;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio;

// Capa de Negocio para libros: valida las reglas antes de llamar a Datos.
// Depende de la interfaz ILibroRepositorio (recibida por constructor), no de SQL.
public class LibroNegocio
{
    private const int MaxIsbnLength = 20;

    private readonly ILibroRepositorio _libros;

    // Constructor por defecto: usa la implementación con SQL Server vía adaptador.
    public LibroNegocio() : this(new LibroRepositorioAdaptador()) { }

    // Inyección manual: Negocio depende de la interfaz, no de la clase concreta.
    public LibroNegocio(ILibroRepositorio libros)
    {
        _libros = libros;
    }

    public Task<List<Libro>> ListarAsync() => _libros.ListarAsync();

    public Task<List<Libro>> BuscarPorTituloAsync(string texto) =>
        string.IsNullOrWhiteSpace(texto)
            ? ListarAsync()
            : _libros.BuscarPorTituloAsync(texto.Trim());

    public Task<List<Libro>> BuscarPorAutorAsync(string texto) =>
        string.IsNullOrWhiteSpace(texto)
            ? ListarAsync()
            : _libros.BuscarPorAutorAsync(texto.Trim());

    public Task<List<Autor>> ListarAutoresAsync() => new AutorRepositorio().ListarAsync();

    public async Task RegistrarAsync(Libro libro)
    {
        await ValidarAsync(libro, excluirLibroId: 0);
        await _libros.InsertarAsync(libro);
    }

    public async Task ActualizarAsync(Libro libro)
    {
        if (libro.LibroId <= 0)
            throw new ReglaNegocioException("Debe seleccionar un libro para actualizar.");

        await ValidarAsync(libro, libro.LibroId);
        await _libros.ActualizarAsync(libro);
    }

    public async Task EliminarAsync(int libroId)
    {
        if (libroId <= 0)
            throw new ReglaNegocioException("Debe seleccionar un libro.");

        // No se puede dar de baja a un libro con préstamos pendientes.
        int pendientes = await _libros.ContarPrestamosPendientesAsync(libroId);
        if (pendientes > 0)
            throw new ReglaNegocioException(
                $"No se puede dar de baja el libro: tiene {pendientes} préstamo(s) pendiente(s) de devolución.");

        await _libros.EliminarLogicoAsync(libroId);
    }

    private async Task ValidarAsync(Libro libro, int excluirLibroId)
    {
        // Normalizar antes de validar.
        libro.Titulo = libro.Titulo?.Trim();
        libro.ISBN = libro.ISBN?.Trim();

        if (string.IsNullOrWhiteSpace(libro.Titulo))
            throw new ReglaNegocioException("El título del libro es obligatorio.");

        if (string.IsNullOrWhiteSpace(libro.ISBN))
            throw new ReglaNegocioException("El ISBN es obligatorio.");

        if (libro.ISBN.Length > MaxIsbnLength)
            throw new ReglaNegocioException($"El ISBN no puede superar los {MaxIsbnLength} caracteres.");

        if (libro.AutorId <= 0)
            throw new ReglaNegocioException("Debe seleccionar un autor.");

        if (libro.Ejemplares < 0)
            throw new ReglaNegocioException("El número de ejemplares no puede ser negativo.");

        // No se permite repetir un ISBN.
        if (await _libros.ExisteIsbnAsync(libro.ISBN, excluirLibroId))
            throw new ReglaNegocioException($"Ya existe un libro registrado con el ISBN {libro.ISBN}.");
    }
}
