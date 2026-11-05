using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Escritor;

public static class GeneradorPdf
{
    static GeneradorPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        // Desde QuestPDF 2026.9 las fuentes de Windows (Georgia) no se usan si no se activa esto.
        QuestPDF.Settings.UseSystemFonts = true;
    }

    /// <param name="oscuro">Hojas oscuras (solo para la vista previa; el PDF exportado va en blanco).</param>
    public static byte[] Generar(Libro libro, bool oscuro = false)
    {
        // Páginas sin numerar (portada + índice): el primer capítulo es la página 1.
        // Se cuentan con una pasada previa porque un índice largo puede ocupar varias páginas.
        int sinNumero = Armar(libro, 0, soloInicio: true, oscuro)
            .GenerateImages(new ImageGenerationSettings { RasterDpi = 20 }).Count();
        return Armar(libro, sinNumero, soloInicio: false, oscuro).GeneratePdf();
    }

    private static Document Armar(Libro libro, int desfase, bool soloInicio, bool oscuro) =>
        Document.Create(doc =>
        {
            string Num(int? n) => n is null ? "-" : (n.Value - desfase).ToString();

            // Portada
            doc.Page(p =>
            {
                Base(p, oscuro);
                p.Content().AlignMiddle().AlignCenter().Text(libro.Titulo).FontSize(32).Bold();
            });

            // Índice (sin número de página)
            doc.Page(p =>
            {
                Base(p, oscuro);
                p.Content().Column(col =>
                {
                    col.Item().PaddingBottom(20).Text("Índice").FontSize(24).Bold();
                    if (libro.Capitulos.Count == 0)
                        col.Item().Text("(sin capítulos)").Italic();
                    for (int i = 0; i < libro.Capitulos.Count; i++)
                    {
                        var id = $"cap{i}";
                        // En la pasada de conteo los capítulos no existen: sin enlace.
                        var item = soloInicio ? col.Item() : col.Item().SectionLink(id);
                        item.PaddingVertical(3).Row(r =>
                        {
                            r.RelativeItem().Text($"{i + 1}. {libro.Capitulos[i].Nombre}");
                            if (!soloInicio)
                                r.AutoItem().Text(t => t.BeginPageNumberOfSection(id).Format(Num));
                        });
                    }
                });
            });

            if (soloInicio || libro.Capitulos.Count == 0) return;

            // Capítulos (numerados desde 1)
            doc.Page(p =>
            {
                Base(p, oscuro);
                p.Footer().AlignCenter().Text(t => t.CurrentPageNumber().Format(Num));
                p.Content().Column(col =>
                {
                    for (int i = 0; i < libro.Capitulos.Count; i++)
                    {
                        var cap = libro.Capitulos[i];
                        if (i > 0) col.Item().PageBreak();
                        col.Item().Section($"cap{i}").PaddingBottom(15)
                            .Text(cap.Nombre).FontSize(20).Bold();
                        foreach (var parrafo in cap.Texto.Replace("\r", "").Split('\n'))
                            col.Item().PaddingBottom(8).Text(parrafo).Justify();
                    }
                });
            });
        });

    private static void Base(PageDescriptor p, bool oscuro)
    {
        p.PageColor(oscuro ? "#1E1E22" : Colors.White);
        p.Size(PageSizes.A5);
        p.Margin(2, Unit.Centimetre);
        p.DefaultTextStyle(x => x.FontSize(11).FontFamily("Georgia").FontColor(oscuro ? "#E6E6E6" : Colors.Black));
    }
}
