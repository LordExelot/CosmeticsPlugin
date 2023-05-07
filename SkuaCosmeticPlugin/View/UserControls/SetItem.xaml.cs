using Newtonsoft.Json;
using Skua.Core.Models.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Skua_CosmeticPlugin.View.UserControls
{
    /// <summary>
    /// Interaction logic for SetItem.xaml
    /// </summary>
    public partial class SetItem : UserControl
    {
        public SetItem()
        {
            InitializeComponent();
            DataContext = this;
        }

        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("category")]
        private string CategoryString { get; set; }
        private ItemCategory? _category = null;
        [JsonIgnore]
        public ItemCategory Category
        {
            get
            {
                return _category ??= Enum.TryParse(CategoryString, true, out ItemCategory result) ? result : ItemCategory.Unknown;
            }
        }
        //[JsonProperty("offHand")]
        public bool? offHand;

        //public SetItem(SWF item, bool? OffHand = null)
        //{
        //    ID = item.ID;
        //    Name = item.Name;
        //    Path = item.Path;
        //    Link = item.Link;
        //    CategoryString = item.CategoryString;
        //    _category = item.Category;
        //    offHand = OffHand;
        //}
    }
}
