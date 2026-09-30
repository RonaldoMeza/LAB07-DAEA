using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

public class PrestamoRepositorio
{
    // Registra cabecera + detalles + descuento de ejemplares dentro de una
    // sola transacción: si alguna operación falla, no se guarda ningún cambio.
    public async Task<int> RegistrarPrestamoAsync(Prestamo prestamo)
    {
        using var cn = DbConnectionFactory.Create();
        await cn.OpenAsync();
        using var tx = cn.BeginTransaction();

        try
        {
            const string sqlCabecera = @"INSERT INTO Prestamos (SocioId, FechaPrestamo, FechaLimite, Estado)
                                         VALUES (@SocioId, @FechaPrestamo, @FechaLimite, 'Prestado');
                                         SELECT CAST(SCOPE_IDENTITY() AS INT);";

            int prestamoId;
            using (var cmd = new SqlCommand(sqlCabecera, cn, tx))
            {
                cmd.Parameters.AddWithValue("@SocioId", prestamo.SocioId);
                cmd.Parameters.AddWithValue("@FechaPrestamo", prestamo.FechaPrestamo);
                cmd.Parameters.AddWithValue("@FechaLimite", prestamo.FechaLimite);
                prestamoId = (int)(await cmd.ExecuteScalarAsync());
            }

            var libros = new LibroRepositorio();
            foreach (var detalle in prestamo.Detalles)
            {
                const string sqlDetalle = @"INSERT INTO DetallePrestamo (PrestamoId, LibroId)
                                            VALUES (@PrestamoId, @LibroId)";

                using (var cmd = new SqlCommand(sqlDetalle, cn, tx))
                {
                    cmd.Parameters.AddWithValue("@PrestamoId", prestamoId);
                    cmd.Parameters.AddWithValue("@LibroId", detalle.LibroId);
                    await cmd.ExecuteNonQueryAsync();
                }

                await libros.DescontarEjemplarAsync(detalle.LibroId, cn, tx);
            }

            await tx.CommitAsync();
            return prestamoId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // Registra la devolución de un libro y, si ya no quedan pendientes,
    // marca el préstamo como Devuelto. Todo en una sola transacción.
    public async Task RegistrarDevolucionAsync(int prestamoId, int libroId, DateTime fechaDevolucion)
    {
        using var cn = DbConnectionFactory.Create();
        await cn.OpenAsync();
        using var tx = cn.BeginTransaction();

        try
        {
            const string sqlDevolucion = @"UPDATE DetallePrestamo
                                           SET FechaDevolucion = @Fecha
                                           WHERE PrestamoId = @PrestamoId AND LibroId = @LibroId";

            using (var cmd = new SqlCommand(sqlDevolucion, cn, tx))
            {
                cmd.Parameters.AddWithValue("@Fecha", fechaDevolucion);
                cmd.Parameters.AddWithValue("@PrestamoId", prestamoId);
                cmd.Parameters.AddWithValue("@LibroId", libroId);
                await cmd.ExecuteNonQueryAsync();
            }

            await new LibroRepositorio().DevolverEjemplarAsync(libroId, cn, tx);

            const string sqlPendientes = @"SELECT COUNT(1) FROM DetallePrestamo
                                           WHERE PrestamoId = @PrestamoId AND FechaDevolucion IS NULL";

            int pendientes;
            using (var cmd = new SqlCommand(sqlPendientes, cn, tx))
            {
                cmd.Parameters.AddWithValue("@PrestamoId", prestamoId);
                pendientes = (int)(await cmd.ExecuteScalarAsync());
            }

            if (pendientes == 0)
            {
                const string sqlEstado = @"UPDATE Prestamos SET Estado = 'Devuelto'
                                           WHERE PrestamoId = @PrestamoId";

                using var cmd = new SqlCommand(sqlEstado, cn, tx);
                cmd.Parameters.AddWithValue("@PrestamoId", prestamoId);
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // Libros con FechaDevolucion en NULL de un socio (para el formulario de devolución).
    public async Task<List<DetallePendiente>> ListarPendientesPorSocioAsync(int socioId)
    {
        const string sql = @"
            SELECT dp.PrestamoId, dp.LibroId, l.Titulo, p.FechaPrestamo, p.FechaLimite
            FROM DetallePrestamo dp
            INNER JOIN Prestamos p ON p.PrestamoId = dp.PrestamoId
            INNER JOIN Libros l ON l.LibroId = dp.LibroId
            WHERE p.SocioId = @SocioId AND dp.FechaDevolucion IS NULL
            ORDER BY p.FechaLimite";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@SocioId", socioId);
        await cn.OpenAsync();

        var lista = new List<DetallePendiente>();
        using var dr = await cmd.ExecuteReaderAsync();
        while (await dr.ReadAsync())
        {
            lista.Add(new DetallePendiente
            {
                PrestamoId = dr.GetInt32(0),
                LibroId = dr.GetInt32(1),
                TituloLibro = dr.GetString(2),
                FechaPrestamo = dr.GetDateTime(3),
                FechaLimite = dr.GetDateTime(4)
            });
        }
        return lista;
    }

    // Fecha límite de la cabecera del préstamo (la usa Negocio para calcular la multa).
    public async Task<DateTime?> ObtenerFechaLimiteAsync(int prestamoId)
    {
        const string sql = "SELECT FechaLimite FROM Prestamos WHERE PrestamoId = @Id";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", prestamoId);
        await cn.OpenAsync();
        var resultado = await cmd.ExecuteScalarAsync();
        return resultado == null ? null : (DateTime?)resultado;
    }

    // Reporte por intervalo de fechas: INNER JOIN entre las 4 tablas.
    public async Task<List<PrestamoReporte>> ReportePorFechasAsync(DateTime desde, DateTime hasta)
    {
        const string sql = @"
            SELECT p.PrestamoId, s.Nombre AS Socio, l.Titulo AS Libro,
                   p.FechaPrestamo, p.FechaLimite, p.Estado
            FROM Prestamos p
            INNER JOIN DetallePrestamo dp ON dp.PrestamoId = p.PrestamoId
            INNER JOIN Libros l ON l.LibroId = dp.LibroId
            INNER JOIN Socios s ON s.SocioId = p.SocioId
            WHERE p.FechaPrestamo >= @Desde AND p.FechaPrestamo < @Hasta
            ORDER BY p.FechaPrestamo DESC, s.Nombre";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Desde", desde.Date);
        // Se suma un día para incluir todo el último día del intervalo.
        cmd.Parameters.AddWithValue("@Hasta", hasta.Date.AddDays(1));
        await cn.OpenAsync();

        var lista = new List<PrestamoReporte>();
        using var dr = await cmd.ExecuteReaderAsync();
        while (await dr.ReadAsync())
        {
            lista.Add(new PrestamoReporte
            {
                PrestamoId = dr.GetInt32(0),
                Socio = dr.GetString(1),
                Libro = dr.GetString(2),
                FechaPrestamo = dr.GetDateTime(3),
                FechaLimite = dr.GetDateTime(4),
                Estado = dr.GetString(5)
            });
        }
        return lista;
    }
}
