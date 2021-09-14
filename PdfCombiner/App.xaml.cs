using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PdfCombiner
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("NDg5ODA0QDMxMzkyZTMyMmUzMFp0aTR6N244RjlOL2R2ZitiK2JNa05Gb2JIVDV4MGFPam5yWjY3QVQ2YlE9");
        }
    }
}
