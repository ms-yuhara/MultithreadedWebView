using System.Windows;
using System.Windows.Controls;

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
