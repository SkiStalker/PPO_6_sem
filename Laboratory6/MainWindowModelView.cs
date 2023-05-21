using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FileClientUI;
using Microsoft.Win32;

namespace Laboratory6;

public class MainWindowModelView : ObservableObject
{
    private string? rows1;
    private string? rows2;
    private string? columns1;
    private string? columns2;
    private object? matrix1;
    private object? matrix2;
    private string? checkResult;
    private string? mtTime;
    private string? stTime;
    private ICommand? resizeCommand;
    private ICommand? loadFromFileCommand;
    private ICommand? checkCommand;

    public ICommand CheckCommand
    {
        get
        {
            return checkCommand ??= new RelayCommand((param) =>
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                Task<(double, bool)> m1MaxTask =
                    FindMaxMatrixElement(((DataView?)Matrix1 is { Table: { } })
                        ? ((DataView?)Matrix1).Table
                        : throw new NullReferenceException(nameof(Matrix1)));

                Task<(double, bool)> m2MaxTask =
                    FindMaxMatrixElement(((DataView?)Matrix2 is { Table: { } })
                        ? ((DataView?)Matrix2).Table
                        : throw new NullReferenceException(nameof(Matrix2)));

                Task.WaitAll(m1MaxTask, m2MaxTask);

                bool matrixNumber = m1MaxTask.Result.Item1 > m2MaxTask.Result.Item1;
                stopwatch.Stop();
                string matrixCheckRes;
                if (matrixNumber)
                {
                    matrixCheckRes = m1MaxTask.Result.Item2 ? "Success" : "Fail";
                }
                else
                {
                    matrixCheckRes = m2MaxTask.Result.Item2 ? "Success" : "Fail";
                }

                long mtElapsedTime = stopwatch.Elapsed.Nanoseconds;

                stopwatch.Restart();

                (double, bool) singleRes = FindMatrixNumberWithMaxElement();

                stopwatch.Stop();

                StTime = $"{stopwatch.Elapsed.Nanoseconds} ns";

                MtTime = $"{mtElapsedTime} ns";

                CheckResult = $"For {(matrixNumber ? "left" : "right")} matrix - {matrixCheckRes}";
            }, o => Matrix1 != null && Matrix2 != null);
        }
    }

    public string? CheckResult
    {
        get => checkResult;
        set
        {
            checkResult = value;
            OnPropertyChanged("CheckResult");
        }
    }

    public string? MtTime
    {
        get => mtTime;
        set
        {
            mtTime = value;
            OnPropertyChanged("MtTime");
        }
    }

    public string? StTime
    {
        get => stTime;
        set
        {
            stTime = value;
            OnPropertyChanged("StTime");
        }
    }

    public ICommand LoadFromFileCommand
    {
        get
        {
            return loadFromFileCommand ??= new RelayCommand((param) =>
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Text files |*.txt";
                if (openFileDialog.ShowDialog() ?? false)
                {
                    if (TryLoadMatricesFromFile(openFileDialog.FileName ?? "", out double[][] _matrix1,
                            out double[][] _matrix2))
                    {
                        Rows1 = $"{_matrix1.Length}";
                        Columns1 = $"{(_matrix1.Length > 0 ? _matrix1[0].Length : 0)}";
                        Rows2 = $"{_matrix2.Length}";
                        Columns2 = $"{(_matrix2.Length > 0 ? _matrix2[0].Length : 0)}";
                        SetMatrix(Rows1, Columns1, Matrix1, (m) => { Matrix1 = m; }, _matrix1);
                        SetMatrix(Rows2, Columns2, Matrix2, (m) => { Matrix2 = m; }, _matrix2);
                    }
                    else
                    {
                        Matrix1 = null;
                        Matrix2 = null;
                        Rows1 = null;
                        Rows2 = null;
                        Columns1 = null;
                        Columns2 = null;
                    }

                    MtTime = null;
                    StTime = null;
                    CheckResult = null;
                }
            });
        }
    }

    public ICommand ResizeCommand
    {
        get
        {
            return resizeCommand ??= new RelayCommand((param) =>
                {
                    SetMatrix(Rows1, Columns1, Matrix1, (val) => { Matrix1 = val; });
                    SetMatrix(Rows2, Columns2, Matrix2, (val) => { Matrix2 = val; });
                    StTime = null;
                    MtTime = null;
                    CheckResult = null;
                },
                (param) => !string.IsNullOrWhiteSpace(Rows1) && !string.IsNullOrWhiteSpace(Rows2) &&
                           !string.IsNullOrWhiteSpace(Columns1) && !string.IsNullOrWhiteSpace(Columns2));
        }
    }

    public string? Rows1
    {
        get => rows1;
        set
        {
            rows1 = value;
            OnPropertyChanged("Rows1");
        }
    }

    public string? Rows2
    {
        get => rows2;
        set
        {
            rows2 = value;
            OnPropertyChanged("Rows2");
        }
    }

    public string? Columns1
    {
        get => columns1;
        set
        {
            columns1 = value;
            OnPropertyChanged("Columns1");
        }
    }

    public string? Columns2
    {
        get => columns2;
        set
        {
            columns2 = value;
            OnPropertyChanged("Columns2");
        }
    }

    public object? Matrix1
    {
        get => matrix1;
        set
        {
            matrix1 = value;
            OnPropertyChanged("Matrix1");
        }
    }

    public object? Matrix2
    {
        get => matrix2;
        set
        {
            matrix2 = value;
            OnPropertyChanged("Matrix2");
        }
    }


    private async Task<(double, bool)> FindMaxMatrixElement(DataTable matrix)
    {
        List<Task<(double, bool)>> tasks = new List<Task<(double, bool)>>();
        for (int i = 0; i < Environment.ProcessorCount; i++)
        {
            int threadI = i;
            Task<(double, bool)> newTask = new Task<(double, bool)>(() =>
            {
                double? threadMax = null;
                bool threadCheck = true;

                for (int k = threadI; k < matrix.Rows.Count; k += Environment.ProcessorCount)
                {
                    double? localMax = null;
                    bool localCheck = false;

                    for (int j = 0; j < matrix.Columns.Count; j++)
                    {
                        double curElement = (double)matrix.Rows[k][j];
                        localMax = Math.Max(localMax ?? curElement, curElement);
                        localCheck |= curElement != 0;
                    }

                    if (threadMax == null)
                    {
                        threadMax = localMax;
                        threadCheck = localCheck;
                    }
                    else if ((localMax ?? 0) > threadMax)
                    {
                        threadMax = localMax;
                        threadCheck &= localCheck;
                    }
                    else
                    {
                        threadCheck &= localCheck;
                    }
                }

                return (threadMax ?? 0, threadCheck);
            });
            newTask.Start();
            tasks.Add(newTask);
        }

        (double, bool)[] res = await Task.WhenAll(tasks);

        double? max = null;
        bool check = true;

        for (int i = 0; i < res.Length; i++)
        {
            if (max == null)
            {
                max = res[i].Item1;
                check = res[i].Item2;
            }
            else if (res[i].Item1 > max)
            {
                max = res[i].Item1;
                check &= res[i].Item2;
            }
            else
            {
                check &= res[i].Item2;
            }
        }

        return (max ?? 0, check);
    }

    private (double, bool) FindMatrixNumberWithMaxElement()
    {
        DataTable table1 = (DataView?)Matrix1 is { Table : { } }
            ? ((DataView?)Matrix1).Table
            : throw new NullReferenceException(nameof(Matrix1));

        DataTable table2 = (DataView?)Matrix2 is { Table : { } }
            ? ((DataView?)Matrix2).Table
            : throw new NullReferenceException(nameof(Matrix2));
        double? max1 = null;
        bool check1 = true;
        double? max2 = null;
        bool check2 = true;

        for (int i = 0; i < table1.Rows.Count; i++)
        {
            (max1, check1) = FindLocalMaxAndCheck(table1, i, max1, check1);
        }

        for (int i = 0; i < table2.Rows.Count; i++)
        {
            (max2, check2) = FindLocalMaxAndCheck(table2, i, max2, check2);
        }

        if (max1 == null)
        {
            return ((max2 ?? 0), check2);
        }
        else if (max2 == null)
        {
            return ((max1 ?? 0), check1);
        }
        else if (max1 > max2)
        {
            return ((max1 ?? 0), check1);
        }
        else
        {
            return ((max2 ?? 0), check2);
        }
    }

    private (double, bool) FindLocalMaxAndCheck(DataTable table, int i, double? threadMax, bool threadCheck)
    {
        double? localMax = null;
        bool localCheck = false;
        for (int j = 0; j < table.Columns.Count; j++)
        {
            double curElement = (double)table.Rows[i][j];
            localMax = Math.Max(localMax ?? curElement, curElement);
            localCheck |= curElement != 0;
        }

        if (threadMax == null)
        {
            return (localMax ?? 0, localCheck);
        }
        else if ((localMax ?? 0) > threadMax)
        {
            return ((localMax ?? 0), localCheck & threadCheck);
        }
        else
        {
            return ((threadMax ?? 0), threadCheck & localCheck);
        }
    }

    private double[][] ReadMatrix(double[] doubleParts, int off)
    {
        int matrix1Rows = (int)doubleParts[0 + off];
        int matrix1Columns = (int)doubleParts[1 + off];
        double[][] resMatrix = new double[matrix1Rows][];
        for (int i = 0; i < matrix1Rows; i++)
        {
            resMatrix[i] = new double[matrix1Columns];
            for (int j = 0; j < matrix1Columns; j++)
            {
                resMatrix[i][j] = doubleParts[i * matrix1Columns + j + off + 2];
            }
        }

        return resMatrix;
    }

    private bool TryLoadMatricesFromFile(string fileName, out double[][] _matrix1, out double[][] _matrix2)
    {
        if (File.Exists(fileName))
        {
            string data = File.ReadAllText(fileName);

            IEnumerable<string> parts = data.Split(new char[] { ' ', '\r', '\n', '\t' },
                StringSplitOptions.RemoveEmptyEntries);

            if (parts.Count(part => double.TryParse(part, out double _)) != parts.Count())
            {
                MessageBox.Show("Incorrect file values");
                _matrix1 = Array.Empty<double[]>();
                _matrix2 = Array.Empty<double[]>();
                return false;
            }
            else
            {
                double[] doubleParts = parts.Select(double.Parse).ToArray();
                try
                {
                    _matrix1 = ReadMatrix(doubleParts, 0);
                    _matrix2 = ReadMatrix(doubleParts,
                        _matrix1.Length * (_matrix1.Length > 0 ? _matrix1[0].Length : 0) + 2);
                }
                catch
                {
                    MessageBox.Show("Incorrect matrices structure");
                    _matrix1 = Array.Empty<double[]>();
                    _matrix2 = Array.Empty<double[]>();
                    return false;
                }
            }

            return true;
        }
        else
        {
            _matrix1 = Array.Empty<double[]>();
            _matrix2 = Array.Empty<double[]>();
            return false;
        }
    }

    private void SetMatrix(string? rows, string? columns, object? matrix, Action<object?> setMatrix,
        double[][]? initMatrix = null)
    {
        if (!string.IsNullOrEmpty(rows) && !string.IsNullOrEmpty(columns))
        {
            if (int.TryParse(rows, out int intRows) && int.TryParse(columns, out int intColumns))
            {
                DataView? oldDataTable = (DataView?)matrix;

                DataTable dataTable = new DataTable();
                for (int j = 0; j < intColumns; j++)
                {
                    dataTable.Columns.Add(new DataColumn($"c{j}", typeof(double)));
                }

                for (int i = 0; i < intRows; i++)
                {
                    DataRow dataRow = dataTable.NewRow();
                    for (int j = 0; j < intColumns; j++)
                    {
                        if (initMatrix != null)
                        {
                            dataRow[j] = initMatrix[i][j];
                        }
                        else
                        {
                            if (oldDataTable is { Table: { } } &&
                                oldDataTable.Table.Rows.Count > i && oldDataTable.Table.Rows[i].ItemArray.Length > j)
                            {
                                dataRow[j] = oldDataTable.Table.Rows[i][j];
                            }
                            else
                            {
                                dataRow[j] = 0;
                            }
                        }
                    }

                    dataTable.Rows.Add(dataRow);
                }

                setMatrix(dataTable.DefaultView);
            }
        }
    }
}