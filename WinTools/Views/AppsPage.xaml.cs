using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.ApplicationModel.Store.Preview.InstallControl;
using Windows.Management.Deployment;
using Windows.Storage;
using Windows.UI.Notifications;
using WinTools.ViewModels;

namespace WinTools.Views;

public sealed partial class AppsPage : Page
{
    public AppsViewModel ViewModel
    {
        get;
    }

    public AppsPage()
    {
        ViewModel = App.GetService<AppsViewModel>();
        InitializeComponent();
    }

    private async void Button_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            await InstallWingetAsync();
        }
        catch (Exception ex) 
        {
            ContentDialog err = new ContentDialog();
            err.Content = ex.Message;
            err.CloseButtonText = "Cancel";
            err.Title = "An error has occured.";
            err.XamlRoot = this.XamlRoot;
            await err.ShowAsync();
        }



    }

    private async Task InstallWingetAsync()
    {       
        string wingetInstallerUrl = "https://github.com/microsoft/winget-cli/releases/latest/download/Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle";
        string installerPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "winget.msixbundle");

        using (HttpClient client = new HttpClient())
        {
                HttpResponseMessage response = await client.GetAsync(wingetInstallerUrl);
                response.EnsureSuccessStatusCode();

                using (FileStream fileStream = new FileStream(installerPath, FileMode.Create))
                {
                    await response.Content.CopyToAsync(fileStream);
                }
            }

            PackageManager packageManager = new PackageManager();
            var res = await packageManager.AddPackageAsync(new Uri(installerPath), null, DeploymentOptions.None);
        if (res != null) 
        {
            if (res.IsRegistered)
            {
                ShowToastNotification("Success!", "Winget has been installed!");
            }
            else 
            {
                ShowToastNotification("Failure!", "WinGet failed to install!");
            }
        }
    }
    public void ShowToastNotification(string title, string content)
    {
        var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
        var toastTextElements = toastXml.GetElementsByTagName("text");
        toastTextElements[0].AppendChild(toastXml.CreateTextNode(title));
        toastTextElements[1].AppendChild(toastXml.CreateTextNode(content));

        var toast = new ToastNotification(toastXml);
        ToastNotificationManager.CreateToastNotifier().Show(toast);
    }

    private void update_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ProcessStartInfo upI = new();
        upI.Verb = "runas";
        upI.UseShellExecute = true;
        upI.FileName = "cmd.exe";
        upI.Arguments = "/c winget upgrade --all --accept-package-agreements --accept-source-agreements  --disable-interactivity --force";
        upI.CreateNoWindow = true;
        Process.Start(upI);
    }
}


