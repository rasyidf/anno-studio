
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AnnoStudio.ViewModels;

public partial class FileDocument : ObservableObject
{
    
    private string _title = "Untitled";
    private string _content = "";

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Content
    {
        get => _content;
        set => SetProperty(ref _content, value);
    }

    public bool CanClose { get; set; } = true;

    [RelayCommand(CanExecute = nameof(CanClose))]
    private void Close()
    {
        // TODO: Add logic to close the document 
    }
     
}