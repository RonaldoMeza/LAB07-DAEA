using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

// Repositorio de libros: lectura/escritura pura. Devuelve entidades o List<T>,
// nunca SqlDataReader ni DataTable. Los métodos que participan en el préstamo
// reciben la conexión y la transacción abiertas desde PrestamoRepositorio.
public class LibroRepositorio
{
    private const string SelectConAutor = @"
        SELECT l.LibroId, l.Titulo, l.ISBN, l.AutorId, l.Ejemplares, a.Nombre AS Autor
        FROM Libros l
        INNER JOIN Autores a ON a.AutorId = l.AutorId";

    public async Task<List<Libro>> ListarAsync()
    {
        const string sql = SelectConAutor + " WHERE l.Activo = 1 ORDER BY l.Titulo";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        await cn.OpenAsync();
        return await LeerTodosAsync(cmd);
    }

    // El comodín % se agrega al valor del parámetro, no al texto del SQL.
    public async Task<List<Libro>> BuscarPorTituloAsync(string texto)
    {
        const string sql = SelectConAutor + @"
            WHERE l.Activo = 1 AND l.Titulo LIKE @Texto
            ORDER BY l.Titulo";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Texto", $"%{texto}%");
        await cn.OpenAsync();
        return await LeerTodosAsync(cmd);
    }

    public async Task<List<Libro>> BuscarPorAutorAsync(string texto)
    {
        const string sql = SelectConAutor + @"
            WHERE l.Activo = 1 AND a.Nombre LIKE @Texto
            ORDER BY l.Titulo";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Texto", $"%{texto}%");
        await cn.OpenAsync();
        return await LeerTodosAsync(cmd);
    }

    public async Task<Libro> ObtenerPorIdAsync(int libroId)
    {
        const string sql = SelectConAutor + " WHERE l.LibroId = @Id";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", libroId);
        await cn.OpenAsync();
        return (await LeerTodosAsync(cmd)).FirstOrDefault();
    }

    // Se excluye el propio registro para que, al editar, no choque consigo mismo.
    public async Task<bool> ExisteIsbnAsync(string isbn, int excluirLibroId)
    {
        // Solo considerar libros activos: un libro dado de baja (Activo = 0)
        // no debe impedir volver a usar el mismo ISBN.
        const string sql = @"SELECT COUNT(1) FROM Libros
                             WHERE ISBN = @ISBN AND LibroId <> @Id AND Activo = 1";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@ISBN", isbn);
        cmd.Parameters.AddWithValue("@Id", excluirLibroId);
        await cn.OpenAsync();
        return (int)(await cmd.ExecuteScalarAsync()) > 0;
    }

    // Préstamos pendientes (FechaDevolucion en NULL) que contienen este libro.
    public async Task<int> ContarPrestamosPendientesAsync(int libroId)
    {
        const string sql = @"SELECT COUNT(1) FROM DetallePrestamo
                             WHERE LibroId = @Id AND FechaDevolucion IS NULL";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", libroId);
        await cn.OpenAsync();
        return (int)(await cmd.ExecuteScalarAsync());
    }

    public async Task InsertarAsync(Libro libro)
    {
        const string sql = @"INSERT INTO Libros (Titulo, ISBN, AutorId, Ejemplares)
                             VALUES (@Titulo, @ISBN, @AutorId, @Ejemplares)";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        AgregarParametros(cmd, libro);
        await cn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ActualizarAsync(Libro libro)
    {
        const string sql = @"UPDATE Libros
                             SET Titulo = @Titulo, ISBN = @ISBN, AutorId = @AutorId, Ejemplares = @Ejemplares
                             WHERE LibroId = @Id";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        AgregarParametros(cmd, libro);
        cmd.Parameters.AddWithValue("@Id", libro.LibroId);
        await cn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    // Eliminación lógica: nunca un DELETE físico.
    public async Task EliminarLogicoAsync(int libroId)
    {
        const string sql = "UPDATE Libros SET Activo = 0 WHERE LibroId = @Id";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", libroId);
        await cn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    // Las dos operaciones siguientes NO abren conexión: participan en la
    // transacción del préstamo que coordina PrestamoRepositorio.

    public async Task DescontarEjemplarAsync(int libroId, SqlConnection cn, SqlTransaction tx)
    {
        const string sql = @"UPDATE Libros SET Ejemplares = Ejemplares - 1
                             WHERE LibroId = @Id AND Ejemplares > 0";

        using var cmd = new SqlCommand(sql, cn, tx);
        cmd.Parameters.AddWithValue("@Id", libroId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DevolverEjemplarAsync(int libroId, SqlConnection cn, SqlTransaction tx)
    {
        const string sql = "UPDATE Libros SET Ejemplares = Ejemplares + 1 WHERE LibroId = @Id";

        using var cmd = new SqlCommand(sql, cn, tx);
        cmd.Parameters.AddWithValue("@Id", libroId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static void AgregarParametros(SqlCommand cmd, Libro libro)
    {
        cmd.Parameters.AddWithValue("@Titulo", libro.Titulo);
        cmd.Parameters.AddWithValue("@ISBN", libro.ISBN);
        cmd.Parameters.AddWithValue("@AutorId", libro.AutorId);
        cmd.Parameters.AddWithValue("@Ejemplares", libro.Ejemplares);
    }

    private static async Task<List<Libro>> LeerTodosAsync(SqlCommand cmd)
    {
        var lista = new List<Libro>();
        using var dr = await cmd.ExecuteReaderAsync();
        while (await dr.ReadAsync())
        {
            lista.Add(new Libro
            {
                LibroId = dr.GetInt32(0),
                Titulo = dr.GetString(1),
                ISBN = dr.GetString(2),
                AutorId = dr.GetInt32(3),
                Ejemplares = dr.GetInt32(4),
                NombreAutor = dr.GetString(5),
                Activo = true
            });
        }
        return lista;
    }
}
