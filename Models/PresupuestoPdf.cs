using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WebViajes.Models;

namespace WebViajes.Services;

public class PresupuestoPdf : IDocument
{
    private readonly Presupuesto _p;
    public PresupuestoPdf(Presupuesto presupuesto) => _p = presupuesto;

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        var total = _p.Partidas.Sum(x => x.Importe);
        var personas = _p.Participantes?.Count ?? 0;

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(t => t.FontSize(11));

            page.Header().Column(col =>
            {
                col.Item().Text(_p.Nombre).FontSize(22).SemiBold();
                col.Item().Text($"Generado el {DateTime.Now:dd/MM/yyyy}")
                    .FontSize(9).FontColor(Colors.Grey.Darken1);
            });

            page.Content().PaddingVertical(15).Column(col =>
            {
                col.Spacing(10);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn();
                        c.ConstantColumn(100);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Concepto").SemiBold();
                        h.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Precio").SemiBold();
                    });

                    foreach (var partida in _p.Partidas)
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                            .Padding(5).Column(c =>
                            {
                                c.Item().Text(partida.Concepto).SemiBold();
                                if (!string.IsNullOrWhiteSpace(partida.Nota))
                                    c.Item().Text(partida.Nota).FontSize(9).Italic()
                                        .FontColor(Colors.Grey.Darken1);
                            });
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                            .Padding(5).AlignRight().Text($"{partida.Importe:N2} €");

                        foreach (var d in partida.Detalles)
                        {
                            table.Cell().PaddingLeft(20).PaddingVertical(2)
                                .Text(d.Concepto).FontSize(10).FontColor(Colors.Grey.Darken2);
                            table.Cell().PaddingVertical(2).AlignRight()
                                .Text($"{d.Importe:N2} €").FontSize(10).FontColor(Colors.Grey.Darken2);
                        }
                    }
                });

                col.Item().AlignRight().Text($"Total: {total:N2} €").FontSize(14).Bold();

                if (personas > 0)
                    col.Item().AlignRight()
                        .Text($"Entre {personas} personas: {total / personas:N2} € por persona");
            });

            page.Footer().AlignCenter().Text(t =>
            {
                t.Span("Página ");
                t.CurrentPageNumber();
                t.Span(" de ");
                t.TotalPages();
            });
        });
    }
}