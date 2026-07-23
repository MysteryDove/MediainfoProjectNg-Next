using System.Collections;
using System.Reflection;
using MediainfoProjectNg.Next.Domain.Models;
using MediainfoProjectNg.Next.ViewModels;

namespace MediainfoProjectNg.Next.Presentation;

/// <summary>
/// Ports legacy TechnicalWindow.GetTreeStructure reflection walk (mpng).
/// Type recursion cases match OG: GeneralInfo, MediaFileInfo, AudioInfo, ChapterInfo, ProfileInfo
/// (not VideoInfo/SubInfo as single types — those expand via IList then leaf properties).
/// </summary>
public static class PropertyTreeBuilder
{
    public static TreeNode Build(MediaFileInfo info) =>
        GetTreeStructure(info.GeneralInfo.Filename, info);

    private static TreeNode GetTreeStructure(string name, object o)
    {
        var props = o.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var root = new TreeNode(name);
        foreach (var prop in props)
        {
            if (prop.GetIndexParameters().Length > 0)
            {
                continue;
            }

            // OG FileInfo has no Findings; skip to avoid extra branch not in legacy tree.
            if (prop.Name is "Findings")
            {
                continue;
            }

            object? value;
            try
            {
                value = prop.GetValue(o);
            }
            catch
            {
                continue;
            }

            switch (value)
            {
                case GeneralInfo:
                case MediaFileInfo:
                case AudioInfo:
                case ChapterInfo:
                case ProfileInfo:
                    root.Children.Add(GetTreeStructure(prop.Name, value));
                    break;
                case IList list:
                {
                    var keyChildren = new TreeNode(prop.Name);
                    for (var i = 0; i < list.Count; i++)
                    {
                        var item = list[i];
                        if (item is not null)
                        {
                            keyChildren.Children.Add(
                                GetTreeStructure($"{item.GetType().Name}[{i}]", item));
                        }
                    }

                    root.Children.Add(keyChildren);
                    break;
                }
                default:
                    if (prop.Name == "Summary" && value is string sum)
                    {
                        var sumNode = new TreeNode(prop.Name);
                        sumNode.Children.Add(TreeNode.Leaf(string.Empty, sum));
                        root.Children.Add(sumNode);
                    }
                    else if ((prop.Name is "Duration" or "Timespan") && value is IConvertible)
                    {
                        // Prefer correct long formatting (OG only handled int Timespan).
                        var ms = Convert.ToInt64(value);
                        var ts = TimeSpan.FromMilliseconds(ms);
                        root.Children.Add(TreeNode.Leaf(prop.Name, ts.ToString(@"hh\:mm\:ss\.fff")));
                    }
                    else
                    {
                        root.Children.Add(TreeNode.Leaf(prop.Name, value?.ToString() ?? string.Empty));
                    }

                    break;
            }
        }

        return root;
    }
}
