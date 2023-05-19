using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Input;
using FileClientUI;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace Laboratory5;

public class MainWindowViewModel : ObservableObject
{
    private PlotModel? graphModel;
    private object? matrix;
    private ICommand? loadFileCommand;

    public object? Matrix
    {
        get => matrix;
        set
        {
            matrix = value;
            OnPropertyChanged("Matrix");
        }
    }

    public ICommand LoadFileCommand
    {
        get
        {
            return loadFileCommand ??= new RelayCommand(param =>
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Txt files |*.txt";

                if (openFileDialog.ShowDialog() ?? false)
                {
                    using Stream stream = openFileDialog.OpenFile();
                    using StreamReader reader = new StreamReader(stream);

                    DataTable newDataTable = new DataTable();
                    List<Point> points = new List<Point>();
                    double sx4 = 0;
                    double sx3 = 0;
                    double sx2 = 0;
                    double sx2y = 0;
                    double sx = 0;
                    double sxy = 0;
                    double sy = 0;
                    double n = 0;
                    double s1_x = 0;
                    double sy_x = 0;
                    double s1_x2 = 0;
                    double? minX = null;
                    double? maxX = null;
                    double? minY = null;
                    double? maxY = null;


                    newDataTable.Columns.Clear();
                    newDataTable.Rows.Clear();
                    newDataTable.Columns.Add(new DataColumn("X", typeof(double)));
                    newDataTable.Columns.Add(new DataColumn("Y", typeof(double)));


                    while (reader.ReadLine() is { } str)
                    {
                        string[] parts = str.Trim().Split(":");
                        if (parts.Length == 2)
                        {
                            if (double.TryParse(parts[0].Trim(), out double x) &&
                                double.TryParse(parts[1].Trim(), out double y))
                            {
                                maxX = Math.Max(maxX ??= x, x);
                                minX = Math.Min(minX ??= x, x);
                                maxY = Math.Max(maxY ??= y, y);
                                minY = Math.Min(minY ??= y, y);
                                
                                
                                points.Add(new Point(x, y));
                                sx4 += Math.Pow(x, 4);
                                sx3 += Math.Pow(x, 3);
                                sx2 += Math.Pow(x, 2);
                                sx += x;
                                sx2y += Math.Pow(x, 2) * y;
                                sxy += x * y;
                                sy += y;
                                n += 1;
                                s1_x2 += Math.Pow((1 / x), 2);
                                s1_x += 1 / x;
                                sy_x += y / x;

                                DataRow newRow = newDataTable.NewRow();
                                newRow[0] = x;
                                newRow[1] = y;
                                newDataTable.Rows.Add(newRow);
                            }
                        }
                    }

                    (double qA, double qB, double qC, double hA, double hB) =
                        CalcFunctions(sx4, sx3, sx2, sx2y, sx, sxy, sy, n, s1_x, sy_x, s1_x2);

                    PlotModel plotModel = new PlotModel();

                    LinearAxis horizontalAxis = new LinearAxis()
                        { Position = AxisPosition.Bottom, Minimum = minX - 5 ?? -1, Maximum = maxX + 5 ?? 1, IsZoomEnabled = false, Title = "X", MajorGridlineStyle = LineStyle.Solid, MajorStep = 1};
                    plotModel.Axes.Add(horizontalAxis);
                    
                    LinearAxis verticalAxis = new LinearAxis()
                        { Position = AxisPosition.Left, Minimum = minY - 5 ?? -1, Maximum = maxY + 5 ?? 1 , IsZoomEnabled = false, Title = "Y", MajorGridlineStyle = LineStyle.Solid, MajorStep = 1};
                    plotModel.Axes.Add(verticalAxis);
                    
                    plotModel.Series.Add(new FunctionSeries(x => qA * x * x + qB * x + qC, minX - 5 ?? -1,
                        maxX + 5 ?? 1, 0.1, "Quadratic"){Color = OxyColors.Green});
                    
                    plotModel.Series.Add(new FunctionSeries(x => hA + hB / x, minX - 5 ?? -1,
                        -0.1, 0.1, "Hyperbolic"){Color = OxyColors.Blue});
                    
                    plotModel.Series.Add(new FunctionSeries(x => hA + hB / x, 0.1,
                        maxX + 5 ?? 1, 0.1, "Hyperbolic"){Color = OxyColors.Blue});

                    LineSeries dots= new LineSeries()
                    {
                        Color = OxyColors.Transparent,
                        MarkerFill = OxyColors.Red,
                        MarkerType = MarkerType.Circle,
                        Title = "Dots"
                    };

                    foreach (Point point in points)
                    {
                        dots.Points.Add(new DataPoint(point.X, point.Y));
                    }
                    plotModel.Series.Add(dots);
                    
                    GraphModel = plotModel;
                    
                    Matrix = newDataTable.DefaultView;
                }
            });
        }
    }

    public PlotModel GraphModel
    {
        get => graphModel ?? new PlotModel();
        set
        {
            graphModel = value;
            OnPropertyChanged("GraphModel");
        }
    }

    private (double, double, double, double, double) CalcFunctions(double sx4,
        double sx3,
        double sx2,
        double sx2y,
        double sx,
        double sxy,
        double sy,
        double n,
        double s1_x,
        double sy_x,
        double s1_x2)
    {
        double hA = (sy * s1_x2 - s1_x * sy_x) / (n * s1_x2 - s1_x * s1_x);

        double hB = (n * sy_x - s1_x * sy) / (n * s1_x2 - s1_x * s1_x);

        GaussMethod gaussMethod = new GaussMethod(3, 3)
        {
            Matrix = new double[][]
            {
                new double[] { sx4, sx3, sx2 },
                new double[] { sx3, sx2, sx },
                new double[] { sx2, sx, n }
            },
            RightPart = new double[]
            {
                sx2y, sxy, sy
            }
        };

        gaussMethod.SolveMatrix();

        double qA = gaussMethod.Answer[0];
        double qB = gaussMethod.Answer[1];
        double qC = gaussMethod.Answer[2];


        return (qA, qB, qC, hA, hB);
    }
}