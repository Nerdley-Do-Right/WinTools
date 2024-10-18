using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WUApiLib;
using WinTools.ViewModels;
using System.Diagnostics;

namespace WinTools.Views
{
    public sealed partial class GeneralPage : Page
    {
        public GeneralViewModel ViewModel
        {
            get;
        }

        public GeneralPage()
        {
            ViewModel = App.GetService<GeneralViewModel>();
            InitializeComponent();
        }

        private void update_Click(object sender, RoutedEventArgs e)
        {
            ShowMessageBox("Checking for updates...");
            CheckAndUpdateWindows();
        }

        private Task CheckAndUpdateWindows()
        {
            return Task.Run(() =>
            {
                try
                {
                    // Initialize COM library
                    Type t = Type.GetTypeFromProgID("Microsoft.Update.Session", false);
                    dynamic updateSession = Activator.CreateInstance(t);

                    // Create a new update session
                    dynamic updateSearcher = updateSession.CreateUpdateSearcher();
                    var searchResult = updateSearcher.Search("IsInstalled=0 AND IsHidden=0");

                    // Search for updates
                    IUpdateCollection updates = searchResult.Updates;
                    if (updates.Count > 0)
                    {
                        string result = "Updates found:";
                        foreach (IUpdate update in updates)
                        {
                            result += $"\nTitle: {update.Title}";
                            result += $"\nDescription: {update.Description}";
                        }

                        // Download updates
                        IUpdateDownloader downloader = updateSession.CreateUpdateDownloader();
                        downloader.Updates = (UpdateCollection)updates;
                        IDownloadResult downloadResult = downloader.Download();

                        if (downloadResult.ResultCode == OperationResultCode.orcSucceeded)
                        {
                            // Install updates
                            IUpdateInstaller installer = updateSession.CreateUpdateInstaller();
                            installer.Updates = (UpdateCollection)updates;
                            IInstallationResult installResult = installer.Install();

                            if (installResult.ResultCode == OperationResultCode.orcSucceeded)
                            {
                                result += "\nUpdates installed successfully.";
                            }
                            else
                            {
                                result += "\nFailed to install updates.";
                            }
                        }
                        else
                        {
                            result += "\nFailed to download updates.";
                        }

                        DispatcherQueue.TryEnqueue(() => ShowMessageBox(result));
                    }
                    else
                    {
                        DispatcherQueue.TryEnqueue(() => ShowMessageBox("No updates found."));
                    }
                }
                catch (Exception ex)
                {
                    DispatcherQueue.TryEnqueue(() => ShowMessageBox($"An error occurred: {ex.Message}"));
                }
            });
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public extern static bool DuplicateToken(IntPtr ExistingTokenHandle, int SECURITY_IMPERSONATION_LEVEL, out IntPtr DuplicateTokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool ImpersonateLoggedOnUser(IntPtr hToken);

        const int TOKEN_DUPLICATE = 0x0002;
        const int TOKEN_QUERY = 0x0008;
        const int TOKEN_IMPERSONATE = 0x0004;
        const int SecurityImpersonation = 2;



        private void RunAsSystemOrTrustedInstaller(string command)
        {
            IntPtr currentProcessToken = IntPtr.Zero;
            IntPtr duplicateToken = IntPtr.Zero;

            try
            {
                // Get the current process token
                if (!OpenProcessToken(GetCurrentProcess(), TOKEN_DUPLICATE | TOKEN_QUERY | TOKEN_IMPERSONATE, out currentProcessToken))
                {
                    ShowMessageBox("Failed to open process token.");
                    return;
                }

                // Duplicate the token
                if (!DuplicateToken(currentProcessToken, SecurityImpersonation, out duplicateToken))
                {
                    ShowMessageBox("Failed to duplicate token.");
                    return;
                }

                // Impersonate the duplicated token
                if (!ImpersonateLoggedOnUser(duplicateToken))
                {
                    ShowMessageBox("Failed to impersonate the token.");
                    return;
                }

                // Start the process with the duplicated token
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    UseShellExecute = false
                };

                Process.Start(startInfo);
                ShowMessageBox("Process started with elevated privileges.");
            }
            catch (Exception ex)
            {
                ShowMessageBox($"An error occurred: {ex.Message}");
            }
            finally
            {
                if (currentProcessToken != IntPtr.Zero)
                {
                    Marshal.Release(currentProcessToken);
                }
                if (duplicateToken != IntPtr.Zero)
                {
                    Marshal.Release(duplicateToken);
                }
            }
        }

        private void ShowMessageBox(string message)
        {
            ContentDialog messageDialog = new ContentDialog
            {
                Title = "Update Check",
                Content = message,
                CloseButtonText = "OK"
                
            };
            messageDialog.XamlRoot = this.XamlRoot;
            _ = messageDialog.ShowAsync();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string command = CommandTextBox.Text;
            string executionLevel = (ExecutionLevelComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (string.IsNullOrEmpty(command) || string.IsNullOrEmpty(executionLevel))
            {
                ShowMessageBox("Please enter a command and select an execution level.");
                return;
            }

            try
            {
                //ExecuteCommand(command, executionLevel);
                RunAsSystemOrTrustedInstaller(command);
                ShowMessageBox("Command executed successfully.");
            }
            catch (Exception ex)
            {
                ShowMessageBox($"An error occurred: {ex.Message}");
            }
        }
    }
}
