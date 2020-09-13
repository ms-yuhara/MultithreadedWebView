using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows;
using System.Windows.Controls;

namespace MultithreadedWebView
{
    /// <summary>
    /// Interaction logic for BrowserWindow.xaml
    /// </summary>
    public partial class BrowserWindow : Window, IDocHostUIHandler, IOleClientSite
    {
        public const int S_OK = unchecked((int)0x00000000);
        public const int S_FALSE = unchecked((int)0x00000001);
        public const int E_NOTIMPL = unchecked((int)0x80004001);
        public const int E_NOINTERFACE = unchecked((int)0x80004002);

        public bool Modern { get; set; }

        public string DocumentTitle { get; set; }

        public string Url { get; set; }

        public Microsoft.Web.WebView2.Wpf.WebView2 ModernBrowser { get; }

        public MultithreadedWebView.WebBrowserEx ClassicBrowser { get; }

        public int TabPageIndex { get; set; }

        public event EventHandler<BrowserClosedEventArgs> BrowserClosed;
        public event EventHandler<DocumentTitleChangedEventArgs> DocumentTitleChanged;
        public event EventHandler<NewWindowRequestedEventArgs> NewWindowRequested;
        public event EventHandler<EventArgs> WebViewFirstInitialized;

        public BrowserWindow(bool modern)
        {
            InitializeComponent();

            this.Modern = modern;

            if (this.Modern) {
                var host = new DockPanel();

                this.ModernBrowser = new Microsoft.Web.WebView2.Wpf.WebView2();
                this.ModernBrowser.CoreWebView2Ready += ModernBrowser_CoreWebView2Ready;
                this.ModernBrowser.Visibility = Visibility.Hidden;

                InitializeWebView2Async();

                host.Children.Add(this.ModernBrowser);
                this.AddChild(host);
            } else {
                var host = new System.Windows.Forms.Integration.WindowsFormsHost();

                this.ClassicBrowser = new MultithreadedWebView.WebBrowserEx();
                this.ClassicBrowser.Closing += ClassicBrowser_Closing;
                this.ClassicBrowser.DocumentTitleChanged += ClassicBrowser_DocumentTitleChanged;

                host.Child = this.ClassicBrowser;
                this.AddChild(host);

                var oc = this.ClassicBrowser.ActiveXBrowser.Application as IOleObject;

                if (oc != null) {
                    oc.SetClientSite(this);
                }

                this.ClassicBrowser.ActiveXBrowser.NewWindow2 += ClassicBrowser_NewWindow2;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        protected virtual void OnBrowserClosed(BrowserClosedEventArgs e)
        {
            if (this.BrowserClosed != null) {
                BrowserClosed(this, e);
            }
        }

        protected virtual void OnDocumentTitleChanged(DocumentTitleChangedEventArgs e)
        {
            if (this.DocumentTitleChanged != null) {
                DocumentTitleChanged(this, e);
            }
        }

        protected virtual void OnNewWindowRequested(NewWindowRequestedEventArgs e)
        {
            if (this.NewWindowRequested != null) {
                NewWindowRequested(this, e);
            }
        }

        private async void InitializeWebView2Async()
        {
            if (this.ModernBrowser != null) {
                await this.ModernBrowser.EnsureCoreWebView2Async();
            }
        }

        private void ModernBrowser_CoreWebView2Ready(object sender, EventArgs e)
        {
            ((Microsoft.Web.WebView2.Wpf.WebView2)sender).CoreWebView2.WindowCloseRequested += ModernBrowser_WindowCloseRequested;
            ((Microsoft.Web.WebView2.Wpf.WebView2)sender).CoreWebView2.DocumentTitleChanged += ModernBrowser_DocumentTitleChanged;
            ((Microsoft.Web.WebView2.Wpf.WebView2)sender).CoreWebView2.NewWindowRequested += ModernBrowser_NewWindowRequested;
            ((Microsoft.Web.WebView2.Wpf.WebView2)sender).Visibility = Visibility.Visible;

            if (WebViewFirstInitialized != null) {
                WebViewFirstInitialized(sender, e);
            }
        }

        private void ModernBrowser_WindowCloseRequested(object sender, object e)
        {
            OnBrowserClosed(new BrowserClosedEventArgs(this.TabPageIndex));
        }

        private void ModernBrowser_DocumentTitleChanged(object sender, object e)
        {
            try {
                if (!String.IsNullOrWhiteSpace(this.ModernBrowser.Source.ToString())) {
                    this.DocumentTitle = this.ModernBrowser.Source.ToString();
                    this.Url = this.ModernBrowser.Source.ToString();
                }

                if (!String.IsNullOrWhiteSpace(this.ModernBrowser.CoreWebView2.DocumentTitle)) {
                    this.DocumentTitle = this.ModernBrowser.CoreWebView2.DocumentTitle;
                }

                OnDocumentTitleChanged(new DocumentTitleChangedEventArgs(this.DocumentTitle, this.Url, this.TabPageIndex));
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        private void ModernBrowser_NewWindowRequested(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NewWindowRequestedEventArgs e)
        {
            NewWindowRequestedEventArgs args = new NewWindowRequestedEventArgs(this.TabPageIndex);
            Microsoft.Web.WebView2.Core.CoreWebView2Deferral deferral = e.GetDeferral();

            OnNewWindowRequested(args);

            if ((args.Handled == true) && (args.NewWindow != null)) {
                args.NewWindow.WebViewFirstInitialized += (s, ev) => {
                    try {
                        e.NewWindow = ((Microsoft.Web.WebView2.Wpf.WebView2)s).CoreWebView2;
                        e.Handled = true;
                        deferral.Complete();
                    } catch (Exception ex) {
                        MessageBox.Show(ex.ToString());
                    }
                };
            }
        }

        private void ClassicBrowser_Closing(object sender, EventArgs e)
        {
            OnBrowserClosed(new BrowserClosedEventArgs(this.TabPageIndex));
        }

        private void ClassicBrowser_DocumentTitleChanged(object sender, EventArgs e)
        {
            try {
                if ((this.ClassicBrowser.Url != null) && !String.IsNullOrWhiteSpace(this.ClassicBrowser.Url.ToString())) {
                    this.Title = this.ClassicBrowser.Url.ToString();
                    this.Url = this.ClassicBrowser.Url.ToString();
                }

                if (!String.IsNullOrWhiteSpace(this.ClassicBrowser.DocumentTitle)) {
                    this.Title = this.ClassicBrowser.DocumentTitle;
                }

                OnDocumentTitleChanged(new DocumentTitleChangedEventArgs(this.Title, this.Url, this.TabPageIndex));
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        private void ClassicBrowser_NewWindow2(ref object ppDisp, ref bool Cancel)
        {
            NewWindowRequestedEventArgs args = new NewWindowRequestedEventArgs(this.TabPageIndex);

            OnNewWindowRequested(args);

            if ((args.Handled == true) && (args.NewWindow != null)) {
                ppDisp = args.NewWindow.ClassicBrowser.ActiveXBrowser.Application;
            }
        }

        int IDocHostUIHandler.ShowContextMenu(uint dwID, ref tagPOINT ppt, object pcmdtReserved, object pdispReserved)
        {
            return S_FALSE;
        }

        int IDocHostUIHandler.GetHostInfo(ref DOCHOSTUIINFO pInfo)
        {
            pInfo.dwFlags |= (uint)(DOCHOSTUIFLAG.DOCHOSTUIFLAG_THEME);
            pInfo.dwFlags |= (uint)(DOCHOSTUIFLAG.DOCHOSTUIFLAG_DPI_AWARE);

            return S_OK;
        }

        int IDocHostUIHandler.ShowUI(int dwID, object pActiveObject, object pCommandTarget, object pFrame, object pDoc)
        {
            return S_OK;
        }

        int IDocHostUIHandler.HideUI()
        {
            return S_OK;
        }

        int IDocHostUIHandler.UpdateUI()
        {
            return S_OK;
        }

        int IDocHostUIHandler.EnableModeless(bool fEnable)
        {
            return E_NOTIMPL;
        }

        int IDocHostUIHandler.OnDocWindowActivate(bool fActivate)
        {
            return E_NOTIMPL;
        }

        int IDocHostUIHandler.OnFrameWindowActivate(bool fActivate)
        {
            return E_NOTIMPL;
        }

        int IDocHostUIHandler.ResizeBorder(ref tagRECT prcBorder, object pUIWindow, bool fFrameWindow)
        {
            return E_NOTIMPL;
        }

        int IDocHostUIHandler.TranslateAccelerator(ref tagMSG lpMsg, ref Guid pguidCmdGroup, uint nCmdID)
        {
            return E_NOTIMPL;
        }

        int IDocHostUIHandler.GetOptionKeyPath(out string pchKey, uint dw)
        {
            pchKey = null;

            return E_NOTIMPL;
        }

        int IDocHostUIHandler.GetDropTarget(IDropTarget pDropTarget, out IDropTarget ppDropTarget)
        {
            ppDropTarget = null;

            return E_NOTIMPL;
        }

        int IDocHostUIHandler.GetExternal(out object ppDispatch)
        {
            ppDispatch = null;

            return E_NOTIMPL;
        }

        int IDocHostUIHandler.TranslateUrl(uint dwTranslate, string pchURLIn, out string pstrURLOut)
        {
            pstrURLOut = null;

            return E_NOTIMPL;
        }

        int IDocHostUIHandler.FilterDataObject(System.Runtime.InteropServices.ComTypes.IDataObject pDO, out System.Runtime.InteropServices.ComTypes.IDataObject ppDORet)
        {
            ppDORet = null;

            return E_NOTIMPL;
        }

        int IOleClientSite.SaveObject()
        {
            return E_NOTIMPL;
        }

        int IOleClientSite.GetMoniker(uint dwAssign, uint dwWhichMoniker, out IMoniker ppmk)
        {
            ppmk = null;

            return E_NOTIMPL;
        }

        int IOleClientSite.GetContainer(out object ppContainer)
        {
            ppContainer = null;

            return E_NOTIMPL;
        }

        int IOleClientSite.ShowObject()
        {
            return S_OK;
        }

        int IOleClientSite.OnShowWindow(bool fShow)
        {
            return E_NOTIMPL;
        }

        int IOleClientSite.RequestNewObjectLayout()
        {
            return E_NOTIMPL;
        }
    }

    public class BrowserClosedEventArgs : EventArgs
    {
        public int TabIndex { get; set; }

        public BrowserClosedEventArgs()
        {
            TabIndex = -1;
        }

        public BrowserClosedEventArgs(int index)
        {
            TabIndex = index;
        }
    }

    public class DocumentTitleChangedEventArgs : EventArgs
    {
        public string Title { get; set; }

        public string Url { get; set; }

        public int TabIndex { get; set; }

        public DocumentTitleChangedEventArgs()
        {
            Title = "";
            Url = "";
            TabIndex = -1;
        }

        public DocumentTitleChangedEventArgs(string title, string url, int index)
        {
            Title = title;
            Url = url;
            TabIndex = index;
        }
    }

    public class NewWindowRequestedEventArgs : EventArgs
    {
        public BrowserWindow NewWindow { get; set; }

        public bool Handled { get; set; }

        public int TabIndex { get; set; }

        public NewWindowRequestedEventArgs()
        {
            NewWindow = null;
            Handled = false;
            TabIndex = -1;
        }

        public NewWindowRequestedEventArgs(int index)
        {
            NewWindow = null;
            Handled = false;
            TabIndex = index;
        }
    }

    public enum DOCHOSTUIDBLCLK
    {
        DOCHOSTUIDBLCLK_DEFAULT = 0,
        DOCHOSTUIDBLCLK_SHOWPROPERTIES = 1,
        DOCHOSTUIDBLCLK_SHOWCODE = 2
    }

    [Flags()]
    public enum DOCHOSTUIFLAG
    {
        DOCHOSTUIFLAG_DIALOG = 0x1,
        DOCHOSTUIFLAG_DISABLE_HELP_MENU = 0x2,
        DOCHOSTUIFLAG_NO3DBORDER = 0x4,
        DOCHOSTUIFLAG_SCROLL_NO = 0x8,
        DOCHOSTUIFLAG_DISABLE_SCRIPT_INACTIVE = 0x10,
        DOCHOSTUIFLAG_OPENNEWWIN = 0x20,
        DOCHOSTUIFLAG_DISABLE_OFFSCREEN = 0x40,
        DOCHOSTUIFLAG_FLAT_SCROLLBAR = 0x80,
        DOCHOSTUIFLAG_DIV_BLOCKDEFAULT = 0x100,
        DOCHOSTUIFLAG_ACTIVATE_CLIENTHIT_ONLY = 0x200,
        DOCHOSTUIFLAG_OVERRIDEBEHAVIORFACTORY = 0x400,
        DOCHOSTUIFLAG_CODEPAGELINKEDFONTS = 0x800,
        DOCHOSTUIFLAG_URL_ENCODING_DISABLE_UTF8 = 0x1000,
        DOCHOSTUIFLAG_URL_ENCODING_ENABLE_UTF8 = 0x2000,
        DOCHOSTUIFLAG_ENABLE_FORMS_AUTOCOMPLETE = 0x4000,
        DOCHOSTUIFLAG_ENABLE_INPLACE_NAVIGATION = 0x10000,
        DOCHOSTUIFLAG_IME_ENABLE_RECONVERSION = 0x20000,
        DOCHOSTUIFLAG_THEME = 0x40000,
        DOCHOSTUIFLAG_NOTHEME = 0x80000,
        DOCHOSTUIFLAG_NOPICS = 0x100000,
        DOCHOSTUIFLAG_NO3DOUTERBORDER = 0x200000,
        DOCHOSTUIFLAG_DISABLE_EDIT_NS_FIXUP = 0x400000,
        DOCHOSTUIFLAG_LOCAL_MACHINE_ACCESS_CHECK = 0x800000,
        DOCHOSTUIFLAG_DISABLE_UNTRUSTEDPROTOCOL = 0x1000000,
        DOCHOSTUIFLAG_HOST_NAVIGATES = 0x2000000,
        DOCHOSTUIFLAG_ENABLE_REDIRECT_NOTIFICATION = 0x4000000,
        DOCHOSTUIFLAG_USE_WINDOWLESS_SELECTCONTROL = 0x8000000,
        DOCHOSTUIFLAG_USE_WINDOWED_SELECTCONTROL = 0x10000000,
        DOCHOSTUIFLAG_ENABLE_ACTIVEX_INACTIVATE_MODE = 0x20000000,
        DOCHOSTUIFLAG_DPI_AWARE = 0x40000000
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct tagMSG
    {
        public IntPtr hwnd;
        public uint message;
        public uint wParam;
        public int lParam;
        public uint time;
        public tagPOINT pt;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct tagPOINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct tagRECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class COMRECT
    {
        public int left = 0;
        public int top = 0;
        public int right = 0;
        public int bottom = 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DOCHOSTUIINFO
    {
        public uint cbSize;
        public uint dwFlags;
        public uint dwDoubleClick;
        [MarshalAs(UnmanagedType.BStr)] public string pchHostCss;
        [MarshalAs(UnmanagedType.BStr)] public string pchHostNS;
    }

    [ComImport, ComVisible(true)]
    [Guid("00000122-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDropTarget
    {
        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int DragEnter(
            [In, MarshalAs(UnmanagedType.Interface)] System.Runtime.InteropServices.ComTypes.IDataObject pDataObj,
            [In, MarshalAs(UnmanagedType.U4)] uint grfKeyState,
            [In, MarshalAs(UnmanagedType.Struct)] tagPOINT pt,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref uint pdwEffect);

        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int DragLeave();

        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int DragOver(
            [In, MarshalAs(UnmanagedType.U4)] uint grfKeyState,
            [In, MarshalAs(UnmanagedType.Struct)] tagPOINT pt,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref uint pdwEffect);

        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int Drop(
            [In, MarshalAs(UnmanagedType.Interface)] System.Runtime.InteropServices.ComTypes.IDataObject pDataObj,
            [In, MarshalAs(UnmanagedType.U4)] uint grfKeyState,
            [In, MarshalAs(UnmanagedType.Struct)] tagPOINT pt,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref uint pdwEffect);
    }

    [ComImport, ComVisible(true)]
    [Guid("bd3f23c0-d43e-11cf-893b-00aa00bdce1a")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDocHostUIHandler
    {
        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int ShowContextMenu(
            [In, MarshalAs(UnmanagedType.U4)] uint dwID,
            [In, MarshalAs(UnmanagedType.Struct)] ref tagPOINT ppt,
            [In, MarshalAs(UnmanagedType.IUnknown)] object pcmdtReserved,
            [In, MarshalAs(UnmanagedType.IDispatch)] object pdispReserved);

        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int GetHostInfo([In, Out, MarshalAs(UnmanagedType.Struct)] ref DOCHOSTUIINFO pInfo);

        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int ShowUI(
            [In, MarshalAs(UnmanagedType.I4)] int dwID,
            [In, MarshalAs(UnmanagedType.Interface)] object pActiveObject,
            [In, MarshalAs(UnmanagedType.Interface)] object pCommandTarget,
            [In, MarshalAs(UnmanagedType.Interface)] object pFrame,
            [In, MarshalAs(UnmanagedType.Interface)] object pDoc);

        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int HideUI();

        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int UpdateUI();

        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int EnableModeless(
            [In, MarshalAs(UnmanagedType.Bool)] bool fEnable);

        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int OnDocWindowActivate(
            [In, MarshalAs(UnmanagedType.Bool)] bool fActivate);

        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int OnFrameWindowActivate(
            [In, MarshalAs(UnmanagedType.Bool)] bool fActivate);

        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int ResizeBorder(
            [In, MarshalAs(UnmanagedType.Struct)] ref tagRECT prcBorder,
            [In, MarshalAs(UnmanagedType.Interface)] object pUIWindow,
            [In, MarshalAs(UnmanagedType.Bool)] bool fFrameWindow);

        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int TranslateAccelerator(
            [In, MarshalAs(UnmanagedType.Struct)] ref tagMSG lpMsg,
            [In] ref Guid pguidCmdGroup,
            [In, MarshalAs(UnmanagedType.U4)] uint nCmdID);

        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int GetOptionKeyPath(
            [Out, MarshalAs(UnmanagedType.LPWStr)] out String pchKey,
            [In, MarshalAs(UnmanagedType.U4)] uint dw);

        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int GetDropTarget(
            [In, MarshalAs(UnmanagedType.Interface)] IDropTarget pDropTarget,
            [Out, MarshalAs(UnmanagedType.Interface)] out IDropTarget ppDropTarget);

        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int GetExternal(
            [Out, MarshalAs(UnmanagedType.IDispatch)] out object ppDispatch);

        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int TranslateUrl(
            [In, MarshalAs(UnmanagedType.U4)] uint dwTranslate,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pchURLIn,
            [Out, MarshalAs(UnmanagedType.LPWStr)] out string pstrURLOut);

        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int FilterDataObject(
            [In, MarshalAs(UnmanagedType.Interface)] System.Runtime.InteropServices.ComTypes.IDataObject pDO,
            [Out, MarshalAs(UnmanagedType.Interface)] out System.Runtime.InteropServices.ComTypes.IDataObject ppDORet);
    }

    [ComImport, ComVisible(true)]
    [Guid("00000118-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IOleClientSite
    {
        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int SaveObject();

        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int GetMoniker(
            [In, MarshalAs(UnmanagedType.U4)] uint dwAssign,
            [In, MarshalAs(UnmanagedType.U4)] uint dwWhichMoniker,
            [Out, MarshalAs(UnmanagedType.Interface)] out IMoniker ppmk);

        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int GetContainer(
            [Out, MarshalAs(UnmanagedType.Interface)] out object ppContainer);

        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int ShowObject();

        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int OnShowWindow([In, MarshalAs(UnmanagedType.Bool)] bool fShow);

        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int RequestNewObjectLayout();
    }

    [ComImport, ComVisible(true)]
    [Guid("00000112-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IOleObject
    {
        [PreserveSig]
        int SetClientSite(
            [In, MarshalAs(UnmanagedType.Interface)] IOleClientSite pClientSite);

        IOleClientSite GetClientSite();

        [PreserveSig]
        int SetHostNames(
            [In, MarshalAs(UnmanagedType.LPWStr)] string szContainerApp,
            [In, MarshalAs(UnmanagedType.LPWStr)] string szContainerObj);

        [PreserveSig]
        int Close(
            int dwSaveOption);

        [PreserveSig]
        int SetMoniker(
            [In, MarshalAs(UnmanagedType.U4)] int dwWhichMoniker,
            [In, MarshalAs(UnmanagedType.Interface)] object pmk);

        [PreserveSig]
        int GetMoniker(
            [In, MarshalAs(UnmanagedType.U4)] int dwAssign,
            [In, MarshalAs(UnmanagedType.U4)] int dwWhichMoniker,
            [MarshalAs(UnmanagedType.Interface)] out object ppmk);

        [PreserveSig]
        int InitFromData(
            [In, MarshalAs(UnmanagedType.Interface)] System.Windows.Forms.IDataObject pDataObject,
            int fCreation,
            [In, MarshalAs(UnmanagedType.U4)] int dwReserved);

        [PreserveSig]
        int GetClipboardData(
            [In, MarshalAs(UnmanagedType.U4)] int dwReserved,
            out System.Windows.Forms.IDataObject ppDataObject);

        [PreserveSig]
        int DoVerb(
            int iVerb,
            [In] IntPtr lpmsg,
            [In, MarshalAs(UnmanagedType.Interface)] IOleClientSite pActiveSite,
            int lindex,
            IntPtr hwndParent,
            [In] COMRECT lprcPosRect);

        [PreserveSig]
        int EnumVerbs(
            out object ppEnumOleVerb);

        [PreserveSig]
        int OleUpdate();

        [PreserveSig]
        int IsUpToDate();

        [PreserveSig]
        int GetUserClassID(
            [In, Out] ref Guid pClsid);

        [PreserveSig]
        int GetUserType(
            [In, MarshalAs(UnmanagedType.U4)] int dwFormOfType,
            [MarshalAs(UnmanagedType.LPWStr)] out string pszUserType);

        [PreserveSig]
        int SetExtent(
            [In, MarshalAs(UnmanagedType.U4)] int dwDrawAspect,
            [In] object pSizel);

        [PreserveSig]
        int GetExtent(
            [In, MarshalAs(UnmanagedType.U4)] int dwDrawAspect,
            [Out] object pSizel);

        [PreserveSig]
        int Advise(
            object pAdvSink,
            out int pdwConnection);

        [PreserveSig]
        int Unadvise(
            [In, MarshalAs(UnmanagedType.U4)] int dwConnection);

        [PreserveSig]
        int EnumAdvise(
            out object ppenumAdvise);

        [PreserveSig]
        int GetMiscStatus(
            [In, MarshalAs(UnmanagedType.U4)] uint dwAspect,
            [Out, MarshalAs(UnmanagedType.U4)] out uint pdwStatus);

        [PreserveSig]
        int SetColorScheme(
            [In] object pLogpal);
    };
}
