using System.Windows.Controls;

namespace LoyaltySystem.Wpf.Helpers
{
    public static class DataGridZoomHelper
    {
        private const double DefaultFontSize = 12;
        private const double MinFontSize = 8;
        private const double MaxFontSize = 24;
        private const double Step = 2;

        public static void ApplyDefault(DataGrid dataGrid, TextBlock? label = null)
        {
            SaveBaseColumnWidths(dataGrid);
            ApplyFontSize(dataGrid, DefaultFontSize, label);
        }

        public static void Increase(DataGrid dataGrid, TextBlock? label = null)
        {
            SaveBaseColumnWidths(dataGrid);

            var newFontSize = dataGrid.FontSize + Step;

            if (newFontSize > MaxFontSize)
                newFontSize = MaxFontSize;

            ApplyFontSize(dataGrid, newFontSize, label);
        }

        public static void Decrease(DataGrid dataGrid, TextBlock? label = null)
        {
            SaveBaseColumnWidths(dataGrid);

            var newFontSize = dataGrid.FontSize - Step;

            if (newFontSize < MinFontSize)
                newFontSize = MinFontSize;

            ApplyFontSize(dataGrid, newFontSize, label);
        }

        public static void Reset(DataGrid dataGrid, TextBlock? label = null)
        {
            SaveBaseColumnWidths(dataGrid);
            ApplyFontSize(dataGrid, DefaultFontSize, label);
        }

        private static void ApplyFontSize(DataGrid dataGrid, double fontSize, TextBlock? label)
        {
            dataGrid.FontSize = fontSize;

            dataGrid.RowHeight = fontSize + 16;
            dataGrid.ColumnHeaderHeight = fontSize + 20;

            ApplyColumnWidths(dataGrid, fontSize);

            if (label != null)
            {
                var percent = fontSize / DefaultFontSize * 100;
                label.Text = $"{percent:0}%";
            }
        }

        private static void SaveBaseColumnWidths(DataGrid dataGrid)
        {
            if (dataGrid.Tag is Dictionary<DataGridColumn, DataGridLength>)
                return;

            var widths = new Dictionary<DataGridColumn, DataGridLength>();

            foreach (var column in dataGrid.Columns)
            {
                widths[column] = column.Width;
            }

            dataGrid.Tag = widths;
        }

        private static void ApplyColumnWidths(DataGrid dataGrid, double fontSize)
        {
            if (dataGrid.Tag is not Dictionary<DataGridColumn, DataGridLength> widths)
                return;

            var scale = fontSize / DefaultFontSize;

            foreach (var column in dataGrid.Columns)
            {
                if (!widths.TryGetValue(column, out var baseWidth))
                    continue;

                if (baseWidth.IsStar)
                {
                    column.Width = baseWidth;
                    continue;
                }

                if (baseWidth.IsAuto || baseWidth.IsSizeToCells || baseWidth.IsSizeToHeader)
                {
                    column.Width = baseWidth;
                    continue;
                }

                var newWidth = baseWidth.Value * scale;

                column.Width = new DataGridLength(newWidth);
            }
        }
    }
}
