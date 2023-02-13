using System;
using System.Windows.Controls;

namespace SEReader.Utils
{
    internal static class UIHelper
    {
        public delegate void ComboBoxItemChanged<T>(T value);
        public static void InitComboBox<T>(ComboBox cmb, T selected, ComboBoxItemChanged<T> changedAction)
        {
            foreach (var item in Enum.GetNames(typeof(T)))
            {
                cmb.Items.Add(item);
            }

            cmb.SelectedItem = selected.ToString();
            cmb.SelectionChanged += (s, e) => changedAction((T)Enum.Parse(typeof(T), (string)cmb.SelectedItem));
        }
    }
}
