using System.Windows;
using System.Windows.Controls;
using Biblioteca.Entidades;
using Biblioteca.Negocio;
using Biblioteca.WPF.Services;

namespace Biblioteca.WPF.Views;

// Mantenimiento de libros: insertar, actualizar, eliminar lógicamente y buscar.
// La WPF solo usa la capa de Negocio y las Entidades: nunca Datos ni SqlClient.
public partial class LibrosPage : Page
{
    private readonly LibroNegocio _libros = new();
    private bool _buscarPorTitulo = true;

    public LibrosPage()
    {
        InitializeComponent();
        Loaded += LibrosPage_Loaded;
    }

    private async void LibrosPage_Loaded(object sender, RoutedEventArgs e)
    {
        await CargarAutoresAsync();
        await CargarDatosAsync();
    }

    // Todas las cargas son asíncronas: la ventana nunca se bloquea.
    private async Task CargarDatosAsync()
    {
        List<Libro> lista = new();
        var ok = await UiServices.RunAsync(async () =>
        {
            var texto = txtSearch.Text;
            lista = _buscarPorTitulo
                ? await _libros.BuscarPorTituloAsync(texto)
                : await _libros.BuscarPorAutorAsync(texto);
        }, SetBusy);

        dgLibros.ItemsSource = lista;
        emptyState.Visibility = ok && lista.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async Task CargarAutoresAsync()
    {
        var autores = new List<Autor>();
        await UiServices.RunAsync(async () =>
            autores = await _libros.ListarAutoresAsync(), _ => { });

        cboAutor.ItemsSource = autores;
    }

    private async void txtSearch_TextChanged(object sender, TextChangedEventArgs e) =>
        await CargarDatosAsync();

    private async void btnRefresh_Click(object sender, RoutedEventArgs e)
    {
        txtSearch.Text = string.Empty;
        await CargarDatosAsync();
    }

    private void criterioTitulo_Click(object sender, RoutedEventArgs e)
    {
        _buscarPorTitulo = true;
        btnCriterio.Content = "Por título";
        txtSearch.PlaceholderText = "Buscar por título…";
    }

    private void criterioAutor_Click(object sender, RoutedEventArgs e)
    {
        _buscarPorTitulo = false;
        btnCriterio.Content = "Por autor";
        txtSearch.PlaceholderText = "Buscar por autor…";
    }

    private void dgLibros_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dgLibros.SelectedItem is not Libro libro) return;

        txtTitulo.Text = libro.Titulo;
        txtIsbn.Text = libro.ISBN;
        numEjemplares.Value = libro.Ejemplares;
        cboAutor.SelectedValue = libro.AutorId;
        btnGuardar.Content = "Actualizar";
    }

    private void dgLibros_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) =>
        txtTitulo.Focus();

    private void btnNuevo_Click(object sender, RoutedEventArgs e)
    {
        dgLibros.SelectedItem = null;
        txtTitulo.Text = string.Empty;
        txtIsbn.Text = string.Empty;
        numEjemplares.Value = 0;
        cboAutor.SelectedItem = null;
        btnGuardar.Content = "Registrar";
        txtTitulo.Focus();
    }

    private async void btnGuardar_Click(object sender, RoutedEventArgs e)
    {
        var libro = new Libro
        {
            LibroId = (dgLibros.SelectedItem as Libro)?.LibroId ?? 0,
            Titulo = txtTitulo.Text,
            ISBN = txtIsbn.Text,
            AutorId = (cboAutor.SelectedItem as Autor)?.AutorId ?? 0,
            Ejemplares = (int)(numEjemplares.Value ?? 0)
        };

        var ok = await UiServices.RunAsync(async () =>
        {
            if (libro.LibroId == 0)
                await _libros.RegistrarAsync(libro);
            else
                await _libros.ActualizarAsync(libro);
        }, SetBusy);

        if (ok)
        {
            UiServices.ShowSuccess(libro.LibroId == 0 ? "Libro registrado." : "Libro actualizado.");
            btnNuevo_Click(sender, e);
            await CargarDatosAsync();
        }
    }

    private async void btnEliminar_Click(object sender, RoutedEventArgs e)
    {
        if (dgLibros.SelectedItem is not Libro libro)
        {
            UiServices.ShowWarning("Selecciona un libro de la lista.");
            return;
        }

        if (!await UiServices.ConfirmAsync("Dar de baja",
                $"¿Dar de baja el libro \"{libro.Titulo}\"? No se eliminará físicamente.", "Dar de baja"))
            return;

        var ok = await UiServices.RunAsync(async () =>
            await _libros.EliminarAsync(libro.LibroId), SetBusy);

        if (ok)
        {
            UiServices.ShowSuccess("Libro dado de baja.");
            btnNuevo_Click(sender, e);
            await CargarDatosAsync();
        }
    }

    private void SetBusy(bool busy) => loading.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
}
