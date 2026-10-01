using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Biblioteca.Entidades;
using Biblioteca.Negocio;
using Biblioteca.WPF.Services;

namespace Biblioteca.WPF.Views;

// Formulario de préstamo (varios libros a un socio) y de devolución (con multa calculada).
public partial class PrestamosPage : Page
{
    private readonly PrestamoNegocio _prestamos = new();
    private readonly List<Libro> _seleccionados = new();

    public PrestamosPage()
    {
        InitializeComponent();
        Loaded += PrestamosPage_Loaded;
    }

    private async void PrestamosPage_Loaded(object sender, RoutedEventArgs e)
    {
        await CargarCombosAsync();
        ActualizarSeleccionados();
    }

    private async Task CargarCombosAsync()
    {
        List<Socio> socios = new();
        List<Libro> libros = new();

        await UiServices.RunAsync(async () =>
        {
            socios = await _prestamos.ListarSociosAsync();
            libros = await _prestamos.ListarLibrosAsync();
        }, _ => { });

        cboSocio.ItemsSource = socios;
        cboSocioDevolucion.ItemsSource = socios;
        // Solo libros activos con ejemplares disponibles para prestar.
        cboLibro.ItemsSource = libros.Where(l => l.Ejemplares > 0).ToList();
    }

    // ===== Préstamo =====

    private void btnAgregarLibro_Click(object sender, RoutedEventArgs e)
    {
        if (cboLibro.SelectedItem is not Libro libro)
        {
            UiServices.ShowWarning("Selecciona un libro para agregar.");
            return;
        }

        if (_seleccionados.Any(l => l.LibroId == libro.LibroId))
        {
            UiServices.ShowWarning($"El libro \"{libro.Titulo}\" ya está en la lista.");
            return;
        }

        if (_seleccionados.Count >= PrestamoNegocio.MaxLibrosPendientes)
        {
            UiServices.ShowWarning($"Un préstamo no puede tener más de {PrestamoNegocio.MaxLibrosPendientes} libros.");
            return;
        }

        _seleccionados.Add(libro);
        ActualizarSeleccionados();
    }

    private void btnQuitarLibro_Click(object sender, RoutedEventArgs e)
    {
        if (dgSeleccionados.SelectedItem is Libro libro)
        {
            _seleccionados.Remove(libro);
            ActualizarSeleccionados();
        }
    }

    private void ActualizarSeleccionados()
    {
        dgSeleccionados.ItemsSource = null;
        dgSeleccionados.ItemsSource = _seleccionados;
        emptySeleccionados.Visibility = _seleccionados.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void btnRegistrarPrestamo_Click(object sender, RoutedEventArgs e)
    {
        if (cboSocio.SelectedItem is not Socio socio)
        {
            UiServices.ShowWarning("Selecciona el socio del préstamo.");
            return;
        }

        if (_seleccionados.Count == 0)
        {
            UiServices.ShowWarning("Agrega al menos un libro al préstamo.");
            return;
        }

        var ok = await UiServices.RunAsync(async () =>
        {
            var ids = _seleccionados.Select(l => l.LibroId).ToList();
            await _prestamos.RegistrarPrestamoAsync(socio.SocioId, ids);
        }, busy => loadingPrestamo.Visibility = busy ? Visibility.Visible : Visibility.Collapsed);

        if (ok)
        {
            UiServices.ShowSuccess($"Préstamo registrado a {socio.Nombre} con {_seleccionados.Count} libro(s). " +
                                   $"Fecha límite: {DateTime.Now.AddDays(PrestamoNegocio.DiasPrestamo):dd/MM/yyyy}.");
            _seleccionados.Clear();
            ActualizarSeleccionados();
            await CargarCombosAsync(); // el stock cambió
        }
    }

    // ===== Devolución =====

    private async void cboSocioDevolucion_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        txtMulta.Text = "S/ 0.00";
        await CargarPendientesAsync();
    }

    private async Task CargarPendientesAsync()
    {
        if (cboSocioDevolucion.SelectedItem is not Socio socio)
        {
            dgPendientes.ItemsSource = null;
            emptyPendientes.Visibility = Visibility.Visible;
            return;
        }

        List<DetallePendiente> lista = new();
        await UiServices.RunAsync(async () =>
            lista = await _prestamos.ListarPendientesPorSocioAsync(socio.SocioId),
            busy => loadingDevolucion.Visibility = busy ? Visibility.Visible : Visibility.Collapsed);

        dgPendientes.ItemsSource = lista;
        emptyPendientes.Visibility = lista.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    // Al elegir uno o varios libros pendientes se muestra la multa total calculada por la capa de Negocio.
    private void dgPendientes_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dgPendientes.SelectedItems != null && dgPendientes.SelectedItems.Count > 0)
        {
            var seleccion = dgPendientes.SelectedItems.Cast<DetallePendiente>().ToList();
            var total = seleccion.Sum(d => d.Multa);
            txtMulta.Text = $"S/ {total:0.00}";
        }
        else
        {
            txtMulta.Text = "S/ 0.00";
        }
    }

    private async void btnDevolver_Click(object sender, RoutedEventArgs e)
    {
        if (dgPendientes.SelectedItems == null || dgPendientes.SelectedItems.Count == 0)
        {
            UiServices.ShowWarning("Selecciona al menos un libro que se está devolviendo.");
            return;
        }

        var detalles = dgPendientes.SelectedItems.Cast<DetallePendiente>().ToList();
        decimal multaTotal = 0m;
        int devoluciones = 0;

        var ok = await UiServices.RunAsync(async () =>
        {
            foreach (var d in detalles)
            {
                var resultado = await _prestamos.RegistrarDevolucionAsync(d.PrestamoId, d.LibroId);
                if (resultado != null)
                {
                    multaTotal += resultado.Multa;
                    devoluciones++;
                }
            }
        }, busy => loadingDevolucion.Visibility = busy ? Visibility.Visible : Visibility.Collapsed);

        if (ok)
        {
            UiServices.ShowSuccess($"Se registraron {devoluciones} devolución(es). Multa total: S/ {multaTotal:0.00}.");
            txtMulta.Text = "S/ 0.00";
            await CargarPendientesAsync();
            await CargarCombosAsync(); // el stock cambió
        }
    }
}
