using System;
using System.ComponentModel;

namespace Ui
{
    /// <summary>
    /// Логика взаимодействия для CheckBox.xaml
    /// </summary>
    public partial class CheckBox : System.Windows.Controls.CheckBox, INotifyPropertyChanged
    {
        public CheckBox()
        {
            InitializeComponent();
        }

        protected System.Windows.Media.Geometry _GlyphData;
        public System.Windows.Media.Geometry GlyphData
        {
            get
            {
                return _GlyphData;
            }
            set
            {
                _GlyphData = value;
                NotifyPropertyChanged(nameof(GlyphData));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void NotifyPropertiesChanged(String[] propertyNames)
        {
            if (PropertyChanged != null)
            {
                foreach (var prop in propertyNames)
                    PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }
    }
}
