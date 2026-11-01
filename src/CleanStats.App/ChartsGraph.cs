using LiveChartsCore;
using LiveChartsCore.Kernel.Events;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Xml.Linq;
namespace CleanStats.App;

public class ItemData
{
    public string nombre { get; set; }
    public long valor { get; set; }
    public SolidColorPaint color { get; set; }
}
public class ChartsGraph
{
    public ObservableCollection<ISeries> Series { get; } = [];
    public Axis[] XAxes { get; private set; } = [];

    public void PieChart(IEnumerable<ItemData> items, bool isStorage = false)
    {
        Series.Clear();

        var data = items.ToList();
        var total = data.Sum(x => (double)x.valor);

        var pieSeries = new PieSeries<double>
        {
            Name = "Categorías",
            InnerRadius = 70,
            Values = data.Select(x => (double)x.valor).ToArray(),
            ToolTipLabelFormatter = point =>
            {
               if (isStorage)
                {
                    int index = (int)point.Index;
                    var item = data[index];

                    double percentage = total > 0
                        ? item.valor / total * 100
                        : 0;

                    return $"{item.nombre}: {FormatBytes(item.valor)} ({percentage:N2}%)";
                }
                else
                {
                    int index = (int)point.Index;
                    var item = data[index];
                    return $"{item.nombre}: {item.valor})";
                }
            }
        };

        // Cada porción recibe su propio color.
        pieSeries.PointMeasured += point =>
        {
            int index = (int)point.Index;
            point.Context.Visual.Fill = data[index].color;
        };

        Series.Add(pieSeries);
    }
    public void ColumnChart(IEnumerable<ItemData> items)
    {
        Series.Clear();

        var data = items.ToList();

        XAxes =
        [
            new Axis
            {
                Labels = ["Valores"]
            }
        ];

        foreach (var item in data)
        {
            Series.Add(new ColumnSeries<double>
            {
                Name = item.nombre,
                Values = [(double)item.valor],
                Fill = item.color
            });
        }
    }
    public void SlimGauge(IEnumerable<ItemData> items)
    {
        Series.Clear();

        var data = items.ToList();

        foreach (var item in data)
        {
            Series.Add(new PieSeries<double>
            {
                Name = item.nombre,
                Values = [Math.Clamp(item.valor, 0, 100)],
                InnerRadius = 75,
                RelativeOuterRadius = 100,
                RelativeInnerRadius = 100,
                Fill = item.color,
                MaxRadialColumnWidth = 10,
                IsHoverable = false,

                // Mostrar etiquetas
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsSize = 16,
                DataLabelsFormatter = point =>
                    $"{item.nombre}",
            });
        }
    }
    public string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];

        double size = bytes;
        int order = 0;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:N2} {sizes[order]}";
    }

}
