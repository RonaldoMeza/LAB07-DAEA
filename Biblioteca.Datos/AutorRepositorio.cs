using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

// Capa de Datos: solo habla con la base de datos. No valida reglas ni muestra mensajes.
// Consultas siempre parametrizadas (nunca concatenadas) para evitar inyección SQL.
public class AutorRepositorio
{
    private const string SelectColumns =
        "SELECT AutorId, Nombre, Nacionalidad, Activo FROM Autores";

    public async Task<List<Autor>> ListarAsync()
    {
        const string sql = SelectColumns + " WHERE Activo = 1 ORDER BY Nombre";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        await cn.OpenAsync();
        return await LeerTodosAsync(cmd);
    }

    public async Task<Autor> ObtenerPorIdAsync(int autorId)
    {
        const string sql = SelectColumns + " WHERE AutorId = @Id";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", autorId);
        await cn.OpenAsync();
        return (await LeerTodosAsync(cmd)).FirstOrDefault();
    }

    private static async Task<List<Autor>> LeerTodosAsync(SqlCommand cmd)
    {
        var lista = new List<Autor>();
        using var dr = await cmd.ExecuteReaderAsync();
        while (await dr.ReadAsync())
        {
            lista.Add(new Autor
            {
                AutorId = dr.GetInt32(0),
                Nombre = dr.GetString(1),
                Nacionalidad = await dr.IsDBNullAsync(2) ? null : dr.GetString(2),
                Activo = dr.GetBoolean(3)
            });
        }
        return lista;
    }
}
