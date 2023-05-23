using System.Collections.Generic;
using System.Collections.ObjectModel;
using FileClientUI;

namespace Laboratory8;

public class UserDataDiagramViewModel : ObservableObject, IPageViewModel
{
    public string Name => "Data diagram";

    public UserDataDiagramViewModel(ObservableCollection<UserDataGridModel> dataTables)
    {
        
    }
}