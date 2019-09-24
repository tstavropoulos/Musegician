using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Musegician.LoadingDialog
{
    public partial class LoadingDialog : Window
    {
        #region Progress Setters

        private readonly LoadingUpdater updater;

        #endregion Progress Setters
        #region Constructor

        public LoadingDialog()
        {
            InitializeComponent();

            updater = new LoadingUpdater(this);
        }

        #endregion Constructor
        #region Public Factory Methods

        /// <summary>
        /// Creates and displays the indefinite loading progress bar, while running the workMethod asynchronously.
        /// Only returns when the work task is finished, and does not lock down UI thread.
        /// </summary>
        public static void VoidBuilder(
            Action workMethod,
            string title)
        {
            LoadingDialog dialog = new LoadingDialog();

            dialog.SetTitle(title);

            //Execute the work asynchronously
            Task.Run(() =>
            {
                workMethod();
                //Dispatch call to appropriate UI Thread to close the modal dialog and resume execution
                dialog.updater.CloseDialog();
            });

            //Display window as a Modal Dialog
            //Execution pauses on this call until the dialog is closed
            dialog.ShowDialog();
        }

        /// <summary>
        /// Creates and displays the loading progress bar, while running the workMethod asynchronously.
        /// Creates Progress objects and passes them into the work method.
        /// Only returns when the work task is finished, and does not lock down UI thread.
        /// </summary>
        public static void VoidBuilder(
            Action<LoadingUpdater> workMethod)
        {
            LoadingDialog dialog = new LoadingDialog();
            
            //Execute the work asynchronously
            Task.Run(() =>
            {
                workMethod(dialog.updater);
                //Dispatch call to appropriate UI Thread to close the modal dialog and resume execution
                dialog.updater.CloseDialog();
            });

            //Display window as a Modal Dialog
            //Execution pauses on this call until the dialog is closed
            dialog.ShowDialog();
        }

        /// <summary>
        /// Creates and displays the loading progress bar, while running the workMethod asynchronously.
        /// Creates Progress objects and passes them into the work method.
        /// Only returns when the work task is finished, and does not lock down UI thread.
        /// </summary>
        public static void ArgBuilder<T>(
            Action<LoadingUpdater, T> workMethod,
            T workArgument = default)
        {
            LoadingDialog dialog = new LoadingDialog();

            //Execute the work asynchronously
            Task.Run(() =>
            {
                workMethod(dialog.updater, workArgument);
                //Dispatch call to appropriate UI Thread to close the modal dialog and resume execution
                dialog.updater.CloseDialog();
            });

            //Display window as a Modal Dialog
            //Execution pauses on this call until the dialog is closed
            dialog.ShowDialog();
        }

        /// <summary>
        /// Creates and displays the loading progress bar, while running the workMethod asynchronously.
        /// Creates Progress objects and passes them into the work method.
        /// Returns the result only when the work task is finished, and does not lock down UI thread.
        /// </summary>
        public static async Task<T> ReturnBuilder<T>(
            Func<LoadingUpdater, T> workMethod)
        {
            LoadingDialog dialog = new LoadingDialog();

            //Execute the work asynchronously
            Task<T> work = Task.Run(() =>
            {
                T result = workMethod(dialog.updater);
                //Dispatch call to appropriate UI Thread to close the modal dialog and resume execution
                dialog.updater.CloseDialog();
                return result;
            });

            //Display window as a Modal Dialog
            //Execution pauses on this call until the dialog is closed
            dialog.ShowDialog();

            //Return the result of the work (which will already be finished)
            return await work;
        }

        #endregion Public Factory Methods
        #region UI Update Methods

        public void SetTitle(string text) => Title = text;
        public void SetSubtitle(string text) => DialogSubtitle.Content = text;
        public void SetBarProgress(int value) => ProgressBar.Value = value;

        public void SetBarLimit(int limit)
        {
            if (limit > 0)
            {
                ProgressBar.Maximum = limit;
                ProgressBar.IsIndeterminate = false;
            }
            else
            {
                //Overloaded method to set the bar to indefinite if the argument isn't meaningful
                ProgressBar.IsIndeterminate = true;
            }
        }

        #endregion UI Update Methods
        #region LoadingUpdater InnerClass

        /// <summary>
        /// Class to construct and contain Progress objects which dispatch UI updates for LoadingDialog class
        /// </summary>
        public class LoadingUpdater
        {
            private readonly LoadingDialog dialog;

            public LoadingUpdater(LoadingDialog dialog)
            {
                this.dialog = dialog;
            }

            public void SetTitle(string title) => dialog.Dispatcher.InvokeAsync(() => dialog.SetTitle(title));
            public void SetSubtitle(string subtitle) => dialog.Dispatcher.InvokeAsync(() => dialog.SetSubtitle(subtitle));
            public void SetLimit(int limit) => dialog.Dispatcher.InvokeAsync(() => dialog.SetBarLimit(limit));
            public void SetProgress(int progress) => dialog.Dispatcher.InvokeAsync(() => dialog.SetBarProgress(progress));
            public void SetBarIndefinite() => dialog.Dispatcher.InvokeAsync(() => dialog.SetBarLimit(0));

            public void CloseDialog() => dialog.Dispatcher.InvokeAsync(dialog.Close);
        }

        #endregion LoadingUpdater InnerClass
    }
}
