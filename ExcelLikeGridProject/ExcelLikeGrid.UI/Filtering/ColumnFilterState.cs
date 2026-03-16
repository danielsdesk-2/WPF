using System.Collections.Generic;

namespace ExcelLikeGrid.UI.Filtering
{
    public class ColumnFilterState
    {
        public string ColumnKey { get; set; } = "";
        public HashSet<string> SelectedValues { get; set; } = new();

        public bool HasFilter => SelectedValues.Count > 0;
    }
}