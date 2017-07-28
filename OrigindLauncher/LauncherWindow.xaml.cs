﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using OrigindLauncher.Resources.Client;
using OrigindLauncher.Resources.Configs;
using OrigindLauncher.Resources.Server;
using OrigindLauncher.UI;
using OrigindLauncher.UI.Code;
using OrigindLauncher.UI.Dialogs;

namespace OrigindLauncher
{
    /// <summary>
    ///     Interaction logic for LauncherWindow.xaml
    /// </summary>
    public partial class LauncherWindow
    {
        private static readonly Regex CheckUrlRegex =
            new Regex(
                "((http|ftp|https):\\/\\/)?[\\w\\-_]+(\\.[\\w\\-_]+)+([\\w\\-\\.,@?^=%&amp;:/~\\+#]*[\\w\\-\\@?^=%&amp;/~\\+#])?");

        public LauncherWindow()
        {
            InitializeComponent();
            WelcomeMessage.Text += " " + Config.Instance.PlayerAccount.Username;
            TitleTextBlock.Text += " " + Config.LauncherVersion +
                                   (Config.Admins.Any(u => u == Config.Instance.PlayerAccount.Username)
                                       ? " Admin"
                                       : "");
            try
            {
                var result1 = ServerInfoGetter.GetServerInfoAsync();
                result1.Wait();
                var result = result1.Result;
                ServerMessage.Text += " " + result.players.online;
                ServerMessage.ToolTip = string.Join("\r\n", result.players.sample.Select(p => p.name));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async void StartButton_OnClick(object sender, RoutedEventArgs e)
        {//TODO: Check for Java and Install r
            StartButton.IsEnabled = false;
            if (CheckUrlRegex.IsMatch(Config.Instance.UpdatePath))
                await UpdateUpdatePathAsync();

            // 刷新登录状态
            var status = await Task.Run(() => Config.Instance.PlayerAccount.UpdateLoginStatus());
            if (status != LoginStatus.Successful)
            {
                MainSnackbar.MessageQueue.Enqueue("登录状态刷新失败.");
                StartButton.IsEnabled = true;
                return;
            }

            // 检测更新状态
            bool updateStatus;
            try
            {
                updateStatus = ClientManager.CheckUpdate();
            }
            catch (Exception exception)
            {
                MainSnackbar.MessageQueue.Enqueue("更新检测失败." + exception);
                StartButton.IsEnabled = true;
                return;
            }

            // 等待更新
            if (updateStatus)
            {
                var result = await UpdateClientAsync();
                if (!result)
                {
                    MainSnackbar.MessageQueue.Enqueue("更新失败.");
                    StartButton.IsEnabled = true;
                    return;
                }
            }

            // 启动游戏
            var gm = new GameManager();
            gm.OnError += result => Dispatcher.Invoke(() => MainSnackbar.MessageQueue.Enqueue(result.Exception));
            gm.OnGameExit += (handle, i) => Environment.Exit(0);

            var lpm = new LaunchProgressManager();
            gm.OnGameLog += (lh, log) => lpm.OnGameLog(log);

            // 游戏状态
            var lh1 = gm.Run();

            if (File.Exists(ClientManager.GetGameStorageDirectory(@"mods\OrigindLauncherHelper.jar")))
            {
                if (Config.Instance.LaunchProgress)
                    lpm.Begin(lh1);
            }
            else
            {
                await DialogHost.Show(new MessageDialog {Message = {Text = "你的客户端没有安装 OrigindLauncherHelper."}},
                    "RootDialog");
            }


            // 退出
            StartButton.IsEnabled = true;
            this.Flyout(() =>
            {
                Hide();
                //TODO
            });
        }

        private static async Task UpdateUpdatePathAsync()
        {
            while (!CheckUrlRegex.IsMatch(Config.Instance.UpdatePath))
            {
                var input = new InputDialog {Title = {Text = "输入客户端更新地址."}};
                await DialogHost.Show(input, "RootDialog");
                var text = input.InputBox.Text;
                if (CheckUrlRegex.IsMatch(text))
                    Config.Instance.UpdatePath = text;
                Config.Save();
            }
        }

        private async Task<bool> UpdateClientAsync()
        {
            MainSnackbar.MessageQueue.Enqueue("正在更新客户端");

            //TODO: Update Client
            await Task.Run(() =>
            {

                var dl = ClientManager.Update();

            });
            return true;
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            this.FlyoutAndClose(() => Application.Current.Shutdown());
        }

        private void OpenDMinecraft(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer",
                Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\" +
                ClientManager.GameStorageDirectory);
        }

        private async void Options(object sender, RoutedEventArgs e)
        {
            await DialogHost.Show(new SettingsDialog(), "RootDialog");
        }

        private void Move(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private async void ServerMessage_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var result = await ServerInfoGetter.GetServerInfoAsync();
                ServerMessage.Text = "服务器在线人数: " + result.players.online;
                ServerMessage.ToolTip = string.Join("\r\n", result.players.sample.Select(p => p.name));
            }
            catch (Exception e1)
            {
                Console.WriteLine(e1);
            }
        }

        private async void InitEnvironment(object sender, RoutedEventArgs e)
        {
            await UpdateUpdatePathAsync();
        }

        private void SwitchUser(object sender, RoutedEventArgs e)
        {
            Process.Start(Process.GetCurrentProcess().MainModule.FileName, "Setup");
            this.Flyout(() => Environment.Exit(0));
        }
    }
}