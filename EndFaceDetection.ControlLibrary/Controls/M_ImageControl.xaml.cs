using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using EndFaceDetection.Common.Converters;
using EndFaceDetection.Services;

namespace EndFaceDetection.ControlLibrary.Controls
{
    /// <summary>
    /// M_ImageControl.xaml 的交互逻辑
    /// </summary>
    public partial class M_ImageControl : UserControl
    {

        public static readonly DependencyProperty MOrignImageProperty = DependencyProperty.Register(
        "MOrignImage",
        typeof(BitmapImage),
        typeof(M_ImageControl),
        new PropertyMetadata(null)
    );

        public BitmapImage MOrignImage
        {
            get { return (BitmapImage)GetValue(MOrignImageProperty); }
            set { SetValue(MOrignImageProperty, value); }
        }

        public static readonly DependencyProperty MResultImageProperty = DependencyProperty.Register(
        "MResultImage",
        typeof(BitmapImage),
        typeof(M_ImageControl),
        new PropertyMetadata(null)
    );

        public BitmapImage MResultImage
        {
            get { return (BitmapImage)GetValue(MResultImageProperty); }
            set { SetValue(MResultImageProperty, value); }
        }

        public M_ImageControl()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (MOrignImage == null)
            {
                MessageBox.Show("没有原图像信息！");
                return;
            }
            var showOrignImage = new ShowOrignalImageWindow(MOrignImage);
            showOrignImage.Show();
        }
    }
}
