using Biblioteca.Datos;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio;

public class SocioNegocio
{
    private const int MaxDniLength = 15;

    private readonly SocioRepositorio _socios = new();

    public Task<List<Socio>> ListarAsync() => _socios.ListarAsync();

    public Task<List<Socio>> BuscarPorNombreAsync(string texto) =>
        string.IsNullOrWhiteSpace(texto)
            ? ListarAsync()
            : _socios.BuscarPorNombreAsync(texto.Trim());

    public Task<List<Socio>> BuscarPorDniAsync(string texto) =>
        string.IsNullOrWhiteSpace(texto)
            ? ListarAsync()
            : _socios.BuscarPorDniAsync(texto.Trim());

    public Task<Socio> ObtenerPorIdAsync(int socioId) => _socios.ObtenerPorIdAsync(socioId);

    public async Task RegistrarAsync(Socio socio)
    {
        await ValidarAsync(socio, excluirSocioId: 0);
        await _socios.InsertarAsync(socio);
    }

    public async Task ActualizarAsync(Socio socio)
    {
        if (socio.SocioId <= 0)
            throw new ReglaNegocioException("Debe seleccionar un socio para actualizar.");

        await ValidarAsync(socio, socio.SocioId);
        await _socios.ActualizarAsync(socio);
    }

    public async Task EliminarAsync(int socioId)
    {
        if (socioId <= 0)
            throw new ReglaNegocioException("Debe seleccionar un socio.");

        // No se puede dar de baja a un socio con préstamos pendientes.
        int pendientes = await _socios.ContarPrestamosPendientesAsync(socioId);
        if (pendientes > 0)
            throw new ReglaNegocioException(
                $"No se puede dar de baja al socio: tiene {pendientes} libro(s) pendiente(s) de devolución.");

        await _socios.EliminarLogicoAsync(socioId);
    }

    private async Task ValidarAsync(Socio socio, int excluirSocioId)
    {
        socio.DNI = socio.DNI?.Trim();
        socio.Nombre = socio.Nombre?.Trim();
        socio.Email = string.IsNullOrWhiteSpace(socio.Email) ? null : socio.Email.Trim();

        if (string.IsNullOrWhiteSpace(socio.DNI))
            throw new ReglaNegocioException("El DNI del socio es obligatorio.");

        if (socio.DNI.Length > MaxDniLength)
            throw new ReglaNegocioException($"El DNI no puede superar los {MaxDniLength} caracteres.");

        if (string.IsNullOrWhiteSpace(socio.Nombre))
            throw new ReglaNegocioException("El nombre del socio es obligatorio.");

        if (socio.Email != null)
        {
            try { _ = new System.Net.Mail.MailAddress(socio.Email); }
            catch { throw new ReglaNegocioException("El correo electrónico no tiene un formato válido."); }
        }

        // No se permite repetir un DNI.
        if (await _socios.ExisteDniAsync(socio.DNI, excluirSocioId))
            throw new ReglaNegocioException($"Ya existe un socio registrado con el DNI {socio.DNI}.");
    }
}
