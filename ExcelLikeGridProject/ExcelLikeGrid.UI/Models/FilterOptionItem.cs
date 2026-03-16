using System.ComponentModel;

namespace ExcelLikeGrid.UI.Models
{
    public class FilterOptionItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string Value { get; set; } = "";

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                    return;

                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}