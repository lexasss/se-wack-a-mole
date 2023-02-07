﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace SEReader.Logging
{
    [Flags]
    public enum LogSource
    {
        Tracker = 1,
        Experiment = 2,
    }

    public enum SavingResult
    {
        Save,
        Discard,
    }

    public abstract class Logger<T> where T : class
    {
        public SavingResult SaveTo(string defaultFileName, string greeting = "")
        {
            var filename = defaultFileName;
            var result = PromptToSave(ref filename, greeting);
            if (result == SavingResult.Save)
            {
                result = Save(filename, _records, Header) ? SavingResult.Save : SavingResult.Discard;
            }

            _records.Clear();

            return result;
        }

        public void Clear()
        {
            _records.Clear();
        }


        // Internal

        protected readonly List<T> _records = new();

        protected abstract string Header { get; }

        string _folder;


        protected Logger()
        {
            _folder = Properties.Settings.Default.DataFolder;
            if (string.IsNullOrEmpty(_folder))
            {
                _folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
        }

        protected SavingResult PromptToSave(ref string filename, string greeting = "")
        {
            if (!string.IsNullOrEmpty(greeting))
            {
                greeting += "\n";
            }

            var answer = MessageBox.Show(
                $"{greeting}Save data into\n'{_folder}\\{filename}'?\n\n" +
                $"Press 'No' to change the name or folder\n" +
                $"Press 'Cancel' to abandon data",
                Application.Current.MainWindow.Title + " - Logger",
                MessageBoxButton.YesNoCancel, 
                MessageBoxImage.Question);

            if (answer == MessageBoxResult.Cancel)
            {
                return SavingResult.Discard;
            }
            else if (answer == MessageBoxResult.No)
            {
                filename = AskFileName(filename);
                return string.IsNullOrEmpty(filename) ? SavingResult.Discard : SavingResult.Save;
            }
            else
            {
                filename = Path.Combine(_folder, filename);
                return SavingResult.Save;
            }
        }

        protected string AskFileName(string defaultFileName)
        {
            var savePicker = new Microsoft.Win32.SaveFileDialog()
            {
                DefaultExt = "txt",
                FileName = defaultFileName,
                Filter = "Log files (*.txt)|*.txt"
            };

            if (savePicker.ShowDialog() ?? false)
            {
                _folder = Path.GetDirectoryName(savePicker.FileName);
                Properties.Settings.Default.DataFolder = _folder;
                Properties.Settings.Default.Save();

                return savePicker.FileName;
            }

            return null;
        }

        protected bool Save(string filename, IEnumerable<object> records, string header = "")
        {
            if (string.IsNullOrEmpty(filename))
            {
                return false;
            }

            var folder = Path.GetDirectoryName(filename);

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            using (StreamWriter writer = File.CreateText(filename))
            {
                try
                {
                    if (!string.IsNullOrEmpty(header))
                    {
                        writer.WriteLine(header);
                    }

                    writer.WriteLine(string.Join("\n", records));

                    MessageBox.Show(
                        $"Data saved into\n'{filename}'",
                        Application.Current.MainWindow.Title + " - Logger",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return true;
                }
                catch (Exception ex)
                {
                    var answer = MessageBox.Show(
                        $"Failed to save data into\n'{filename}':\n\n{ex.Message}\n\n" +
                        "Retry?",
                        Application.Current.MainWindow.Title + " - Logger",
                        MessageBoxButton.YesNo);
                    if (answer == MessageBoxResult.Yes)
                    {
                        return Save(AskFileName(filename), records, header);
                    }
                }
            }

            return false;
        }
    }
}
