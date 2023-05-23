using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using FileClientUI;
using Microsoft.Win32;

namespace Laboratory8
{
    public class ApplicationViewModel : ObservableObject
    {
        private ICommand? changePageCommand;

        private IPageViewModel? currentPageViewModel;
        private List<IPageViewModel>? pageViewModels;
        private ICommand? loadDb;
        private readonly ObservableCollection<UserDataGridModel> dataTables;

        public ApplicationViewModel()
        {
            dataTables = new ObservableCollection<UserDataGridModel>();
            PageViewModels.Add(new UserDataGridViewModel(dataTables));
            PageViewModels.Add(new UserDataDiagramViewModel(dataTables));
            CurrentPageViewModel = PageViewModels[0];
        }
        

        public ICommand LoadDb
        {
            get { return loadDb ??= new RelayCommand((param) => { LoadDbFromFile(); }); }
        }

        public ICommand ChangePageCommand
        {
            get
            {
                return changePageCommand ??= new RelayCommand(
                    p => ChangeViewModel((IPageViewModel)(p ?? throw new NullReferenceException(nameof(p)))),
                    p => p is IPageViewModel);
            }
        }

        public List<IPageViewModel> PageViewModels
        {
            get { return pageViewModels ??= new List<IPageViewModel>(); }
        }

        public IPageViewModel? CurrentPageViewModel
        {
            get => currentPageViewModel;
            set
            {
                if (currentPageViewModel != value)
                {
                    currentPageViewModel = value;
                    OnPropertyChanged("CurrentPageViewModel");
                }
            }
        }

        private void LoadDbFromFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "CSV files|*.csv";
            if (openFileDialog.ShowDialog() ?? false)
            {
                Stream[] fileStreams = openFileDialog.OpenFiles();
                string[] fileNames = openFileDialog.FileNames;
                for (int i = 0; i < fileNames.Length    ; i++)
                {
                    string parseRes = UserDataGridModel.Parse(fileStreams[i], out DataTable newDataTable);
                    if (parseRes == "")
                    {
                        dataTables.Add(new UserDataGridModel(newDataTable, Path.GetFileNameWithoutExtension(fileNames[i])));
                    }
                    else
                    {
                        MessageBox.Show(parseRes);
                    }
                }
            }
        }

        private void ChangeViewModel(IPageViewModel viewModel)
        {
            if (!PageViewModels.Contains(viewModel))
                PageViewModels.Add(viewModel);
            CurrentPageViewModel = PageViewModels
                .FirstOrDefault(vm => vm == viewModel);
        }
    }
}