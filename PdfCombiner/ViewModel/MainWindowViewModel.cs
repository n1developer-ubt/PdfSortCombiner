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
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.Win32;
using PostSharp.Patterns.Model;
using Syncfusion.Windows.PdfViewer;
using ZFile;
using DelegateCommand = Syncfusion.Windows.Shared.DelegateCommand;
using MessageBox = System.Windows.MessageBox;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;

namespace PdfCombiner.ViewModel
{
    [NotifyPropertyChanged]
    public class MainWindowViewModel
    {
        public MainWindowViewModel()
        {
            InitCommand();
        }

        #region Props

        public string FolderName { get; set; }
        public int FilesToMix { get; set; } = 5;
        public ObservableCollection<string> SelectedFiles { get; set; } = new ObservableCollection<string>();
        public string OutputFile { get; set; }

        public int Progress { get; set; } = 0;
        public bool AllowAll { get; set; } = true;

        public Visibility ProgressBarVisibility { get; set; } = Visibility.Hidden;

        #endregion

        #region Commands

        public ICommand SelectFileCommand { get; set; }
        public ICommand ExportCommand { get; set; }

        private void InitCommand()
        {
            SelectFileCommand = new DelegateCommand(o =>
            {
                var fs = new FolderSelectDialog();

                if (fs.ShowDialog())
                {
                    FolderName = fs.FileName;
                }
            });

            ExportCommand = new DelegateCommand(async o =>
            {
                if (Directory.Exists(FolderName))
                {
                    using (var s = new SaveFileDialog())
                    {
                        s.Filter = "Pdf Document (*.pdf)|*.pdf;";

                        if (s.ShowDialog() == DialogResult.OK)
                        {
                            ProgressBarVisibility = Visibility.Visible;
                            var files = new DirectoryInfo(FolderName).GetFiles("*.pdf").ToList().OrderBy(a => new Guid());

                            foreach (var fileInfo in files.Take(5))
                            {
                                SelectedFiles.Add(fileInfo.FullName);
                            }

                            await Task.Run(() =>
                            {
                                AllowAll = false;
                                CombineMultiplePdFs(SelectedFiles.ToArray(), s.FileName);
                                AllowAll = true;
                                OutputFile = s.FileName;
                                MessageBox.Show("File Exported", "Message", MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                            });
                        }
                    }
                }
                else MessageBox.Show("Path not exist!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
