using System.Windows;
using System.Windows.Controls;
using Biblioteca.Entidades;
using Biblioteca.Negocio;
using Biblioteca.WPF.Services;

namespace Biblioteca.WPF.Views;

// Reporte de préstamos por intervalo de fechas.
public partial class ReportePage : Page
{
    private readonly PrestamoNegocio _prestamos = new();

    public ReportePage()
    {
        InitializeComponent();
        Loaded += ReportePage_Loaded;
    }

    private async void ReportePage_Loaded(object sender, RoutedEventArgs e)
    {
        // Por defecto: el último mes.
        dpDesde.SelectedDate = DateTime.Today.AddMonths(-1);
        dpHasta.SelectedDate = DateTime.Today;
        await CargarDatosAsync();
    }

    private async void btnBuscar_Click(object sender, RoutedEventArgs e) => await CargarDatosAsync();

    private async Task CargarDatosAsync()
    {
        if (dpDesde.SelectedDate == null || dpHasta.SelectedDate == null)
        {
            UiServices.ShowWarning("Selecciona las fechas inicial y final del intervalo.");
            return;
        }

        List<PrestamoReporte> lista = new();
        var ok = await UiServices.RunAsync(async () =>
            lista = await _prestamos.ReportePorFechasAsync(dpDesde.SelectedDate.Value, dpHasta.SelectedDate.Value),
            busy => loading.Visibility = busy ? Visibility.Visible : Visibility.Collapsed);

        dgReporte.ItemsSource = lista;
        emptyState.Visibility = ok && lista.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}
