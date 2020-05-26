using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadedWebView
{
    public delegate void ClosingEventHandler(object sender, EventArgs e);

    public class WebBrowserEx : System.Windows.Forms.WebBrowser
    {
        private const int WM_PARENTNOTIFY = 0x210;
        private const int WM_DESTROY = 0x2;

        public SHDocVw.WebBrowser ActiveXBrowser
        {
            get
            {
                return (SHDocVw.WebBrowser)ActiveXInstance;
            }
        }

        public event ClosingEventHandler Closing;

        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.LinkDemand, Name = "FullTrust")]
        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            switch (m.Msg) {
                case WM_PARENTNOTIFY:
                    if (!this.DesignMode) {
                        if (m.WParam.ToInt32() == WM_DESTROY) {
                            if (this.Closing != null) {
                                this.Closing(this, EventArgs.Empty);
                            }
                        }
                    }

                    DefWndProc(ref m);
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }
    }
}
