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
using System.Windows.Shapes;

namespace EndFaceDetection.ControlLibrary.Controls
{
    /// <summary>
    /// ShowOrignalImage.xaml 的交互逻辑
    /// </summary>
    public partial class ShowOrignalImageWindow : Window
    {
        public ShowOrignalImageWindow(BitmapImage image)
        {
            InitializeComponent();
            TransGroup.Children.Add(new ScaleTransform());
            TransGroup.Children.Add(new TranslateTransform());
            ImgCtl.RenderTransform = TransGroup;
            this.ImgCtl.Source = image;
        }

        public TransformGroup TransGroup = new TransformGroup();


        #region 实现图片缩放
        private void Image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Control ctl = sender as Control;
            System.Windows.Point point = e.GetPosition(ctl);
            //滚轮滚动时控制 放大的倍数,没有固定的值，可以根据需要修改。
            double scale = e.Delta * 0.002;

            ZoomImage(TransGroup, point, scale);

        }

        private void ZoomImage(TransformGroup group, System.Windows.Point point, double scale)
        {
            System.Windows.Point pointToContent = group.Inverse.Transform(point);

            ScaleTransform scaleT = group.Children[0] as ScaleTransform;

            if (scaleT.ScaleX < 0.3 && scaleT.ScaleY < 0.3 && scale < 0)
            {
                return;
            }

            scaleT.ScaleX += scale;

            scaleT.ScaleY += scale;

            TranslateTransform translateT = group.Children[1] as TranslateTransform;

            translateT.X = -1 * ((pointToContent.X * scaleT.ScaleX) - point.X);

            translateT.Y = -1 * ((pointToContent.Y * scaleT.ScaleY) - point.Y);
        }

        //用于保存鼠标点击时的位置
        System.Windows.Point mousePosition = new System.Windows.Point();
        //鼠标点击时控件位置
        System.Windows.Point contentCtlPosition = new System.Windows.Point();
        private void ContentControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Control ctl = sender as Control;
            mousePosition = e.GetPosition(ctl);
            TranslateTransform tt = TransGroup.Children[1] as TranslateTransform;

            contentCtlPosition = new System.Windows.Point(tt.X, tt.Y);
        }

        private void ContentControl_MouseUp(object sender, MouseButtonEventArgs e)
        {

        }

        private void ContentControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Control ctl = sender as Control;
                System.Windows.Point desPosition = e.GetPosition(ctl);

                TranslateTransform transform = TransGroup.Children[1] as TranslateTransform;
                //控件移动位置的计算方式为：鼠标点击时位置+拖拽的长度(当前坐标-鼠标点击时的坐标).
                transform.X = contentCtlPosition.X + (desPosition.X - mousePosition.X);
                transform.Y = contentCtlPosition.Y + (desPosition.Y - mousePosition.Y);
            }
        }
        #endregion
    }
}
