using System.Collections.ObjectModel;
using System.Windows;

namespace ExcelLikeGrid.Sandbox
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<SampleRow> Rows { get; } = new();

        public MainWindow()
        {
            InitializeComponent();

            Rows.Add(new SampleRow { Name = "Adam", Category = "A", Amount = 100, Status = "Open" });
            Rows.Add(new SampleRow { Name = "Bella", Category = "B", Amount = 250, Status = "Closed" });
            Rows.Add(new SampleRow { Name = "Chris", Category = "A", Amount = 180, Status = "Pending" });

            DataContext = this;
        }
    }
}