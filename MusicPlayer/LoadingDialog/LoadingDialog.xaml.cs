using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace Musegician.LoadingDialog
{
    public partial class LoadingDialog : Window
    {
        public LoadingDialog()
        {
            InitializeComponent();
        }

        public static async Task VoidBuilder(
            Action<IProgress<string>, IProgress<int>, IProgress<int>> callback)
        {
            LoadingDialog loadingDiag = new LoadingDialog();
            Task work = loadingDiag.VoidBuilderTask(callback);
            loadingDiag.ShowDialog();
            await work;
        }

        public static async Task<T> ReturnBuilder<T>(
            Func<IProgress<string>, IProgress<int>, IProgress<int>, T> callback)
        {
            LoadingDialog loadingDiag = new LoadingDialog();
            Task<T> work = loadingDiag.ReturnBuilderTask(callback);
            loadingDiag.ShowDialog();
            return await work;
        }

        public static async Task ArgBuilder<T>(
            Action<IProgress<string>, IProgress<int>, IProgress<int>, T> callback,
            T data = default)
        {
            LoadingDialog loadingDiag = new LoadingDialog();
            Task work = loadingDiag.ArgBuilderTask(callback, data);
            loadingDiag.ShowDialog();
            await work;
        }

        private async Task VoidBuilderTask(
            Action<IProgress<string>, IProgress<int>, IProgress<int>> callback)
        {
            IProgress<string> textSetter = new Progress<string>(value => SetText(value));
            IProgress<int> limitSetter = new Progress<int>(value => SetFileCount(value));
            IProgress<int> progressSetter = new Progress<int>(value => SetProgress(value));

            await Task.Run(() => callback(textSetter, limitSetter, progressSetter));

            Close();
        }

        private async Task<T> ReturnBuilderTask<T>(
            Func<IProgress<string>, IProgress<int>, IProgress<int>, T> callback)
        {
            IProgress<string> textSetter = new Progress<string>(value => SetText(value));
            IProgress<int> limitSetter = new Progress<int>(value => SetFileCount(value));
            IProgress<int> progressSetter = new Progress<int>(value => SetProgress(value));

            T result = await Task.Run(() => callback(textSetter, limitSetter, progressSetter));

            Close();

            return result;
        }

        private async Task ArgBuilderTask<T>(
            Action<IProgress<string>, IProgress<int>, IProgress<int>, T> callback,
            T argument)
        {
            IProgress<string> textSetter = new Progress<string>(value => SetText(value));
            IProgress<int> limitSetter = new Progress<int>(value => SetFileCount(value));
            IProgress<int> progressSetter = new Progress<int>(value => SetProgress(value));

            await Task.Run(() => callback(textSetter, limitSetter, progressSetter, argument));

            Close();
        }

        public void SetFileCount(int count)
        {
            if (count > 0)
            {
                ProgressBar.Maximum = count;
                ProgressBar.IsIndeterminate = false;
            }
            else
            {
                ProgressBar.IsIndeterminate = true;
            }
        }

        public void SetProgress(int value)
        {
            ProgressBar.Value = value;
        }

        public void SetText(string text)
        {
            DialogText.Content = text;
        }
    }
}
