using System.Collections.ObjectModel;
using Avalonia.Media;
using MediainfoProjectNg.Next.Converters;
using MediainfoProjectNg.Next.Core.Presentation;
using MediainfoProjectNg.Next.Domain.Models;
using MediainfoProjectNg.Next.Presentation;

namespace MediainfoProjectNg.Next.ViewModels;

public sealed class TechnicalWindowViewModel : ViewModelBase
{
    public TechnicalWindowViewModel(MediaFileInfo info)
    {
        Title = info.GeneralInfo.Filename;
        Findings = new ObservableCollection<FindingRowViewModel>(
            info.Findings.Select(f => new FindingRowViewModel(f)));
        // Legacy: only the filename root TreeViewItem is IsExpanded=True; children stay collapsed.
        var root = PropertyTreeBuilder.Build(info);
        root.IsExpanded = true;
        TreeRoots = new ObservableCollection<TreeNode> { root };
    }

    public string Title { get; }
    public ObservableCollection<FindingRowViewModel> Findings { get; }
    public ObservableCollection<TreeNode> TreeRoots { get; }
}

public sealed class FindingRowViewModel
{
    public FindingRowViewModel(ValidationFinding finding)
    {
        Level = finding.Level.ToString();
        Description = finding.Description;
        var token = LegacyColorRules.TokenForFinding(finding);
        BackgroundBrush = token == ColorToken.None
            ? Brushes.White
            : ColorTokenToBrushConverter.TokenToBrush(token);
    }

    public string Level { get; }
    public string Description { get; }
    public IBrush BackgroundBrush { get; }
}

/// <summary>
/// Branch node (children) or leaf (Key + Value) — matches legacy KeyChildren / KeyValue.
/// </summary>
public sealed class TreeNode : ViewModelBase
{
    private bool _isExpanded;

    private TreeNode(string key, string? value, bool isLeaf)
    {
        Key = key;
        Value = value;
        IsLeaf = isLeaf;
        Children = new ObservableCollection<TreeNode>();
    }

    /// <summary>Branch node (legacy KeyChildren). Children start collapsed.</summary>
    public TreeNode(string key)
    {
        Key = key;
        Value = null;
        IsLeaf = false;
        Children = new ObservableCollection<TreeNode>();
    }

    public static TreeNode Leaf(string key, string value) => new(key, value, isLeaf: true);

    public string Key { get; }
    public string? Value { get; }
    public bool IsLeaf { get; }
    public bool IsBranch => !IsLeaf;
    public ObservableCollection<TreeNode> Children { get; }

    /// <summary>
    /// Bound to TreeViewItem.IsExpanded. Only the filename root is true initially (OG parity).
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
            {
                return;
            }

            _isExpanded = value;
            OnPropertyChanged();
        }
    }
}
