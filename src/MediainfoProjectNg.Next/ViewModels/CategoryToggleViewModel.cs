using CommunityToolkit.Mvvm.ComponentModel;
using MediainfoProjectNg.Next.Core.Presentation;

namespace MediainfoProjectNg.Next.ViewModels;

public partial class CategoryToggleViewModel : ObservableObject
{
    public CategoryToggleViewModel(IssueCategory category, string label, ColorToken swatchToken)
    {
        Category = category;
        Label = label;
        SwatchToken = swatchToken;
    }

    public IssueCategory Category { get; }
    public string Label { get; }
    public string AccessibleName => ButtonText;
    public ColorToken SwatchToken { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ButtonText))]
    [NotifyPropertyChangedFor(nameof(AccessibleName))]
    public partial int Count { get; set; }

    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    public string ButtonText => $"{Label} ({Count})";
}
