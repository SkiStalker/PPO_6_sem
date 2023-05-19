using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Laboratory4
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string? a;
        private string? n;
        private string? x1;
        private string? dx;
        private string? rows;
        private string? columns;

        public event PropertyChangedEventHandler? PropertyChanged;


        private object? matrix;
        private string? functionResult;

        public object? Matrix
        {
            get => matrix;
            set
            {
                matrix = value;
                OnPropertyChanged("Matrix");
            }
        }

        public string? FunctionResult
        {
            get => functionResult;
            set
            {
                functionResult = value;
                OnPropertyChanged("FunctionResult");
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void CalcFunction()
        {
            DataView? dataView = (DataView?)Matrix;
            if (dataView is { Table: { } })
            {
                int topZeros = 0;
                int bottomZeros = 0;
                int rowsCount = dataView.Table.Rows.Count;
                int columnsCount = dataView.Table.Columns.Count;
                for (int i = 0; i < rowsCount / 2; i++)
                {
                    for (int j = 0; j < columnsCount; j++)
                    {
                        topZeros += (double)(dataView.Table.Rows[i][j] ?? 0) == 0 ? 1 : 0;

                        bottomZeros += (double)(dataView.Table.Rows[i + rowsCount / 2 + rowsCount % 2][j] ?? 0) == 0
                            ? 1
                            : 0;
                    }
                }

                if (topZeros > bottomZeros)
                {
                    FunctionResult = "Больше в верхней";
                }
                else if (topZeros < bottomZeros)
                {
                    FunctionResult = "Больше в нижней";
                }
                else
                {
                    FunctionResult = "Одинаково";
                }
            }
            else
            {
                FunctionResult = "Undefined";
            }
        }

        private void SetXYGrid()
        {
            if (!string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(n) && !string.IsNullOrEmpty(x1) &&
                !string.IsNullOrEmpty(dx))
            {
                if (double.TryParse(a, out double doubleA) && double.TryParse(n, out double doubleN) &&
                    double.TryParse(x1, out double doubleX1) && double.TryParse(dx, out double doubleDX))

                {
                    XYGrid.Items.Clear();
                    double y = 0;
                    double x = 0;
                    for (int i = 0; i < doubleN; i++)
                    {
                        x = doubleX1 + doubleDX * i;
                        if (x < 0)
                        {
                            y = Math.Sqrt(Math.Cbrt(doubleA * doubleA) - Math.Cbrt(x * x));
                        }
                        else
                        {
                            y = doubleA + Math.Sqrt(Math.Pow(x, 3) / (2 * doubleA - x));
                        }

                        XYGrid.Items.Add(new Point(x, y));
                    }
                }
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void SetMatrix()
        {
            if (!string.IsNullOrEmpty(rows) && !string.IsNullOrEmpty(columns))
            {
                if (int.TryParse(rows, out int intRows) && int.TryParse(columns, out int intColumns))
                {
                    DataView? oldDataTable = (DataView?)Matrix;

                    DataTable dataTable = new DataTable();
                    for (int i = 0; i < intColumns; i++)
                    {
                        dataTable.Columns.Add(new DataColumn($"c{i}", typeof(double)));
                    }

                    for (int j = 0; j < intRows; j++)
                    {
                        DataRow dataRow = dataTable.NewRow();
                        for (int c = 0; c < intColumns; c++)
                        {
                            if (oldDataTable is { Table: { } } &&
                                oldDataTable.Table.Rows.Count > j && oldDataTable.Table.Rows[j].ItemArray.Length > c)
                            {
                                dataRow[c] = oldDataTable.Table.Rows[j][c];
                            }
                            else
                            {
                                dataRow[c] = 0;
                            }
                        }

                        dataTable.Rows.Add(dataRow);
                    }

                    Matrix = dataTable.DefaultView;
                }
            }
        }

        private void A_TextChanged(object sender, TextChangedEventArgs e)
        {
            a = A.Text;
            SetXYGrid();
        }

        private void X1_TextChanged(object sender, TextChangedEventArgs e)
        {
            x1 = XInit.Text;
            SetXYGrid();
        }

        private void DX_TextChanged(object sender, TextChangedEventArgs e)
        {
            dx = DX.Text;
            SetXYGrid();
        }

        private void N_TextChanged(object sender, TextChangedEventArgs e)
        {
            n = N.Text;
            SetXYGrid();
        }

        private void Resize_OnClick(object sender, RoutedEventArgs e)
        {
            rows = Rows.Text;
            columns = Columns.Text;
            SetMatrix();
            CalcFunction();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            CalcFunction();
        }
    }
}