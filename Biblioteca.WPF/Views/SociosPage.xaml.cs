using System.Windows;
using System.Windows.Controls;
using Biblioteca.Entidades;
using Biblioteca.Negocio;
using Biblioteca.WPF.Services;

namespace Biblioteca.WPF.Views;

// Mantenimiento de socios: insertar, actualizar, eliminar lógicamente y buscar por nombre o DNI.
public partial class SociosPage : Page
{
    private readonly SocioNegocio _socios = new();
    private bool _buscarPorNombre = true;

    public SociosPage()
    {
        InitializeComponent();
        Loaded += async (s, e) => await CargarDatosAsync();
    }

    private async Task CargarDatosAsync()
    {
        List<Socio> lista = new();
        var ok = await UiServices.RunAsync(async () =>
        {
            var texto = txtSearch.Text;
            lista = _buscarPorNombre
                ? await _socios.BuscarPorNombreAsync(texto)
                : await _socios.BuscarPorDniAsync(texto);
        }, SetBusy);

        dgSocios.ItemsSource = lista;
        emptyState.Visibility = ok && lista.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void txtSearch_TextChanged(object sender, TextChangedEventArgs e) =>
        await CargarDatosAsync();

    private async void btnRefresh_Click(object sender, RoutedEventArgs e)
    {
        txtSearch.Text = string.Empty;
        await CargarDatosAsync();
    }

    private void criterioNombre_Click(object sender, RoutedEventArgs e)
    {
        _buscarPorNombre = true;
        btnCriterio.Content = "Por nombre";
        txtSearch.PlaceholderText = "Buscar por nombre…";
    }

    private void criterioDni_Click(object sender, RoutedEventArgs e)
    {
        _buscarPorNombre = false;
        btnCriterio.Content = "Por DNI";
        txtSearch.PlaceholderText = "Buscar por DNI…";
    }

    private void dgSocios_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dgSocios.SelectedItem is not Socio socio) return;

        txtDni.Text = socio.DNI;
        txtNombre.Text = socio.Nombre;
        txtEmail.Text = socio.Email ?? string.Empty;
        btnGuardar.Content = "Actualizar";
    }

    private void dgSocios_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) =>
        txtNombre.Focus();

    private void btnNuevo_Click(object sender, RoutedEventArgs e)
    {
        dgSocios.SelectedItem = null;
        txtDni.Text = string.Empty;
        txtNombre.Text = string.Empty;
        txtEmail.Text = string.Empty;
        btnGuardar.Content = "Registrar";
        txtDni.Focus();
    }

    private async void btnGuardar_Click(object sender, RoutedEventArgs e)
    {
        var socio = new Socio
        {
            SocioId = (dgSocios.SelectedItem as Socio)?.SocioId ?? 0,
            DNI = txtDni.Text,
            Nombre = txtNombre.Text,
            Email = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text
        };

        var ok = await UiServices.RunAsync(async () =>
        {
            if (socio.SocioId == 0)
                await _socios.RegistrarAsync(socio);
            else
                await _socios.ActualizarAsync(socio);
        }, SetBusy);

        if (ok)
        {
            UiServices.ShowSuccess(socio.SocioId == 0 ? "Socio registrado." : "Socio actualizado.");
            btnNuevo_Click(sender, e);
            await CargarDatosAsync();
        }
    }

    private async void btnEliminar_Click(object sender, RoutedEventArgs e)
    {
        if (dgSocios.SelectedItem is not Socio socio)
        {
            UiServices.ShowWarning("Selecciona un socio de la lista.");
            return;
        }

        if (!await UiServices.ConfirmAsync("Dar de baja",
                $"¿Dar de baja al socio {socio.Nombre}? No se eliminará físicamente.", "Dar de baja"))
            return;

        var ok = await UiServices.RunAsync(async () =>
            await _socios.EliminarAsync(socio.SocioId), SetBusy);

        if (ok)
        {
            UiServices.ShowSuccess("Socio dado de baja.");
            btnNuevo_Click(sender, e);
            await CargarDatosAsync();
        }
    }

    private void SetBusy(bool busy) => loading.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
}
