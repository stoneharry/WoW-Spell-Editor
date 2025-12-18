using System.Windows;
using MahApps.Metro.Controls;

namespace SpellEditor.Sources.Controls.Common
{
    public class TextOnlyNumericUpDown : MahApps.Metro.Controls.NumericUpDown
    {
        public TextOnlyNumericUpDown()
        {
            // Set defaults here
            Minimum = 0;
            Maximum = uint.MaxValue;    // 4294967295
            Value = 0;
            Interval = 1;               // optional: no stepping
            NumericInputMode = NumericInput.Numbers; // no decimals
        }
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Hide up button
            if (GetTemplateChild("PART_NumericUp") is UIElement upButton)
                upButton.Visibility = Visibility.Collapsed;

            // Hide down button
            if (GetTemplateChild("PART_NumericDown") is UIElement downButton)
                downButton.Visibility = Visibility.Collapsed;
        }
    }
}
