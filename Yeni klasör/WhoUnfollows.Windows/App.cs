using System;
using Microsoft.MobileBlazorBindings.WebView.Windows;
using Xamarin.Forms;
using Xamarin.Forms.Platform.WPF;
using Application = System.Windows.Application;

namespace WhoUnfollows.Windows
{
    public class MainWindow : FormsApplicationPage
    {
        public MainWindow()
        {
            Forms.Init();
            BlazorHybridWindows.Init();
            LoadApplication(new App());
        }

        [STAThread]
        public static void Main()
        {
            var app = new Application();
            app.Run(new MainWindow());
        }
    }
}