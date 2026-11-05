using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Escritor;

public abstract class Notificable : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void Set<T>(ref T campo, T valor, [CallerMemberName] string? nombre = null)
    {
        if (Equals(campo, valor)) return;
        campo = valor;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
    }
}

public class Capitulo : Notificable
{
    private string _nombre = "Nuevo capítulo";
    private string _texto = "";

    public string Nombre { get => _nombre; set => Set(ref _nombre, value); }
    public string Texto { get => _texto; set => Set(ref _texto, value); }
}

public class Libro : Notificable
{
    private string _titulo = "Sin título";

    public string Titulo { get => _titulo; set => Set(ref _titulo, value); }
    public ObservableCollection<Capitulo> Capitulos { get; set; } = new();

    private static readonly JsonSerializerOptions Opciones = new() { WriteIndented = true };

    public void Guardar(string ruta) => File.WriteAllText(ruta, JsonSerializer.Serialize(this, Opciones));

    public static Libro Cargar(string ruta) =>
        JsonSerializer.Deserialize<Libro>(File.ReadAllText(ruta)) ?? new Libro();
}
