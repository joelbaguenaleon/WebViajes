using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WebViajes.Models;

namespace WebViajes.Services
{
    public class RepartoPdf : IDocument
    {
        private static readonly CultureInfo Es = new("es-ES");
        private readonly ResumenReparto _r;

        public RepartoPdf(ResumenReparto resumen)
        {
            _r = resumen;
        }

        private static string Eur(decimal d) => d.ToString("N2", Es) + " €";

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10.5f));

                page.Header().Column(col =>
                {
                    col.Item().Text(_r.Nombre).FontSize(22).SemiBold();
                    col.Item().Text($"Reparto de gastos · {DateTime.Now.ToString("dd/MM/yyyy", Es)}")
                        .FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Spacing(18);

                    // ---- Gastos ----
                    col.Item().Text("Gastos").FontSize(14).SemiBold();
                    if (_r.Gastos.Count == 0)
                    {
                        col.Item().Text("No hay gastos.").FontColor(Colors.Grey.Darken1);
                    }
                    else
                    {
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(65);
                                c.RelativeColumn(2);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(2);
                                c.ConstantColumn(70);
                            });

                            t.Header(h =>
                            {
                                h.Cell().Element(Cab).Text("Fecha");
                                h.Cell().Element(Cab).Text("Concepto");
                                h.Cell().Element(Cab).Text("Pagó");
                                h.Cell().Element(Cab).Text("Participan");
                                h.Cell().Element(Cab).AlignRight().Text("Importe");
                            });

                            foreach (var g in _r.Gastos)
                            {
                                t.Cell().Element(Fila).Text(g.Fecha.ToString("dd/MM/yyyy", Es));
                                t.Cell().Element(Fila).Column(c =>
                                {
                                    c.Item().Text(g.Concepto);
                                    if (!string.IsNullOrWhiteSpace(g.Notas))
                                        c.Item().Text(g.Notas).FontSize(9).FontColor(Colors.Grey.Darken1);
                                });
                                t.Cell().Element(Fila).Text(g.PagadoPor);
                                t.Cell().Element(Fila).Text(g.Participantes);
                                t.Cell().Element(Fila).AlignRight().Text(Eur(g.Importe));
                            }
                        });

                        col.Item().AlignRight().Text($"Total: {Eur(_r.Total)}").SemiBold().FontSize(12);
                    }

                    // ---- Balance ----
                    col.Item().Text("Balance").FontSize(14).SemiBold();
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        t.Header(h =>
                        {
                            h.Cell().Element(Cab).Text("Miembro");
                            h.Cell().Element(Cab).AlignRight().Text("Ha pagado");
                            h.Cell().Element(Cab).AlignRight().Text("Le corresponde");
                            h.Cell().Element(Cab).AlignRight().Text("Saldo");
                        });

                        foreach (var b in _r.Balances)
                        {
                            var color = b.Saldo > 0 ? Colors.Green.Darken2
                                      : b.Saldo < 0 ? Colors.Red.Darken2
                                      : Colors.Black;

                            t.Cell().Element(Fila).Text(b.Nombre);
                            t.Cell().Element(Fila).AlignRight().Text(Eur(b.Pagado));
                            t.Cell().Element(Fila).AlignRight().Text(Eur(b.Debe));
                            t.Cell().Element(Fila).AlignRight().Text(Eur(b.Saldo)).FontColor(color).SemiBold();
                        }
                    });

                    // ---- Liquidación ----
                    col.Item().Text("Quién paga a quién").FontSize(14).SemiBold();
                    if (_r.Liquidaciones.Count == 0)
                    {
                        col.Item().Text("Todas las cuentas están saldadas.").FontColor(Colors.Grey.Darken1);
                    }
                    else
                    {
                        foreach (var l in _r.Liquidaciones)
                        {
                            col.Item().Text(t =>
                            {
                                t.Span(l.De).SemiBold();
                                t.Span(" paga ");
                                t.Span(Eur(l.Importe)).SemiBold();
                                t.Span(" a ");
                                t.Span(l.A).SemiBold();
                            });
                        }
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        }

        private static IContainer Cab(IContainer c) =>
            c.BorderBottom(1).BorderColor(Colors.Grey.Darken2).PaddingVertical(4).DefaultTextStyle(x => x.SemiBold());

        private static IContainer Fila(IContainer c) =>
            c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4);
    }
}