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

namespace MultithreadedWebView
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly object SyncObject = new object();

        private Dictionary<TabItem, BrowserWindow> BrowserWindows = new Dictionary<TabItem, BrowserWindow>();
        private Dictionary<DockPanel, IntPtr> BrowserPanels = new Dictionary<DockPanel, IntPtr>();

        private BrowserWindow ActiveBrowserWindow
        {
            get
            {
                try {
                    if (this.BrowserTabControl.Items.Count > 0) {
                        return this.BrowserWindows[(TabItem)this.BrowserTabControl.SelectedItem];
                    } else {
                        return null;
                    }
                } catch {
                    return null;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AddHandler(ClosableTabItem.CloseTabItemEvent, new RoutedEventHandler(CloseTab));
            CreateTabWindow(this.Modern.IsChecked.Value);
            NavigateInBrowser(this.Url.Text);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            while (this.BrowserTabControl.Items.Count > 0) {
                RemoveTabWindow((TabItem)this.BrowserTabControl.Items[0]);
            }
        }

        private void Url_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) {
                NavigateInBrowser(this.Url.Text);
            }
        }

        private void Navigate_Click(object sender, RoutedEventArgs e)
        {
            NavigateInBrowser(this.Url.Text);
        }

        private void NewTab_Click(object sender, RoutedEventArgs e)
        {
            CreateTabWindow(this.Modern.IsChecked.Value);
            NavigateInBrowser("about:blank");
        }

        private void CloseTab(object source, RoutedEventArgs args)
        {
            TabItem tabItem = args.Source as TabItem;

            if (tabItem != null) {
                RemoveTabWindow(tabItem);
            }
        }

        private TabItem CreateTabItem()
        {
            var item = new ClosableTabItem();

            item.Header = "Blank Page";
            item.Width = 180;
            item.MinWidth = 180;
            item.MaxWidth = 180;
            item.MinHeight = 25;
            item.MinWidth = 25;

            return item;
        }

        private BrowserWindow CreateBrowser(bool modern)
        {
            var window = new BrowserWindow(modern);

            window.BrowserClosed += BrowserWindow_BrowserClosed;
            window.DocumentTitleChanged += BrowserWindow_DocumentTitleChanged;
            window.NewWindowRequested += BrowserWindow_NewWindowRequested;
            window.TabPageIndex = this.BrowserTabControl.Items.Count;

            return window;
        }

        private BrowserWindow CreateTabWindow(bool modern)
        {
            lock (SyncObject) {
                var item = CreateTabItem();
                var dock = new DockPanel();
                var wfh = new System.Windows.Forms.Integration.WindowsFormsHost();
                var panel = new System.Windows.Forms.Panel();
                var handle = panel.Handle;

                dock.SizeChanged += Dock_SizeChanged;
                wfh.Child = panel;
                dock.LastChildFill = true;
                dock.Children.Add(wfh);
                item.Content = dock;

                BrowserWindow child = null;

                System.Threading.Thread thread = new System.Threading.Thread(() =>
                {
                    System.Threading.SynchronizationContext.SetSynchronizationContext(
                        new System.Windows.Threading.DispatcherSynchronizationContext(
                            System.Windows.Threading.Dispatcher.CurrentDispatcher));

                    lock (SyncObject) {
                        child = CreateBrowser(modern);
                        child.Closed += (sender, e) => System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvokeShutdown(System.Windows.Threading.DispatcherPriority.Background);
                        child.Show();

                        var hWndChild = new System.Windows.Interop.WindowInteropHelper(child).Handle;

                        NativeMethods.SetWindowAsChild(handle, hWndChild);
                        this.BrowserPanels.Add(dock, hWndChild);

                        System.Threading.Monitor.Pulse(SyncObject);
                    }

                    System.Windows.Threading.Dispatcher.Run();
                });

                thread.SetApartmentState(System.Threading.ApartmentState.STA);
                thread.IsBackground = true;
                thread.Start();
                System.Threading.Monitor.Wait(SyncObject);

                this.BrowserWindows.Add(item, child);
                this.BrowserTabControl.Items.Add(item);
                this.BrowserTabControl.SelectedItem = item;

                Activate();

                return child;
            }
        }

        private void RemoveTabWindow(TabItem tabItem)
        {
            if (tabItem != null) {
                var target = this.BrowserWindows[tabItem];

                if (target != null) {
                    target.Dispatcher.Invoke(new Action(() => {
                        target.BrowserClosed -= BrowserWindow_BrowserClosed;

                        if (target.Modern) {
                            target.ModernBrowser.Dispose();
                        } else {
                            target.ClassicBrowser.Dispose();
                        }

                        target.Close();
                    }));

                    this.BrowserPanels.Remove((DockPanel)tabItem.Content);
                    this.BrowserWindows.Remove(tabItem);
                    this.BrowserTabControl.Items.RemoveAt(target.TabPageIndex);

                    for (int i = 0; i < this.BrowserTabControl.Items.Count; i++) {
                        var tab = this.BrowserTabControl.Items[i] as TabItem;

                        if (tab != null) {
                            var window = this.BrowserWindows[tab];

                            if (window != null) {
                                window.TabPageIndex = i;
                            }
                        }
                    }
                }
            }
        }

        private void NavigateInBrowser(string url)
        {
            var window = this.ActiveBrowserWindow;

            if ((window == null) || String.IsNullOrEmpty(url)) {
                return;
            }

            window.Dispatcher.Invoke(new Action(() => {
                if (window.Modern) {
                    if (window.ModernBrowser.CoreWebView2 != null) {
                        window.ModernBrowser.CoreWebView2.Navigate(url);
                    }
                } else {
                    window.ClassicBrowser.Navigate(url);
                }
            }));
        }

        private void BrowserTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.ActiveBrowserWindow != null) {
                if (this.ActiveBrowserWindow.Modern) {
                    this.Indicator.Content = "Modern (Chromium)";
                    this.Modern.IsChecked = true;
                } else {
                    this.Indicator.Content = "Classic (Trident)";
                    this.Modern.IsChecked = false;
                }

                this.Url.Text = this.ActiveBrowserWindow.Url;
            }
        }

        private void BrowserWindow_BrowserClosed(object sender, BrowserClosedEventArgs e)
        {
            var tabItem = this.BrowserTabControl.Items[e.TabIndex] as TabItem;

            this.BrowserTabControl.Dispatcher.BeginInvoke(new Action(() => { RemoveTabWindow(tabItem); }));
        }

        private void BrowserWindow_DocumentTitleChanged(object sender, DocumentTitleChangedEventArgs e)
        {
            this.BrowserTabControl.Dispatcher.BeginInvoke(new Action(() => {
                var item = this.BrowserTabControl.Items[e.TabIndex] as TabItem;

                if (item != null) {
                    item.Header = e.Title;

                    if (sender.Equals(this.ActiveBrowserWindow)) {
                        this.Url.Text = e.Url;
                    }
                }
            }));
        }

        private void BrowserWindow_NewWindowRequested(object sender, NewWindowRequestedEventArgs e)
        {
            BrowserWindow child = null;

            this.BrowserTabControl.Dispatcher.Invoke(new Action(() => { child = CreateTabWindow(((BrowserWindow)sender).Modern); }));

            e.NewWindow = child;
            e.Handled = true;
        }

        private void Dock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var dock = (DockPanel)sender;

            NativeMethods.MoveWindow(this.BrowserPanels[dock], 0, 0, (int)dock.ActualWidth, (int)dock.ActualHeight, true);
        }

        private void DoEvents()
        {
            System.Windows.Threading.DispatcherFrame frame = new System.Windows.Threading.DispatcherFrame();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new System.Windows.Threading.DispatcherOperationCallback(ExitFrames), frame);
            System.Windows.Threading.Dispatcher.PushFrame(frame);
        }

        private object ExitFrames(object f)
        {
            ((System.Windows.Threading.DispatcherFrame)f).Continue = false;

            return null;
        }
    }
}
