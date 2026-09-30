using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

public class SocioRepositorio
{
    private const string SelectColumns =
        "SELECT SocioId, DNI, Nombre, Email, Activo FROM Socios";

    public async Task<List<Socio>> ListarAsync()
    {
        const string sql = SelectColumns + " WHERE Activo = 1 ORDER BY Nombre";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        await cn.OpenAsync();
        return await LeerTodosAsync(cmd);
    }

    public async Task<List<Socio>> BuscarPorNombreAsync(string texto)
    {
        const string sql = SelectColumns + @"
            WHERE Activo = 1 AND Nombre LIKE @Texto
            ORDER BY Nombre";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Texto", $"%{texto}%");
        await cn.OpenAsync();
        return await LeerTodosAsync(cmd);
    }

    public async Task<List<Socio>> BuscarPorDniAsync(string texto)
    {
        const string sql = SelectColumns + @"
            WHERE Activo = 1 AND DNI LIKE @Texto
            ORDER BY Nombre";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Texto", $"%{texto}%");
        await cn.OpenAsync();
        return await LeerTodosAsync(cmd);
    }

    public async Task<Socio> ObtenerPorIdAsync(int socioId)
    {
        const string sql = SelectColumns + " WHERE SocioId = @Id";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", socioId);
        await cn.OpenAsync();
        return (await LeerTodosAsync(cmd)).FirstOrDefault();
    }

    // Se excluye el propio registro para que, al editar, no choque consigo mismo.
    public async Task<bool> ExisteDniAsync(string dni, int excluirSocioId)
    {
        const string sql = @"SELECT COUNT(1) FROM Socios
                             WHERE DNI = @DNI AND SocioId <> @Id";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@DNI", dni);
        cmd.Parameters.AddWithValue("@Id", excluirSocioId);
        await cn.OpenAsync();
        return (int)(await cmd.ExecuteScalarAsync()) > 0;
    }

    // Libros pendientes (FechaDevolucion en NULL) que tiene el socio.
    public async Task<int> ContarPrestamosPendientesAsync(int socioId)
    {
        const string sql = @"SELECT COUNT(1)
                             FROM DetallePrestamo dp
                             INNER JOIN Prestamos p ON p.PrestamoId = dp.PrestamoId
                             WHERE p.SocioId = @Id AND dp.FechaDevolucion IS NULL";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", socioId);
        await cn.OpenAsync();
        return (int)(await cmd.ExecuteScalarAsync());
    }

    public async Task InsertarAsync(Socio socio)
    {
        const string sql = @"INSERT INTO Socios (DNI, Nombre, Email)
                             VALUES (@DNI, @Nombre, @Email)";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        AgregarParametros(cmd, socio);
        await cn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ActualizarAsync(Socio socio)
    {
        const string sql = @"UPDATE Socios
                             SET DNI = @DNI, Nombre = @Nombre, Email = @Email
                             WHERE SocioId = @Id";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        AgregarParametros(cmd, socio);
        cmd.Parameters.AddWithValue("@Id", socio.SocioId);
        await cn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    // Eliminación lógica: nunca un DELETE físico.
    public async Task EliminarLogicoAsync(int socioId)
    {
        const string sql = "UPDATE Socios SET Activo = 0 WHERE SocioId = @Id";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", socioId);
        await cn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    private static void AgregarParametros(SqlCommand cmd, Socio socio)
    {
        cmd.Parameters.AddWithValue("@DNI", socio.DNI);
        cmd.Parameters.AddWithValue("@Nombre", socio.Nombre);
        // Un null de C# no es un NULL de SQL: hay que enviar DBNull.Value.
        cmd.Parameters.AddWithValue("@Email", (object)socio.Email ?? DBNull.Value);
    }

    private static async Task<List<Socio>> LeerTodosAsync(SqlCommand cmd)
    {
        var lista = new List<Socio>();
        using var dr = await cmd.ExecuteReaderAsync();
        while (await dr.ReadAsync())
        {
            lista.Add(new Socio
            {
                SocioId = dr.GetInt32(0),
                DNI = dr.GetString(1),
                Nombre = dr.GetString(2),
                Email = await dr.IsDBNullAsync(3) ? null : dr.GetString(3),
                Activo = dr.GetBoolean(4)
            });
        }
        return lista;
    }
}
