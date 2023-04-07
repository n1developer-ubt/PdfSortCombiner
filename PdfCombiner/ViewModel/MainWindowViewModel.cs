using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using Ghostscript.NET;
using Ghostscript.NET.Rasterizer;
using ImageMagick;
using iTextSharp.text;
using iTextSharp.text.pdf; 
using PostSharp.Patterns.Model;
using Syncfusion.Windows.PdfViewer;
using ZFile;
using DelegateCommand = Syncfusion.Windows.Shared.DelegateCommand;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;

namespace PdfCombiner.ViewModel
{
    [NotifyPropertyChanged]
    public class MainWindowViewModel
    {
        private List<string> _files = new List<string>();
        public MainWindowViewModel()
        { 
            InitCommand();
        }

        public async Task StartExport(string output)
        {  
            await Task.Run(() =>
            {
                MainWindow.Main.Dispatcher.Invoke(() =>
                {
                    AllowAll = false;
                    ProgressBarVisibility = Visibility.Visible;
                    Progress = 0;
                });
                  
                if (File.Exists(output))
                    File.Delete(output);

                CombineMultiplePdFs(_files.ToArray(), output);
                
                OutputFile = null;
                OutputFile = output;
                AllowAll = true;
                ProgressBarVisibility = Visibility.Collapsed;
                MessageBox.Show("File Exported!", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }
        

        #region Props

        public string SelectedFolder { get; set; }
        public string OutputFile { get; set; }

        public int Progress { get; set; } = 0;
        public bool AllowAll { get; set; } = true;

        public Visibility ProgressBarVisibility { get; set; } = Visibility.Hidden;

        #endregion

        #region Commands

        public ICommand SelectDirectoryCommand { get; set; }
        public ICommand ExportCommand { get; set; }

        private void InitCommand()
        {
            SelectDirectoryCommand = new DelegateCommand(o =>
            {
                var fs = new FolderBrowserDialog();

                if (fs.ShowDialog() == DialogResult.OK)
                {
                    SelectedFolder = fs.SelectedPath;
                    var di = new DirectoryInfo(fs.SelectedPath);
                    var files = di.GetFiles("*.pdf");
                    foreach (var fileInfo in files.OrderBy(f=>f.Name))
                    {
                        _files.Add(fileInfo.FullName);
                    }
                }
            });

            ExportCommand = new DelegateCommand(async o =>
            {
                if (_files.Count == 0)
                {
                    MessageBox.Show("No files found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (var sf = new SaveFileDialog())
                {
                    sf.Filter = "PDF Files (*.pdf)|*.pdf;";
                    if (sf.ShowDialog() == DialogResult.OK)
                    {
                        await StartExport(sf.FileName);
                    }
                }
            });
        }

        #endregion

        public void CombineMultiplePdFs(string[] fileNames, string outFile)
        {
            // step 1: creation of a document-object
            Document document = new Document();
            //create newFileStream object which will be disposed at the end
            using (FileStream newFileStream = new FileStream(outFile, FileMode.Create))
            {
                // step 2: we create a writer that listens to the document
                PdfCopy writer = new PdfCopy(document, newFileStream);
                if (writer == null)
                {
                    return;
                }


                // step 3: we open the document
                document.Open();


                foreach (string fileName in fileNames)
                {
                    // we create a reader for a certain document
                    PdfReader reader = new PdfReader(fileName);
                    reader.ConsolidateNamedDestinations();

                    // step 4: we add content
                    for (int i = 1; i <= reader.NumberOfPages; i++)
                    {
                        PdfImportedPage page = writer.GetImportedPage(reader, i);
                        writer.AddPage(page);
                    }

                    PRAcroForm form = reader.AcroForm;
                    if (form != null)
                    {
                        writer.AddDocument(reader);
                    }

                    reader.Close();
                    Progress++;
                }

                // step 5: we close the document and writer
                writer.Close();
                document.Close();
            }//disposes the newFileStream object
        }
    }
}
