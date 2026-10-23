using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Controladores;

/// <summary>Metadatos que acompañan al archivo exportado (HU27, CA03).</summary>
internal sealed class MetadatosExportacion
{
    public DateTime FechaExportacionUtc { get; init; }
    public string? Estado { get; init; }
    public string? Materia { get; init; }
    public string? Docente { get; init; }
    public string? Texto { get; init; }
    public DateTime? VigenciaDesdeUtc { get; init; }
    public DateTime? VigenciaHastaUtc { get; init; }
    public OrdenListadoAsignacionesDto Orden { get; init; } = new();
    public int TotalFilas { get; init; }

    public IReadOnlyList<(string Etiqueta, string Valor)> Lineas()
    {
        var desde = VigenciaDesdeUtc.HasValue ? ZonaHorariaArgentina.FormatearFecha(VigenciaDesdeUtc.Value) : "(sin límite)";
        var hasta = VigenciaHastaUtc.HasValue ? ZonaHorariaArgentina.FormatearFecha(VigenciaHastaUtc.Value) : "(sin límite)";

        return new List<(string, string)>
        {
            ("Fecha de exportación", ZonaHorariaArgentina.FormatearFechaHora(FechaExportacionUtc)),
            ("Estado", string.IsNullOrWhiteSpace(Estado) ? "(todos)" : Estado),
            ("Materia", string.IsNullOrWhiteSpace(Materia) ? "(todas)" : Materia),
            ("Docente", string.IsNullOrWhiteSpace(Docente) ? "(todos)" : Docente),
            ("Texto", string.IsNullOrWhiteSpace(Texto) ? "(sin texto)" : Texto),
            ("Vigencia desde", desde),
            ("Vigencia hasta", hasta),
            ("Orden", DescripcionOrden()),
            ("Total de filas", TotalFilas.ToString(CultureInfo.InvariantCulture))
        };
    }

    private string DescripcionOrden()
    {
        var campo = (Orden.Campo ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "vigencia" => "Vigencia",
            "estado" => "Estado",
            "aula" => "Aula",
            "materia" => "Materia",
            "docente" => "Docente",
            "cupo" => "Cupo",
            "id" => "ID",
            _ => "Fecha de asignación"
        };

        var direccion = string.Equals(Orden.Direccion, "asc", StringComparison.OrdinalIgnoreCase)
            ? "ascendente"
            : "descendente";

        return $"{campo} {direccion}";
    }
}

/// <summary>Genera los archivos CSV y XLSX del listado de asignaciones (HU27).</summary>
internal static class ExportadorAsignacionesAula
{
    private static readonly string[] Encabezados =
    {
        "ID", "Asignación", "Estado", "Docente", "Cupo", "Vigencia"
    };

    public static byte[] ConstruirCsv(
        IReadOnlyList<AsignacionAulaListadoDto> filas,
        MetadatosExportacion metadatos)
    {
        var sb = new StringBuilder();

        foreach (var (etiqueta, valor) in metadatos.Lineas())
            sb.Append(EscaparCsv(etiqueta)).Append(';').Append(EscaparCsv(valor)).Append("\r\n");

        sb.Append("\r\n");
        sb.AppendJoin(';', Encabezados.Select(EscaparCsv)).Append("\r\n");

        foreach (var fila in filas)
        {
            sb.AppendJoin(';', FilaCsv(fila).Select(EscaparCsv)).Append("\r\n");
        }

        // BOM para que Excel respete los acentos.
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return encoding.GetPreamble().Concat(encoding.GetBytes(sb.ToString())).ToArray();
    }

    public static byte[] ConstruirXlsx(
        IReadOnlyList<AsignacionAulaListadoDto> filas,
        MetadatosExportacion metadatos)
    {
        var sheetData = new XElement(Ns + "sheetData");

        var numeroFila = 1;
        foreach (var (etiqueta, valor) in metadatos.Lineas())
        {
            sheetData.Add(FilaXml(numeroFila++, etiqueta, valor));
        }

        numeroFila++; // fila en blanco entre metadatos y tabla
        sheetData.Add(FilaXml(numeroFila++, Encabezados));

        foreach (var fila in filas)
            sheetData.Add(FilaXml(numeroFila++, FilaCsv(fila)));

        var worksheet = new XDocument(
            new XElement(Ns + "worksheet",
                new XAttribute(XNamespace.Xmlns + "r", RelNs),
                new XElement(Ns + "cols",
                    new XElement(Ns + "col", new XAttribute("min", 1), new XAttribute("max", 1), new XAttribute("width", 8)),
                    new XElement(Ns + "col", new XAttribute("min", 2), new XAttribute("max", 2), new XAttribute("width", 32)),
                    new XElement(Ns + "col", new XAttribute("min", 3), new XAttribute("max", 3), new XAttribute("width", 12)),
                    new XElement(Ns + "col", new XAttribute("min", 4), new XAttribute("max", 4), new XAttribute("width", 30)),
                    new XElement(Ns + "col", new XAttribute("min", 5), new XAttribute("max", 5), new XAttribute("width", 10)),
                    new XElement(Ns + "col", new XAttribute("min", 6), new XAttribute("max", 6), new XAttribute("width", 18))),
                sheetData));

        var workbook = new XDocument(
            new XElement(Ns + "workbook",
                new XAttribute(XNamespace.Xmlns + "r", RelNs),
                new XElement(Ns + "sheets",
                    new XElement(Ns + "sheet",
                        new XAttribute("name", "Asignaciones"),
                        new XAttribute("sheetId", 1),
                        new XAttribute(RelNs + "id", "rId1")))));

        var relacionesWorkbook = new XDocument(
            new XElement(PkgRelNs + "Relationships",
                new XElement(PkgRelNs + "Relationship",
                    new XAttribute("Id", "rId1"),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"),
                    new XAttribute("Target", "worksheets/sheet1.xml"))));

        var relacionesPaquete = new XDocument(
            new XElement(PkgRelNs + "Relationships",
                new XElement(PkgRelNs + "Relationship",
                    new XAttribute("Id", "rId1"),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),
                    new XAttribute("Target", "xl/workbook.xml"))));

        var tiposContenido = new XDocument(
            new XElement(ContentTypesNs + "Types",
                new XElement(ContentTypesNs + "Default",
                    new XAttribute("Extension", "rels"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                new XElement(ContentTypesNs + "Default",
                    new XAttribute("Extension", "xml"),
                    new XAttribute("ContentType", "application/xml")),
                new XElement(ContentTypesNs + "Override",
                    new XAttribute("PartName", "/xl/workbook.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
                new XElement(ContentTypesNs + "Override",
                    new XAttribute("PartName", "/xl/worksheets/sheet1.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"))));

        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            EscribirXml(zip, "[Content_Types].xml", tiposContenido);
            EscribirXml(zip, "_rels/.rels", relacionesPaquete);
            EscribirXml(zip, "xl/workbook.xml", workbook);
            EscribirXml(zip, "xl/_rels/workbook.xml.rels", relacionesWorkbook);
            EscribirXml(zip, "xl/worksheets/sheet1.xml", worksheet);
        }

        return buffer.ToArray();
    }

    private static readonly XNamespace Ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PkgRelNs = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace ContentTypesNs = "http://schemas.openxmlformats.org/package/2006/content-types";

    private static string[] FilaCsv(AsignacionAulaListadoDto fila) => new[]
    {
        fila.Id.ToString(CultureInfo.InvariantCulture),
        $"{fila.AulaNombre} · {fila.MateriaNombre}",
        fila.Estado,
        fila.DocenteNombre,
        $"{fila.CuposOcupados} / {fila.CupoHabilitado}",
        ZonaHorariaArgentina.FormatearFechaHora(fila.FinVigencia)
    };

    private static XElement FilaXml(int numeroFila, params string[] valores)
    {
        var fila = new XElement(Ns + "row", new XAttribute("r", numeroFila));

        for (var i = 0; i < valores.Length; i++)
        {
            fila.Add(new XElement(Ns + "c",
                new XAttribute("r", $"{Columna(i)}{numeroFila}"),
                new XAttribute("t", "inlineStr"),
                new XElement(Ns + "is",
                    new XElement(Ns + "t", valores[i]))));
        }

        return fila;
    }

    private static string Columna(int indice)
    {
        var letra = (char)('A' + indice);
        return letra.ToString();
    }

    private static void EscribirXml(ZipArchive zip, string ruta, XDocument documento)
    {
        var entrada = zip.CreateEntry(ruta, CompressionLevel.Optimal);
        using var stream = entrada.Open();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = false
        });
        documento.Save(writer);
    }

    private static string EscaparCsv(string? valor)
    {
        var texto = valor ?? string.Empty;
        if (texto.IndexOfAny(new[] { ';', '"', '\r', '\n' }) < 0)
            return texto;

        return $"\"{texto.Replace("\"", "\"\"")}\"";
    }
}
