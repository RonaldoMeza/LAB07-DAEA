using Biblioteca.Negocio;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Biblioteca.WPF.Services;

// Utilidades de interfaz compartidas por las páginas: notificaciones, confirmaciones y navegación.
// Solo pertenece a la capa de Presentación; no contiene reglas de negocio.
public static class UiServices
{
    private static readonly SnackbarService Snackbar = new();
    private static readonly ContentDialogService Dialogs = new();

    public static NavigationView Navigation { get; private set; }

    // La ventana principal registra aquí sus contenedores al iniciar.
    public static void Initialize(SnackbarPresenter snackbarPresenter, ContentDialogHost dialogHost, NavigationView navigation)
    {
        Snackbar.SetSnackbarPresenter(snackbarPresenter);
        Dialogs.SetDialogHost(dialogHost);
        Navigation = navigation;
    }

    public static void ShowSuccess(string message) =>
        Snackbar.Show("Listo", message, ControlAppearance.Success,
            new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(3));

    // Advertencia: se usa para las reglas de negocio (el usuario puede corregir el dato).
    public static void ShowWarning(string message) =>
        Snackbar.Show("Revisa los datos", message, ControlAppearance.Caution,
            new SymbolIcon(SymbolRegular.Warning24), TimeSpan.FromSeconds(4));

    // Error inesperado (por ejemplo, no hay conexión con la base de datos).
    public static void ShowError(string message) =>
        Snackbar.Show("Algo salió mal", message, ControlAppearance.Danger,
            new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(6));

    // Diálogo de confirmación dentro de la ventana (en lugar de un MessageBox clásico).
    public static async Task<bool> ConfirmAsync(string title, string message, string confirmText)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = confirmText,
            PrimaryButtonAppearance = ControlAppearance.Danger,
            CloseButtonText = "Cancelar"
        };

        var result = await Dialogs.ShowAsync(dialog, CancellationToken.None);
        return result == ContentDialogResult.Primary;
    }

    // Ejecuta una operación mostrando el indicador de carga y traduciendo las excepciones.
    // La presentación solo conoce ReglaNegocioException: nunca captura SqlException.
    // Devuelve true si la operación terminó sin errores.
    public static async Task<bool> RunAsync(Func<Task> action, Action<bool> setBusy)
    {
        try
        {
            setBusy(true);
            await action();
            return true;
        }
        catch (ReglaNegocioException ex)
        {
            ShowWarning(ex.Message);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            setBusy(false);
        }
        return false;
    }
}
