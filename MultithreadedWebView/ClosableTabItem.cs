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
    public class ClosableTabItem : TabItem
    {
        static ClosableTabItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ClosableTabItem), new FrameworkPropertyMetadata(typeof(ClosableTabItem)));
        }

        public static readonly RoutedEvent CloseTabItemEvent =
            EventManager.RegisterRoutedEvent("ClosedTab", RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(ClosableTabItem));

        public event RoutedEventHandler ClosedTab
        {
            add { AddHandler(CloseTabItemEvent, value); }
            remove { RemoveHandler(CloseTabItemEvent, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Button closeTabButton = this.GetTemplateChild("PART_Close") as Button;

            if (closeTabButton != null) {
                closeTabButton.Click += new System.Windows.RoutedEventHandler(CloseButton_Click);
            }
        }

        private void CloseButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(CloseTabItemEvent));
        }
    }
}
