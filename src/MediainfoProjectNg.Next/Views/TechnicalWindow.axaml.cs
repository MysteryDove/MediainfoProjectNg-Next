using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using MediainfoProjectNg.Next.Domain.Models;
using MediainfoProjectNg.Next.ViewModels;

namespace MediainfoProjectNg.Next.Views;

public partial class TechnicalWindow : Window
{
    public TechnicalWindow()
    {
        InitializeComponent();
    }

    public TechnicalWindow(MediaFileInfo info)
        : this()
    {
        DataContext = new TechnicalWindowViewModel(info);
    }

    private void CopyValue_OnClick(object? sender, RoutedEventArgs e)
    {
        if (GetLeafFromMenu(sender) is not { } node)
        {
            return;
        }

        CopyText(node.Value ?? string.Empty);
    }

    private void CopyKeyValue_OnClick(object? sender, RoutedEventArgs e)
    {
        if (GetLeafFromMenu(sender) is not { } node)
        {
            return;
        }

        CopyText($"{node.Key}: {node.Value}");
    }

    private void CopyText(string text)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            return;
        }

        _ = clipboard.SetTextAsync(text);
    }

    private static TreeNode? GetLeafFromMenu(object? sender)
    {
        if (sender is not MenuItem menu)
        {
            return null;
        }

        var ctx = menu.FindAncestorOfType<ContextMenu>() ?? menu.Parent as ContextMenu;
        TreeNode? node = null;
        if (ctx?.PlacementTarget is Control target)
        {
            // PlacementTarget is the StackPanel (leaf) or a child TextBlock.
            node = target.DataContext as TreeNode
                   ?? (target.Parent as Control)?.DataContext as TreeNode;
        }

        node ??= menu.DataContext as TreeNode;
        return node is { IsLeaf: true } ? node : null;
    }
}
