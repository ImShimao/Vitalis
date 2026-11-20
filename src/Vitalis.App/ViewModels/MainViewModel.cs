using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Vitalis.Core.Navigation;

namespace Vitalis.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public ObservableCollection<PageDefinition> Pages { get; } = new(CataloguePages.Toutes);
}
