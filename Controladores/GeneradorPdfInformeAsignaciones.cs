using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Controladores;

/// <summary>PDF del informe visible en pantalla: portada, filtros, indicadores, gráfico y tabla (HU34).</summary>
public static class GeneradorPdfInformeAsignaciones
{
    private static readonly CultureInfo Espanol = CultureInfo.GetCultureInfo("es-AR");

    static GeneradorPdfInformeAsignaciones()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Generar(InformeAsignacionesAulaDto informe)
    {
        return Document.Create(documento =>
        {
            documento.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(32);
                pagina.DefaultTextStyle(estilo => estilo.FontSize(10).FontColor(Colors.Grey.Darken4));

                pagina.Header().PaddingBottom(8).Row(fila =>
                {
                    fila.RelativeItem().Text("EIMA · Informe de asignaciones de aula").FontSize(11).FontColor(Colors.Grey.Darken1);
                    fila.AutoItem().Text(ZonaHorariaArgentina.FormatearFechaHora(informe.FechaActualizacion)).FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                pagina.Content().Column(columna =>
                {
                    columna.Spacing(14);
                    columna.Item().Element(c => Portada(c, informe));
                    columna.Item().Element(c => Indicadores(c, informe));
                    columna.Item().Element(c => Grafico(c, informe));
                    columna.Item().Element(c => Tabla(c, informe));
                });

                pagina.Footer().AlignRight().Text(texto =>
                {
                    texto.DefaultTextStyle(estilo => estilo.FontSize(8).FontColor(Colors.Grey.Darken1));
                    texto.Span("Página ");
                    texto.CurrentPageNumber();
                    texto.Span(" de ");
                    texto.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static void Portada(IContainer contenedor, InformeAsignacionesAulaDto informe)
    {
        contenedor.Column(columna =>
        {
            columna.Item().Text("Informe de asignaciones de aula").FontSize(22).Bold().FontColor(Colors.Black);
            columna.Item().PaddingTop(4).Text("Seguimiento de grupos, cupos y cierres").FontColor(Colors.Grey.Darken1);
            columna.Item().PaddingTop(12).Text($"Fecha de generación: {ZonaHorariaArgentina.FormatearFechaHora(informe.FechaActualizacion)} (hora de Argentina)");
            columna.Item().Text($"Periodo: {informe.EtiquetaPeriodo}");
            columna.Item().Text($"Materia: {informe.MateriaNombre ?? "Todas"}");
            columna.Item().Text(informe.Comparar
                ? $"Comparativo: activo, contra {informe.EtiquetaPeriodoAnterior}"
                : "Comparativo: apagado");
        });
    }

    private static void Indicadores(IContainer contenedor, InformeAsignacionesAulaDto informe)
    {
        contenedor.Column(columna =>
        {
            columna.Spacing(8);
            columna.Item().Text("Indicadores").FontSize(14).Bold();
            columna.Item().Row(fila =>
            {
                Tarjeta(fila, "Creadas", Numero(informe.Creadas), informe.Variaciones?.Creadas, informe.Comparar);
                Tarjeta(fila, "Cerradas", Numero(informe.Cerradas), informe.Variaciones?.Cerradas, informe.Comparar);
                Tarjeta(fila, "Abiertas al cierre", Numero(informe.AbiertasAlCierre), informe.Variaciones?.AbiertasAlCierre, informe.Comparar);
            });
            columna.Item().Row(fila =>
            {
                Tarjeta(fila, "SLA de cierre", Porcentaje(informe.SlaPorcentaje), informe.Variaciones?.Sla, informe.Comparar);
                Tarjeta(fila, "TME de cierre", informe.TmeDias is null ? "—" : $"{Numero(informe.TmeDias.Value)} días", informe.Variaciones?.Tme, informe.Comparar);
                Tarjeta(fila, "Ocupación media", Porcentaje(informe.OcupacionMedia), null, false);
            });
            columna.Item().Text($"P50 {Porcentaje(informe.OcupacionP50)} · P90 {Porcentaje(informe.OcupacionP90)}")
                .FontSize(9)
                .FontColor(Colors.Grey.Darken1);
        });
    }

    private static void Tarjeta(
        RowDescriptor fila,
        string titulo,
        string valor,
        VariacionIndicadorDto? variacion,
        bool mostrarVariacion)
    {
        fila.RelativeItem().Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(columna =>
        {
            columna.Item().Text(titulo).FontSize(8).FontColor(Colors.Grey.Darken1);
            columna.Item().PaddingTop(2).Text(valor).FontSize(14).Bold();
            if (mostrarVariacion)
            {
                columna.Item().PaddingTop(2).Text(TextoVariacion(variacion)).FontSize(8).FontColor(Colors.Grey.Darken2);
            }
        });
    }

    private static void Grafico(IContainer contenedor, InformeAsignacionesAulaDto informe)
    {
        contenedor.Column(columna =>
        {
            columna.Spacing(6);
            columna.Item().Text("Creadas por materia").FontSize(14).Bold();
            columna.Item().Row(leyenda =>
            {
                leyenda.AutoItem().Width(10).Height(10).Background(Colors.Blue.Medium);
                leyenda.AutoItem().PaddingLeft(4).Text(informe.EtiquetaPeriodo).FontSize(8);
                if (informe.Comparar)
                {
                    leyenda.AutoItem().PaddingLeft(12).Width(10).Height(10).Background(Colors.Grey.Medium);
                    leyenda.AutoItem().PaddingLeft(4).Text(informe.EtiquetaPeriodoAnterior ?? "Periodo anterior").FontSize(8);
                }
            });

            if (informe.SeriesPorMateria.Count == 0)
            {
                columna.Item().Text("Sin asignaciones creadas en el periodo.").FontColor(Colors.Grey.Darken1);
                return;
            }

            var maximo = Math.Max(1, informe.SeriesPorMateria.Max(s => Math.Max(s.Creadas, s.CreadasAnterior ?? 0)));

            foreach (var serie in informe.SeriesPorMateria)
            {
                columna.Item().PaddingTop(4).Text(serie.MateriaNombre).FontSize(9).Bold();
                columna.Item().Element(c => Barra(c, serie.Creadas, maximo, Colors.Blue.Medium, informe.EtiquetaPeriodo));
                if (informe.Comparar)
                {
                    columna.Item().PaddingTop(2).Element(c =>
                        Barra(c, serie.CreadasAnterior ?? 0, maximo, Colors.Grey.Medium, informe.EtiquetaPeriodoAnterior));
                }
            }
        });
    }

    private static void Barra(IContainer contenedor, int valor, int maximo, string color, string? etiqueta)
    {
        var proporcion = maximo <= 0 ? 0f : (float)valor / maximo;
        var ancho = Math.Clamp(proporcion, 0.001f, 1f);
        var resto = Math.Max(1f - ancho, 0.001f);

        contenedor.Row(fila =>
        {
            fila.RelativeItem(5).Height(10).Row(pista =>
            {
                pista.RelativeItem(ancho).Background(color);
                pista.RelativeItem(resto).Background(Colors.Grey.Lighten3);
            });
            fila.AutoItem().PaddingLeft(6).AlignMiddle().Text($"{valor}").FontSize(8);
            if (!string.IsNullOrWhiteSpace(etiqueta))
                fila.AutoItem().PaddingLeft(4).AlignMiddle().Text(etiqueta).FontSize(7).FontColor(Colors.Grey.Darken1);
        });
    }

    private static void Tabla(IContainer contenedor, InformeAsignacionesAulaDto informe)
    {
        contenedor.Column(columna =>
        {
            columna.Spacing(6);
            columna.Item().Text("Por materia y docente").FontSize(14).Bold();
            columna.Item().Table(tabla =>
            {
                tabla.ColumnsDefinition(columnas =>
                {
                    columnas.RelativeColumn(3.2f);
                    columnas.RelativeColumn(1);
                    columnas.RelativeColumn(1);
                    columnas.RelativeColumn(1.4f);
                    columnas.RelativeColumn(1.2f);
                });

                tabla.Header(encabezado =>
                {
                    Encabezado(encabezado.Cell(), "Materia / docente");
                    Encabezado(encabezado.Cell(), "Creadas");
                    Encabezado(encabezado.Cell(), "Cerradas");
                    Encabezado(encabezado.Cell(), "Ocupación");
                    Encabezado(encabezado.Cell(), "SLA");
                });

                foreach (var materia in informe.Tabla)
                {
                    Fila(tabla, materia.Nombre, materia, negrita: true);
                    foreach (var docente in materia.Docentes)
                        Fila(tabla, $"    {docente.Nombre}", docente, negrita: false);
                }

                Fila(tabla, informe.Total.Nombre, informe.Total, negrita: true);
            });
        });
    }

    private static void Encabezado(IContainer celda, string texto)
    {
        celda.Background(Colors.Grey.Lighten3).Padding(4).Text(texto).FontSize(8).Bold();
    }

    private static void Fila(TableDescriptor tabla, string nombre, FilaInformeDto fila, bool negrita)
    {
        Celda(tabla.Cell(), nombre, negrita);
        Celda(tabla.Cell(), Numero(fila.Creadas), negrita);
        Celda(tabla.Cell(), Numero(fila.Cerradas), negrita);
        Celda(tabla.Cell(), Porcentaje(fila.OcupacionMedia), negrita);
        Celda(tabla.Cell(), Porcentaje(fila.SlaPorcentaje), negrita);
    }

    private static void Celda(IContainer celda, string texto, bool negrita)
    {
        var contenido = celda.BorderBottom(0.4f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(texto).FontSize(8);
        if (negrita)
            contenido.Bold();
    }

    private static string TextoVariacion(VariacionIndicadorDto? variacion)
    {
        if (variacion?.Absoluta is null)
            return "Sin base para comparar";

        var absoluta = variacion.Absoluta.Value;
        var signo = absoluta > 0 ? "+" : string.Empty;
        var texto = $"{signo}{Numero(absoluta)}";
        if (variacion.Porcentaje is double porcentaje)
            texto += $" ({signo}{Numero(porcentaje)} %)";

        return texto;
    }

    private static string Numero(double valor) => valor.ToString("0.##", Espanol);

    private static string Numero(int valor) => valor.ToString(Espanol);

    private static string Porcentaje(double? valor) =>
        valor is null ? "—" : $"{Numero(valor.Value)} %";
}
