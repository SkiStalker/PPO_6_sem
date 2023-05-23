using System;
using System.Data;
using System.IO;
using FileClientUI;
using Microsoft.VisualBasic.FileIO;

namespace Laboratory8;

public class UserDataGridModel: ObservableObject
{
    private static Type GetTypeByString(string str)
    {
        switch (str)
        {
            case "str": return typeof(string);
            case "int": return typeof(int);
            case "double": return typeof(double);
            case "datetime": return typeof(DateTime);
            default: throw new ArgumentException(str);
        }
    }

    private static bool ParseField(string field, string fieldType, out object typedField)
    {
        if (fieldType == "str")
        {
            typedField = field;
            return true;
        }
        else if (fieldType == "int")
        {
            if (int.TryParse(field, out int res))
            {
                typedField = res;
                return true;
            }
            else
            {
                typedField = default(int);
                return false;
            }
        }
        else if (fieldType == "double")
        {
            if (double.TryParse(field.Replace(".", ","), out double res))
            {
                typedField = res;
                return true;
            }
            else
            {
                typedField = default(double);
                return false;
            }
        }
        else if (fieldType == "datetime")
        {
            if (DateTime.TryParse(field, out DateTime res))
            {
                typedField = res;
                return true;
            }
            else
            {
                typedField = default(DateTime);
                return false;
            }
        }
        else
        {
            typedField = field;
            return false;
        }
    }

    public static string Parse(Stream inputStream, out DataTable newDataTable)
    {
        using TextFieldParser parser = new TextFieldParser(inputStream);
        parser.SetDelimiters(",");

        newDataTable = new DataTable();
        string[]? columnNames = parser.ReadFields();
        if (columnNames == null)
        {
            return "Can not read DB column names";
        }
        else
        {
            string[]? columnTypes = parser.ReadFields();
            if (columnTypes == null)
            {
                return "Can not read DB column types";
            }
            else
            {
                if (columnNames.Length == 0 || columnNames.Length != columnTypes.Length)
                {
                    return "Incorrect DB header - incapable column names and types";
                }
                else
                {
                    string loadResult = "";
                    try
                    {
                        for (int i = 0; i < columnNames.Length; i++)
                        {
                            newDataTable.Columns.Add(new DataColumn(columnNames[i],
                                GetTypeByString(columnTypes[i])));
                        }
                    }
                    catch
                    {
                        loadResult = "Incorrect row type";
                    }

                    if (loadResult == "")
                    {
                        for (int j = 2; !parser.EndOfData; j++)
                        {
                            string[] fields = parser.ReadFields() ?? Array.Empty<string>();

                            if (fields.Length != columnNames.Length)
                            {
                                loadResult = "Incorrect row fields length";
                                break;
                            }
                            else
                            {
                                DataRow newDataRow = newDataTable.NewRow();
                                for (int i = 0; i < fields.Length; i++)
                                {
                                    if (ParseField(fields[i], columnTypes[i], out object typedField))
                                    {
                                        newDataRow[i] = typedField;
                                    }
                                    else
                                    {
                                        loadResult = $"Incorrect field type {i} on row {j}";
                                    }
                                }

                                if (loadResult != "")
                                {
                                    break;
                                }
                                else
                                {
                                    newDataTable.Rows.Add(newDataRow);
                                }
                            }
                        }
                    }

                    return loadResult;
                }
            }
        }
    }


    private bool isSaved;

    private object? dataView;

    private string name;

    public bool IsSaved
    {
        get => isSaved;
        private set
        {
            isSaved = value;
            OnPropertyChanged("IsSaved");
        }
    }

    public object? DataView
    {
        get => dataView;
        set
        {
            dataView = value;
            OnPropertyChanged("DataView");
            IsSaved = false;
        }
    }

    public string Name
    {
        get => name;
        set
        {
            name = value;
            OnPropertyChanged("Name");
        } }

    public Guid Id
    {
        get;
    }
    
    public UserDataGridModel(DataTable dataTable, string name)
    {
        Name = name;
        Id = Guid.NewGuid();
        DataView = dataTable.DefaultView;
    }
}