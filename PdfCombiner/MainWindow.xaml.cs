using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Syncfusion.Windows.PdfViewer;
using Syncfusion.Windows.Shared;

namespace PdfCombiner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ChromelessWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void HideVerticalToolbar(PdfViewerControl pdfViewer)
        {
            // Hides the thumbnail icon. 
            pdfViewer.ThumbnailSettings.IsVisible = false;

            // Hides the bookmark icon. 
            pdfViewer.IsBookmarkEnabled = false;

            // Hides the layer icon. 
            pdfViewer.EnableLayers = false;

            // Hides the organize page icon. 
            pdfViewer.PageOrganizerSettings.IsIconVisible = false;

            // Hides the redaction icon. 
            pdfViewer.EnableRedactionTool = false;

            // Hides the form icon. 
            pdfViewer.FormSettings.IsIconVisible = false;
        }

        private void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
        {
            HideVerticalToolbar((PdfViewerControl)sender);
        }
    }
}
