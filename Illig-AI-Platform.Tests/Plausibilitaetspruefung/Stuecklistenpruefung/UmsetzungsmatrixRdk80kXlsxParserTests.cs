using ClosedXML.Excel;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Xunit;

namespace Illig_AI_Platform.Tests.Plausibilitaetspruefung.Stuecklistenpruefung;

public class UmsetzungsmatrixRdk80kXlsxParserTests
{
    private static Stream Speichern(XLWorkbook wb)
    {
        var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private static IXLWorksheet Rdk80Blatt(XLWorkbook wb) =>
        wb.AddWorksheet("Umsetzmatrix_V05_V06");

    [Fact]
    public void Parse_LiestRdk80UndGemeinsamenAbschnitt_UndIgnoriertRdkp72()
    {
        using var wb = new XLWorkbook();
        var ws = Rdk80Blatt(wb);
        ws.Cell(8, 12).Value = "Nachfolgende Stücklisten gelten nur für die RDK 80";
        ws.Cell(9, 12).Value = "9209711";
        ws.Cell(10, 13).Value = "9204455";
        ws.Cell(10, 23).Value = "014590";

        ws.Cell(12, 12).Value = "Ende RDK80; Nachfolgende Stücklisten gelten nur für die RDKP 72k";
        ws.Cell(13, 12).Value = "9209783";
        ws.Cell(13, 23).Value = "024934";

        ws.Cell(16, 12).Value = "Ende RDKP; Nachfolgende Stücklisten gelten für RDKP 72 und RDK 80";
        ws.Cell(17, 12).Value = "9209811";
        ws.Cell(17, 21).Value = "Option";
        ws.Cell(17, 23).Value = "021745";

        var zeilen = UmsetzungsmatrixRdk80kXlsxParser.Parse(Speichern(wb));

        Assert.Collection(
            zeilen,
            zeile =>
            {
                Assert.Equal(["9209425", "9209711", "9204455"], zeile.Pfad);
                Assert.Equal("014590", zeile.Bedingung);
            },
            zeile =>
            {
                Assert.Equal(["9209425", "9209811"], zeile.Pfad);
                Assert.Equal("021745", zeile.Bedingung);
            });
        Assert.DoesNotContain(zeilen, z => z.Pfad.Contains("9209783"));
    }

    [Fact]
    public void Parse_GemeinsamerAbschnitt_IgnoriertRdkpSpezifischeZeilen()
    {
        using var wb = new XLWorkbook();
        var ws = Rdk80Blatt(wb);
        ws.Cell(8, 12).Value = "Nachfolgende Stücklisten gelten nur für die RDK 80";
        ws.Cell(10, 12).Value = "Nachfolgende Stücklisten gelten nur für die RDKP 72k";
        ws.Cell(12, 12).Value = "Nachfolgende Stücklisten gelten für RDKP 72 und RDK 80";

        ws.Cell(13, 12).Value = "9209783";
        ws.Cell(13, 21).Value = "";
        ws.Cell(13, 22).Value = "Option";
        ws.Cell(13, 23).Value = "024934";

        ws.Cell(14, 12).Value = "9209784";
        ws.Cell(14, 21).Value = "-";
        ws.Cell(14, 22).Value = "Option";
        ws.Cell(14, 23).Value = "024934";

        var zeilen = UmsetzungsmatrixRdk80kXlsxParser.Parse(Speichern(wb));

        Assert.Empty(zeilen);
    }

    [Fact]
    public void Parse_GemeinsamerAbschnitt_UebernimmtGemeinsameStrukturzeile()
    {
        using var wb = new XLWorkbook();
        var ws = Rdk80Blatt(wb);
        ws.Cell(8, 12).Value = "Nachfolgende Stücklisten gelten nur für die RDK 80";
        ws.Cell(10, 12).Value = "Nachfolgende Stücklisten gelten nur für die RDKP 72k";
        ws.Cell(12, 12).Value = "Nachfolgende Stücklisten gelten für RDKP 72 und RDK 80";
        ws.Cell(13, 12).Value = "9209806"; // U und V leer: gemeinsamer Elternknoten
        ws.Cell(14, 13).Value = "9209807";
        ws.Cell(14, 21).Value = "Option";
        ws.Cell(14, 22).Value = "Option";
        ws.Cell(14, 23).Value = "019853";

        var zeile = Assert.Single(UmsetzungsmatrixRdk80kXlsxParser.Parse(Speichern(wb)));

        Assert.Equal(["9209425", "9209806", "9209807"], zeile.Pfad);
    }

    [Fact]
    public void Parse_AusgeschlossenesRdkpElternteil_UnterdruecktEingeruecktesKind()
    {
        using var wb = new XLWorkbook();
        var ws = Rdk80Blatt(wb);
        ws.Cell(8, 12).Value = "Nachfolgende Stücklisten gelten nur für die RDK 80";
        ws.Cell(10, 12).Value = "Nachfolgende Stücklisten gelten nur für die RDKP 72k";
        ws.Cell(12, 12).Value = "Nachfolgende Stücklisten gelten für RDKP 72 und RDK 80";
        ws.Cell(13, 12).Value = "9209783";
        ws.Cell(13, 22).Value = "Option"; // nur RDKP
        ws.Cell(14, 13).Value = "9204455";
        ws.Cell(14, 21).Value = "Option";
        ws.Cell(14, 22).Value = "Option";
        ws.Cell(14, 23).Value = "014590";

        var zeilen = UmsetzungsmatrixRdk80kXlsxParser.Parse(Speichern(wb));

        Assert.Empty(zeilen);
    }

    [Fact]
    public void Parse_IgnoriertDurchgestricheneAltnummer()
    {
        using var wb = new XLWorkbook();
        var ws = Rdk80Blatt(wb);
        ws.Cell(8, 12).Value = "Nachfolgende Stücklisten gelten nur für die RDK 80";
        ws.Cell(9, 12).Value = "9209711";
        ws.Cell(10, 13).Value = "9238185";
        ws.Cell(10, 13).Style.Font.Strikethrough = true;
        ws.Cell(10, 14).Value = "9290576";
        ws.Cell(10, 23).Value = "017367";

        var zeile = Assert.Single(UmsetzungsmatrixRdk80kXlsxParser.Parse(Speichern(wb)));

        Assert.Equal(["9209425", "9209711", "9290576"], zeile.Pfad);
        Assert.DoesNotContain("9238185", zeile.Pfad);
    }

    [Fact]
    public void Parse_FalschesErstesBlatt_WirftFormatException()
    {
        using var wb = new XLWorkbook();
        wb.AddWorksheet("Falsch");
        Rdk80Blatt(wb).Cell(8, 12).Value = "Nachfolgende Stücklisten gelten nur für die RDK 80";

        var fehler = Assert.Throws<FormatException>(
            () => UmsetzungsmatrixRdk80kXlsxParser.Parse(Speichern(wb)));

        Assert.Contains("Umsetzmatrix_V05_V06", fehler.Message);
    }

    [Fact]
    public void Parse_OhneRdk80Abschnitt_WirftFormatException()
    {
        using var wb = new XLWorkbook();
        var ws = Rdk80Blatt(wb);
        ws.Cell(8, 12).Value = "Nachfolgende Stücklisten gelten nur für die RDKP 72k";

        var fehler = Assert.Throws<FormatException>(
            () => UmsetzungsmatrixRdk80kXlsxParser.Parse(Speichern(wb)));

        Assert.Contains("RDK80", fehler.Message);
    }

    [Fact]
    public void Parse_FormatiertMerkmalsnummern_UndGruppiertSpaltenuebergreifendesOder()
    {
        using var wb = new XLWorkbook();
        var ws = Rdk80Blatt(wb);
        ws.Cell(8, 12).Value = "Nachfolgende Stücklisten gelten nur für die RDK 80";
        ws.Cell(9, 12).Value = "9209711";
        ws.Cell(9, 23).Value = "012692/014915";
        ws.Cell(9, 24).Value = "014916 oder";
        ws.Cell(9, 25).Value = 14917;
        ws.Cell(9, 25).Style.NumberFormat.Format = "000000";

        var zeile = Assert.Single(UmsetzungsmatrixRdk80kXlsxParser.Parse(Speichern(wb)));

        Assert.Equal("(012692 / 014915) U (014916 / 014917)", zeile.Bedingung);
    }

    [Fact]
    public void Parse_VerbindetStandaloneOder_UndOperatorfragmente()
    {
        using var wb = new XLWorkbook();
        var ws = Rdk80Blatt(wb);
        ws.Cell(8, 12).Value = "Nachfolgende Stücklisten gelten nur für die RDK 80";
        ws.Cell(9, 12).Value = "9209711";
        ws.Cell(9, 23).Value = "014936";
        ws.Cell(9, 24).Value = "ODER";
        ws.Cell(9, 25).Value = "000722/";
        ws.Cell(9, 26).Value = 723;
        ws.Cell(9, 26).Style.NumberFormat.Format = "000000";

        var zeile = Assert.Single(UmsetzungsmatrixRdk80kXlsxParser.Parse(Speichern(wb)));

        Assert.Equal("014936 / 000722 / 000723", zeile.Bedingung);
    }

    [Fact]
    public void Parse_LoestSondermarkerAusDenSpaltenueberschriftenAuf()
    {
        using var wb = new XLWorkbook();
        var ws = Rdk80Blatt(wb);
        ws.Cell(8, 12).Value = "Nachfolgende Stücklisten gelten nur für die RDK 80";
        ws.Cell(9, 12).Value = "9209711";

        ws.Cell(2, 23).Value = "000710 Hauptschalter";
        ws.Cell(9, 23).Value = "J";
        ws.Cell(2, 24).Value = "000711 Sicherheit";
        ws.Cell(9, 24).Value = "N";
        ws.Cell(2, 25).Value = "019873 oder 019874 oder 019875";
        ws.Cell(9, 25).Value = "x";
        ws.Cell(2, 26).Value = "022392 RDKL";
        ws.Cell(9, 26).Value = "!!";
        ws.Cell(2, 27).Value = "017309 Lochstanze";
        ws.Cell(9, 27).Value = "!!!";
        ws.Cell(2, 28).Value = "XXXXX zukünftige Option";
        ws.Cell(9, 28).Value = "XXXXX";

        var zeile = Assert.Single(UmsetzungsmatrixRdk80kXlsxParser.Parse(Speichern(wb)));

        Assert.Equal(
            "(000710) U (N000711) U (019873 / 019874 / 019875) U (022392) U (017309)",
            zeile.Bedingung);
    }

    [Fact]
    public void Parse_NMarkerMitMehrerenKopfmerkmalen_NegiertDieOdergruppe()
    {
        using var wb = new XLWorkbook();
        var ws = Rdk80Blatt(wb);
        ws.Cell(8, 12).Value = "Nachfolgende Stücklisten gelten nur für die RDK 80";
        ws.Cell(9, 12).Value = "9209711";
        ws.Cell(2, 23).Value = "019873 oder 019874";
        ws.Cell(9, 23).Value = "N";

        var zeile = Assert.Single(UmsetzungsmatrixRdk80kXlsxParser.Parse(Speichern(wb)));

        Assert.Equal("N (019873 / 019874)", zeile.Bedingung);
    }

    [Fact]
    public void Parse_ZaehltUnbedingteVorkommenDesselbenPfadsMit()
    {
        using var wb = new XLWorkbook();
        var ws = Rdk80Blatt(wb);
        ws.Cell(8, 12).Value = "Nachfolgende Stücklisten gelten nur für die RDK 80";
        ws.Cell(9, 12).Value = "9209711";
        ws.Cell(10, 12).Value = "9209711";
        ws.Cell(10, 23).Value = "014590";

        var zeile = Assert.Single(UmsetzungsmatrixRdk80kXlsxParser.Parse(Speichern(wb)));

        Assert.Equal(1, zeile.PfadVorkommen);
    }

    [Fact]
    public void Parse_NormalisiertAbgekuerztesUnd_UndErgaenztMerkmalsnullAusUeberschrift()
    {
        using var wb = new XLWorkbook();
        var ws = Rdk80Blatt(wb);
        ws.Cell(8, 12).Value = "Nachfolgende Stücklisten gelten nur für die RDK 80";
        ws.Cell(9, 12).Value = "9209711";
        ws.Cell(2, 23).Value = "021745 Stapelstation / 021746 Handlings-System";
        ws.Cell(9, 23).Value = "021745/21746 u.";
        ws.Cell(9, 24).Value = "014955";

        var zeile = Assert.Single(UmsetzungsmatrixRdk80kXlsxParser.Parse(Speichern(wb)));

        Assert.Equal("(021745 / 021746) U (014955)", zeile.Bedingung);
    }

    [Fact]
    public void Parse_NormalisiertUndImAusdruck_UndVerwirftNichtAuswertbarenBeschreibungstext()
    {
        using var wb = new XLWorkbook();
        var ws = Rdk80Blatt(wb);
        ws.Cell(8, 12).Value = "Nachfolgende Stücklisten gelten nur für die RDK 80";
        ws.Cell(9, 12).Value = "9209711";
        ws.Cell(9, 23).Value =
            "019875 u. 019873 u. keine Mühle / 019875 u.019874 u. keine Mühle";

        var zeile = Assert.Single(UmsetzungsmatrixRdk80kXlsxParser.Parse(Speichern(wb)));

        Assert.Equal("019875 U 019873 / 019875 U 019874", zeile.Bedingung);
        _ = UmsetzungsmatrixBedingung.Parse(zeile.Bedingung);
    }

    [Fact]
    public void Parse_NormalisiertAbgekuerztesOderUeberSpaltengrenze()
    {
        using var wb = new XLWorkbook();
        var ws = Rdk80Blatt(wb);
        ws.Cell(8, 12).Value = "Nachfolgende Stücklisten gelten nur für die RDK 80";
        ws.Cell(9, 12).Value = "9209711";
        ws.Cell(9, 23).Value = "014954 o.";
        ws.Cell(9, 24).Value = "014955";

        var zeile = Assert.Single(UmsetzungsmatrixRdk80kXlsxParser.Parse(Speichern(wb)));

        Assert.Equal("014954 / 014955", zeile.Bedingung);
    }

    [Theory]
    [InlineData("und 017310", "017310")]
    [InlineData("1x bei017041/2xbei019994", "017041 / 019994")]
    [InlineData("N 017310", "N 017310")]
    [InlineData("nicht 017310", "N 017310")]
    public void Parse_EntferntRedaktionelleWoerterAusMerkmalsausdruck(
        string quellwert,
        string erwarteteBedingung)
    {
        using var wb = new XLWorkbook();
        var ws = Rdk80Blatt(wb);
        ws.Cell(8, 12).Value = "Nachfolgende Stücklisten gelten nur für die RDK 80";
        ws.Cell(9, 12).Value = "9209711";
        ws.Cell(9, 23).Value = quellwert;

        var zeile = Assert.Single(UmsetzungsmatrixRdk80kXlsxParser.Parse(Speichern(wb)));

        Assert.Equal(erwarteteBedingung, zeile.Bedingung);
    }
}
