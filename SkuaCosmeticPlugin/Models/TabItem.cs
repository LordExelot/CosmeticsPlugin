using System.Windows;

namespace Skua_CosmeticPlugin.Models
{
    public class TabItem
    {
        public TabItem(string header, FrameworkElement content)
        {
            Header = header;
            Content = content;
        }

        public string Header { get; }
        public FrameworkElement Content { get; }
    }
}
