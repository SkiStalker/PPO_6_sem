using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using FileClientUI;

namespace Laboratory8;

public class UserDataGridViewModel : ObservableObject, IPageViewModel
{
    private ICommand? closeCommand;
    private int? selectedTabIndex;

    public int? SelectedTabIndex
    {
        get => selectedTabIndex ?? null;
        set
        {
            selectedTabIndex = value;
            OnPropertyChanged("SelectedTabIndex");
        }
    }


    public ICommand CloseCommand
    {
        get
        {
            return closeCommand ??= new RelayCommand((param) =>
            {
                UserDataGridModel typedParam =
                    (UserDataGridModel)(param ?? throw new NullReferenceException(nameof(param)));
                DataTables.Remove(DataTables.Where((item) => item.Id == typedParam.Id).ToArray()[0]);
            }, o => o is UserDataGridModel);
        }
    }

    public string Name => "Data grid";
    public ObservableCollection<UserDataGridModel> DataTables { get; }

    public UserDataGridViewModel(ObservableCollection<UserDataGridModel> dataTables)
    {
        DataTables = dataTables;
    }
}