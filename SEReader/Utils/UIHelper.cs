using System;
using System.Windows.Controls;

namespace SEReader.Utils
{
    internal static class UIHelper
    {
        public delegate void ItemChanged<T>(T value);

        public static void InitComboBox<T>(ComboBox cmb, T selected, ItemChanged<T> changedAction)
        {
            if (typeof(T).IsEnum)
            {
                foreach (var item in Enum.GetNames(typeof(T)))
                {
                    cmb.Items.Add(item);
                }
            }

            cmb.SelectedItem = selected.ToString();
            cmb.SelectionChanged += (s, e) => changedAction(typeof(T).IsEnum ? 
                (T)Enum.Parse(typeof(T), (string)cmb.SelectedItem) :
                (T)cmb.SelectedItem);
        }

        public static void InitCheckBox(CheckBox chk, bool isChecked, ItemChanged<bool> changedAction)
        {
            chk.IsChecked = isChecked;
            chk.Checked += (s, e) => changedAction(chk.IsChecked ?? false);
            chk.Unchecked += (s, e) => changedAction(chk.IsChecked ?? false);
        }

        public static void InitTextBox<T>(TextBox txb, T value, ItemChanged<T> changedAction)
        {
            txb.Text = value.ToString();
            txb.TextChanged += (s, e) =>
            {
                if (typeof(T) == typeof(int))
                {
                    if (int.TryParse(txb.Text, out int value))
                        changedAction((T)(value as object));
                }
                else if (typeof(T) == typeof(double))
                {
                    if (double.TryParse(txb.Text, out double value))
                        changedAction((T)(value as object));
                }
                else if (typeof(T) == typeof(string))
                {
                    changedAction((T)(txb.Text as object));
                }
                else
                {
                    throw new Exception("UIHelper.InitTextBox: unsupported type");
                }
            };
        }
    }
}
