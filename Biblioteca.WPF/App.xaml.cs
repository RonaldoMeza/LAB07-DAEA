using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Biblioteca.WPF;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Arranca con el mismo tema (claro/oscuro) que tiene Windows, pero con nuestro acento.
        ApplicationThemeManager.ApplySystemTheme(false);
        SetTheme(ApplicationThemeManager.GetAppTheme());
    }

    // Cambia el tema aplicando nuestro color de acento.
    public static void SetTheme(ApplicationTheme theme)
    {
        ApplyAccent(theme);
        ApplicationThemeManager.Apply(theme, WindowBackdropType.Mica, false);
        ApplyAccent(theme); // el color del texto sobre el acento depende del tema ya aplicado
    }

    // Color de acento propio (verde): los botones principales se ven igual en
    // cualquier PC, sin depender del color configurado en Windows.
    private static void ApplyAccent(ApplicationTheme theme)
    {
        if (theme == ApplicationTheme.Dark)
            ApplicationAccentColorManager.Apply(Rgb(0x16, 0xA3, 0x4A),
                Rgb(0x4A, 0xDE, 0x80), Rgb(0x86, 0xEF, 0xAC), Rgb(0xBB, 0xF7, 0xD0));
        else
            ApplicationAccentColorManager.Apply(Rgb(0x16, 0xA3, 0x4A),
                Rgb(0x15, 0x80, 0x3D), Rgb(0x16, 0x65, 0x34), Rgb(0x14, 0x53, 0x2D));
    }

    private static Color Rgb(byte r, byte g, byte b) => Color.FromRgb(r, g, b);
}
