using System.Windows;
using MahApps.Metro.Controls;

namespace SpellEditor.Sources.Controls.Common
{
    // base template to hide up/down buttons
    public abstract class TextOnlyNumericUpDownBase : NumericUpDown
    {
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("PART_NumericUp") is UIElement up)
                up.Visibility = Visibility.Collapsed;

            if (GetTemplateChild("PART_NumericDown") is UIElement down)
                down.Visibility = Visibility.Collapsed;
        }
    }

    public sealed class UIntTextBox : TextOnlyNumericUpDownBase
    {
        public uint UIntValue
        {
            get => (uint)(Value ?? 0);
            set => Value = value;
        }

        public UIntTextBox()
        {
            // Set defaults here
            Minimum = 0;
            Maximum = uint.MaxValue;    // 4294967295
            Value = 0;
            Interval = 1;               // optional: no stepping
            NumericInputMode = NumericInput.Numbers; // no decimals
        }
    }

    public sealed class IntTextBox : TextOnlyNumericUpDownBase
    {
        public int IntValue
        {
            get => (int)(Value ?? 0);
            set => Value = value;
        }

        public IntTextBox()
        {
            Minimum = int.MinValue;
            Maximum = int.MaxValue;
            Value = 0;
            Interval = 1;
            NumericInputMode = NumericInput.Numbers;
        }
    }

    public sealed class FloatTextBox : TextOnlyNumericUpDownBase
    {
        public float FloatValue
        {
            get => (float)(Value ?? 0.0);
            set => Value = value;
        }

        public FloatTextBox()
        {
            Minimum = float.MinValue;
            Maximum = float.MaxValue;
            Value = 0.0;
            Interval = 0.1;
            NumericInputMode = NumericInput.Decimal; // or ALL to also allow non float
        }
    }
}
