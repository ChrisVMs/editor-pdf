using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Escritor;

/// <summary>Modo claro/oscuro: cambia los colores de App.xaml y recuerda la elección.</summary>
public static class Tema
{
    private static readonly Dictionary<string, string> Claro = new()
    {
        ["Fondo"] = "#F5F5F5", ["Panel"] = "#FFFFFF", ["Texto"] = "#1E1E1E", ["TextoSuave"] = "#707070",
        ["Borde"] = "#C8C8C8", ["Hover"] = "#E3E3E3", ["Seleccion"] = "#CCE4F7",
    };

    private static readonly Dictionary<string, string> Oscuro = new()
    {
        ["Fondo"] = "#1B1B1F", ["Panel"] = "#26262B", ["Texto"] = "#E6E6E6", ["TextoSuave"] = "#9A9A9A",
        ["Borde"] = "#3C3C44", ["Hover"] = "#34343B", ["Seleccion"] = "#2F4A66",
    };

    private static readonly string Archivo = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Escritor", "tema.txt");

    public static bool EsOscuro { get; private set; }

    public static bool Cargar()
    {
        try { return File.Exists(Archivo) && File.ReadAllText(Archivo).Trim() == "oscuro"; }
        catch { return false; }
    }

    public static void Aplicar(Window ventana, bool oscuro)
    {
        EsOscuro = oscuro;
        var res = Application.Current.Resources;
        foreach (var (clave, color) in oscuro ? Oscuro : Claro)
            res[clave] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));

        BarraDeTitulo(ventana, oscuro);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Archivo)!);
            File.WriteAllText(Archivo, oscuro ? "oscuro" : "claro");
        }
        catch { /* no es grave si no se puede recordar */ }
    }

    // Barra de título oscura de Windows 10/11.
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int valor, int tam);

    private static void BarraDeTitulo(Window ventana, bool oscuro)
    {
        var hwnd = new WindowInteropHelper(ventana).Handle;
        if (hwnd == IntPtr.Zero) return;
        int v = oscuro ? 1 : 0;
        DwmSetWindowAttribute(hwnd, 20, ref v, sizeof(int));
    }
}
