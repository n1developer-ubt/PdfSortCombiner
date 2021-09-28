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
using Ghostscript.NET;
using Ghostscript.NET.Rasterizer;
using ImageMagick;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.Win32;
using PostSharp.Patterns.Diagnostics;
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
        private bool isTest = false;
        public MainWindowViewModel()
        {
            FilesToMix = 18;
            //DirectoryInfo d = new DirectoryInfo(@"C:\Users\Admin\Desktop\pddd\IMAGE");
            //d.GetFiles("*.jpg").ToList().ForEach(f => SelectedFiles.Add(f.FullName));
            //isTest = true;

            InitCommand();
        }

        public async Task StartExport(string output)
        {
            var spacing = 10;
            var width = 18 * 72;
            var height = 12 * 72;

            var iw = width / 6.0 - spacing;
            var ih = height / 3.0 - spacing;

            var margin = 0.45f;

            float ySpacing = spacing / 2.0f - 0.2f, xSpacing = spacing / 2.0f - margin;

            await Task.Run(() =>
            {
                MainWindow.Main.Dispatcher.Invoke(() =>
                {
                    AllowAll = false;
                    ProgressBarVisibility = Visibility.Visible;
                    Progress = 0;
                });

                var pgSize = new Rectangle(width, height);
                var doc = new Document(pgSize, 0, 0, 0, 0);

                if (File.Exists(output))
                    File.Delete(output);

                PdfWriter.GetInstance(doc, new FileStream(output, FileMode.OpenOrCreate));

                doc.Open();

                int x = 0, y = 1, count = 0;

                SelectedFiles.ToList().ForEach(f =>
                {
                    var fileName = isTest ? f : Path.Combine(Path.GetTempPath(), Path.GetTempFileName() + ".png");

                    if (!isTest)
                        ExportFile(f, fileName);


                    var di = Image.GetInstance(fileName);
                    di.ScaleAbsolute((float)ih, (float)iw);

                    var xx = x * iw + xSpacing;

                    di.SetAbsolutePosition((float)xx, (float)(doc.Top - (ih * y) - ySpacing));

                    di.Rotation = RotateLeft ? 1.5708f : -1.5708f;

                    count++;

                    x++;
                    xSpacing += spacing;
                    if (count % 6 == 0)
                    {
                        x = 0;
                        xSpacing = spacing / 2.0f - margin;
                        y++;
                        ySpacing += spacing;
                    }

                    doc.Add(di);

                    Progress++;
                });

                doc.Close();

                OutputFile = output;
                AllowAll = true;
                ProgressBarVisibility = Visibility.Collapsed;
                MessageBox.Show("File Exported!", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        private void ExportFile(string pdf, string output)
        {
            var settings = new MagickReadSettings();
            // Settings the density to 300 dpi will create an image with a better quality
            settings.Density = new Density(500, 500);

            using (var images = new MagickImageCollection())
            {
                // Add all the pages of the pdf file to the collection
                images.Read(pdf, settings);

                var page = 1;
                foreach (var image in images)
                {
                    // Write page to file that contains the page number
                    image.Write(output);
                    page++;
                    break;
                }
            }
        }

        #region Props

        public bool RotateLeft { get; set; } = true;
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
                var fs = new System.Windows.Forms.OpenFileDialog
                {
                    //Filter = "PDF Files (*.pdf)|*.pdf;",
                    Multiselect = true
                };

                if (fs.ShowDialog() == DialogResult.OK)
                {
                    SelectedFiles.Clear();
                    fs.FileNames.ToList().ForEach(SelectedFiles.Add);
                }
            });

            ExportCommand = new DelegateCommand(async o =>
            {
                if (SelectedFiles.Count != 18)
                {
                    MessageBox.Show("Please Select 18 Files!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (isTest)
                {
                    await StartExport("export.pdf");
                }
                else
                {
                    using (var sf = new SaveFileDialog())
                    {
                        sf.Filter = "PDF Files (*.pdf)|*.pdf;";
                        if (sf.ShowDialog() == DialogResult.OK)
                        {
                            await StartExport(sf.FileName);
                        }
                    }
                }

                return;
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
