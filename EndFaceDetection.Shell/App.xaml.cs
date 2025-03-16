using EndFaceDetection.Services;
using EndFaceDetection.Shell.Models;
using EndFaceDetection.Shell.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using System.Data;
using System.Windows;

namespace EndFaceDetection.Shell
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static App _app = (App)App.Current;
        IServiceProvider? ServiceProvider;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // 设置系统配置
            ConfigModel.SetDefalutSettings();

            var serviceCollection = new ServiceCollection();

            //添加单例服务
            serviceCollection.AddSingleton<CameraService>();
            serviceCollection.AddSingleton<DetectionService>();
            serviceCollection.AddSingleton<PLCService>();
            serviceCollection.AddTransient<WoodService>();

            ServiceProvider = serviceCollection.BuildServiceProvider();
            LogInView  logInView = new LogInView();
            logInView.Show();
        }
        public CameraService CameraService { get => ServiceProvider.GetService<CameraService>(); }
        public DetectionService DetectionService { get => ServiceProvider.GetService<DetectionService>(); }
        public PLCService PLCService { get => ServiceProvider.GetService<PLCService>(); }
        public WoodService WoodService { get => ServiceProvider.GetService<WoodService>(); }
    }

}
