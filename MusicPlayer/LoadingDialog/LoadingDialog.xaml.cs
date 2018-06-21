using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace Musegician.LoadingDialog
{
    public partial class LoadingDialog : Window
    {
        static FileManager FileMan => FileManager.Instance;

        public LoadingDialog()
        {
            InitializeComponent();
        }

        public void SetFileCount(int count)
        {
            ProgressBar.Maximum = count;
            ProgressBar.IsIndeterminate = false;
        }

        public void SetProgress(int value)
        {
            ProgressBar.Value = value;
        }

        public void SetText(string text)
        {
            DialogText.Content = text;
        }
        
        public static async Task PushMusegicianTags()
        {
            LoadingDialog loadingDiag = new LoadingDialog();
            Task work = loadingDiag.PushTags();
            loadingDiag.ShowDialog();
            await work;
        }

        public static async Task AddDirectoryLoading(string directory)
        {
            LoadingDialog loadingDiag = new LoadingDialog();
            Task work = loadingDiag.LoadFiles(directory);
            loadingDiag.ShowDialog();
            await work;
        }

        async Task LoadFiles(string directory)
        {
            IProgress<string> textSetter = new Progress<string>(value => SetText(value));
            IProgress<int> limitSetter = new Progress<int>(value => SetFileCount(value));
            IProgress<int> progressSetter = new Progress<int>(value => SetProgress(value));

            await Task.Run(() => {
                FileMan.AddDirectoryToLibrary(directory, textSetter, limitSetter, progressSetter);
            });

            Close();
        }

        async Task PushTags()
        {
            IProgress<string> textSetter = new Progress<string>(value => SetText(value));
            IProgress<int> limitSetter = new Progress<int>(value => SetFileCount(value));
            IProgress<int> progressSetter = new Progress<int>(value => SetProgress(value));

            await Task.Run(() => FileMan.PushMusegicianTagsToFiles(textSetter, limitSetter, progressSetter));

            Close();
        }
    }
}
