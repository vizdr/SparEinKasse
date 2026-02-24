using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace WpfApplication1
{
    /// <summary>
    /// Modal dialog for selecting additional timespans relative to the current date range.
    /// The current timespan is always included as the first entry; this dialog lets the user
    /// pick how many and in which direction additional same-length spans are added.
    /// </summary>
    public partial class WindowTimespanSelector : Window
    {
        private readonly DateTime _currentBegin;
        private readonly DateTime _currentEnd;
        private readonly TimeSpan _duration;

        /// <summary>
        /// List of (begin, end) tuples after dialog confirmation.
        /// The current timespan is always element [0].
        /// </summary>
        public List<(DateTime Begin, DateTime End)> SelectedTimespans { get; private set; }

        public WindowTimespanSelector(DateTime currentBegin, DateTime currentEnd)
        {
            InitializeComponent();
            _currentBegin = currentBegin;
            _currentEnd   = currentEnd;
            _duration     = currentEnd - currentBegin;

            // Pre-fill custom start date with the day after the current span ends
            datePickerCustomStart.SelectedDate = currentEnd.AddDays(1);

            InitializeResources();
            RuntimeLocalization.Instance.PropertyChanged += OnLocalizationChanged;
            Closed += (s, e) => RuntimeLocalization.Instance.PropertyChanged -= OnLocalizationChanged;
        }

        private void OnLocalizationChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new System.Action(InitializeResources));
        }

        private void InitializeResources()
        {
            Title                        = RuntimeLocalization.Instance["TimespanSelectorTitle"];
            textBlockPresetHeader.Text   = RuntimeLocalization.Instance["TimespanSelectorPresetHeader"];
            buttonPlus2.Content          = RuntimeLocalization.Instance["TimespanSelectorPlus2"];
            buttonMinus2.Content         = RuntimeLocalization.Instance["TimespanSelectorMinus2"];
            buttonPlus4.Content          = RuntimeLocalization.Instance["TimespanSelectorPlus4"];
            buttonMinus4.Content         = RuntimeLocalization.Instance["TimespanSelectorMinus4"];
            textBlockCustomHeader.Text   = RuntimeLocalization.Instance["TimespanSelectorCustomHeader"];
            labelStartDate.Content       = RuntimeLocalization.Instance["TimespanSelectorStartDate"];
            labelDirection.Content       = RuntimeLocalization.Instance["TimespanSelectorDirection"];
            radioForward.Content         = RuntimeLocalization.Instance["TimespanSelectorForward"];
            radioBackward.Content        = RuntimeLocalization.Instance["TimespanSelectorBackward"];
            labelAdditionalSpans.Content = RuntimeLocalization.Instance["TimespanSelectorAdditionalSpans"];
            buttonOk.Content             = RuntimeLocalization.Instance["TimespanSelectorOk"];
        }

        private void PresetClick(object sender, RoutedEventArgs e)
        {
            int count = int.Parse(((Button)sender).Tag.ToString());
            BuildTimespans(Math.Abs(count), count > 0);
            DialogResult = true;
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            if (datePickerCustomStart.SelectedDate == null)
            {
                MessageBox.Show(
                    RuntimeLocalization.Instance["TimespanSelectorSelectDate"],
                    RuntimeLocalization.Instance["TimespanSelectorCustomTitle"],
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int count = comboCount.SelectedIndex + 1; // 1–4
            bool forward = radioForward.IsChecked == true;
            BuildTimespans(count, forward, datePickerCustomStart.SelectedDate.Value);
            DialogResult = true;
        }

        /// <summary>
        /// Builds SelectedTimespans: current span first, then <paramref name="count"/> additional
        /// spans of the same duration either forward or backward.
        /// </summary>
        private void BuildTimespans(int count, bool forward, DateTime? customStart = null)
        {
            var result = new List<(DateTime, DateTime)> { (_currentBegin, _currentEnd) };

            if (customStart.HasValue)
            {
                // Custom: additional spans start at customStart, stepping by _duration
                var start = customStart.Value;
                for (int i = 0; i < count; i++)
                {
                    DateTime b = forward
                        ? start + TimeSpan.FromTicks(_duration.Ticks * i)
                        : start - TimeSpan.FromTicks(_duration.Ticks * (i + 1));
                    DateTime en = b + _duration;
                    result.Add((b, en));
                }
            }
            else
            {
                // Preset: spans consecutive to the current span
                for (int i = 1; i <= count; i++)
                {
                    DateTime b = forward
                        ? _currentEnd.AddDays(1) + TimeSpan.FromTicks(_duration.Ticks * (i - 1))
                        : _currentBegin - TimeSpan.FromTicks(_duration.Ticks * i);
                    DateTime en = b + _duration;
                    result.Add((b, en));
                }
            }

            SelectedTimespans = result;
        }
    }
}
