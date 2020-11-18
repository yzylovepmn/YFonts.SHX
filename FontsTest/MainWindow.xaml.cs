using Microsoft.Win32;
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
using YFonts.SHX;

namespace FontsTest
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private ShxFile _shxFile;

        private void _OnClicked(object sender, RoutedEventArgs e)
        {
            _ShowOpenIgesFileDialog();
        }

        private void _ShowOpenIgesFileDialog()
        {
            var dialog = new OpenFileDialog() { Filter = string.Format("{0}|*.{1};", "shx File", "shx") };
            if (dialog.ShowDialog() == true)
                _shxFile = ShxFile.Load(dialog.FileName);
        }

        private void _OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_shxFile == null) return;
            var fontSize = 24.0;
            var data = _shxFile.GetGraphicData(TB_Box.Text, fontSize, fontSize / 1.5, fontSize / 10, fontSize / 10).Select(ts => ts.Offset(new Vector(0, (double)_shxFile.FontFile.BaseDown / _shxFile.FontFile.BaseUp * fontSize)));
            TV_Visual.TextShapes = data;
        }
    }
}