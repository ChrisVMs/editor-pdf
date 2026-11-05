using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Escritor;

public partial class MainWindow : Window
{
    private Libro _libro = new();
    private string? _ruta;
    private readonly string _previewPath = Path.Combine(Path.GetTempPath(), "escritor-preview.pdf");
    private readonly DispatcherTimer _autoguardado = new() { Interval = TimeSpan.FromSeconds(30) };

    public MainWindow()
    {
        InitializeComponent();
        Usar(new Libro());
        _autoguardado.Tick += (_, _) => { if (_ruta != null) _libro.Guardar(_ruta); };
        _autoguardado.Start();
        Closing += (_, _) => { if (_ruta != null) _libro.Guardar(_ruta); };
        SourceInitialized += (_, _) => CambiarTema(Tema.Cargar());
        Loaded += (_, _) => Preview_Click(this, new RoutedEventArgs());
    }

    private void Tema_Click(object s, RoutedEventArgs e) => CambiarTema(!Tema.EsOscuro);

    private void CambiarTema(bool oscuro)
    {
        Tema.Aplicar(this, oscuro);
        BotonTema.Content = oscuro ? "☀ Claro" : "🌙 Oscuro";
        if (Visor.CoreWebView2 != null) Preview_Click(this, new RoutedEventArgs());
    }

    private void Usar(Libro libro)
    {
        _libro = libro;
        DataContext = libro;
        if (libro.Capitulos.Count > 0) Lista.SelectedIndex = 0;
        ActualizarTitulo();
        // La vista previa siempre muestra el libro actual (no el anterior).
        if (IsLoaded) Preview_Click(this, new RoutedEventArgs());
    }

    private void ActualizarTitulo() =>
        Title = "Escritor — " + (_ruta != null ? Path.GetFileName(_ruta) : "sin guardar");

    // --- Archivo ---
    private const string Filtro = "Libro (*.libro.json)|*.libro.json";

    private void Nuevo_Click(object s, RoutedEventArgs e) { _ruta = null; Usar(new Libro()); }

    private void Abrir_Click(object s, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = Filtro };
        if (dlg.ShowDialog() != true) return;
        _ruta = dlg.FileName;
        Usar(Libro.Cargar(_ruta));
    }

    private void Guardar_Click(object s, RoutedEventArgs e)
    {
        if (_ruta == null)
        {
            var dlg = new SaveFileDialog { Filter = Filtro, FileName = _libro.Titulo };
            if (dlg.ShowDialog() != true) return;
            _ruta = dlg.FileName;
        }
        _libro.Guardar(_ruta);
        ActualizarTitulo();
    }

    // --- Capítulos ---
    private void Agregar_Click(object s, RoutedEventArgs e)
    {
        var cap = new Capitulo { Nombre = $"Capítulo {_libro.Capitulos.Count + 1}" };
        _libro.Capitulos.Add(cap);
        Lista.SelectedItem = cap;
    }

    private void Quitar_Click(object s, RoutedEventArgs e)
    {
        if (Lista.SelectedItem is not Capitulo cap) return;
        if (MessageBox.Show($"¿Quitar \"{cap.Nombre}\"?", "Confirmar", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            _libro.Capitulos.Remove(cap);
    }

    private void Subir_Click(object s, RoutedEventArgs e) => Mover(-1);
    private void Bajar_Click(object s, RoutedEventArgs e) => Mover(1);

    private void Mover(int delta)
    {
        int i = Lista.SelectedIndex, j = i + delta;
        if (i < 0 || j < 0 || j >= _libro.Capitulos.Count) return;
        _libro.Capitulos.Move(i, j);
        Lista.SelectedIndex = j;
    }

    private void Editor_TextChanged(object s, TextChangedEventArgs e)
    {
        int palabras = Editor.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        Contador.Text = $"{palabras} palabras";
    }

    // --- PDF ---
    private async void Preview_Click(object s, RoutedEventArgs e)
    {
        try
        {
            bool oscuro = Tema.EsOscuro;
            var pdf = await Task.Run(() => GeneradorPdf.Generar(_libro, oscuro));
            File.WriteAllBytes(_previewPath, pdf);
            await Visor.EnsureCoreWebView2Async();
            Visor.CoreWebView2.Navigate(new Uri(_previewPath).AbsoluteUri + "?t=" + DateTime.Now.Ticks);
        }
        catch (Exception ex) { MostrarError(ex); }
    }

    private async void Exportar_Click(object s, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog { Filter = "PDF (*.pdf)|*.pdf", FileName = _libro.Titulo };
        if (dlg.ShowDialog() != true) return;
        try { File.WriteAllBytes(dlg.FileName, await Task.Run(() => GeneradorPdf.Generar(_libro))); }
        catch (Exception ex) { MostrarError(ex); }
    }

    private static void MostrarError(Exception ex) =>
        MessageBox.Show(ex.Message, "No se pudo generar el PDF", MessageBoxButton.OK, MessageBoxImage.Error);
}
