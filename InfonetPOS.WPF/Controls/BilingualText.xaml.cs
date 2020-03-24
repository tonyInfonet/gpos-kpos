using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using InfonetPOS.Core.Enums;
using InfonetPOS.Core.Resources;

namespace InfonetPOS.WPF.Controls
{
    /// <summary>
    /// Interaction logic for BilingualSpan.xaml
    /// </summary>
    public partial class BilingualText : StackPanel
    {
        #region Bindable Properties

        // Using DependencyProperty 

        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register(
                nameof(Foreground),
                typeof(Brush),
                typeof(BilingualText),
                new PropertyMetadata(default(Brush), (dependency, args) =>
                {
                    var instance = dependency as BilingualText;
                    SetForeGroundColor(instance, args.NewValue as Brush);
                }));

        public static readonly DependencyProperty TextResourceArgumentProperty =
            DependencyProperty.Register(
                nameof(TextResourceArgument),
                typeof(string),
                typeof(BilingualText),
                new PropertyMetadata(default(string), (dependency, args) => {
                    var instance = dependency as BilingualText;
                    AddArgumentStringWithTopAndBottomText(instance, args.NewValue as string);
                }));


        public static readonly DependencyProperty TextResourceKeyProperty =
            DependencyProperty.Register(
                nameof(TextResourceKey),
                typeof(string),
                typeof(BilingualText),
                new PropertyMetadata(default(string), (dependency, args) => {
                    var instance = dependency as BilingualText;
                    SetTopAndBottomText(instance, args.NewValue as string);
                }));


        public static readonly DependencyProperty LangProperty = DependencyProperty.Register(
            nameof(Lang),
            typeof(string),
            typeof(BilingualText),
            new PropertyMetadata(default(string), (dependency, args) =>
            {
                var instance = dependency as BilingualText;
                var langCode = args.NewValue as string;
                UpdateControl(instance, langCode);
            }));

        public static readonly DependencyProperty EnglishTextProperty = DependencyProperty.Register(
            nameof(EnglishText),
            typeof(string),
            typeof(BilingualText),
            new PropertyMetadata(default(string), (dependency, args) =>
            {
                var instance = dependency as BilingualText;
                instance.topText.Text = args.NewValue as string;
            }));

        public static readonly DependencyProperty LocalTextProperty = DependencyProperty.Register(
            nameof(LocalText),
            typeof(string),
            typeof(BilingualText),
            new PropertyMetadata(default(string), (dependency, args) =>
            {
                var instance = dependency as BilingualText;
                instance.bottomText.Text = args.NewValue as string;
            }));

        public static readonly DependencyProperty IsInlineProperty = DependencyProperty.Register(
            nameof(IsInline),
            typeof(bool),
            typeof(BilingualText),
            new PropertyMetadata(default(bool), (dependency, args) =>
            {
                var instance = dependency as BilingualText;
            }));

        public string Lang
        {
            get { return (string)GetValue(LangProperty); }
            set { SetValue(LangProperty, value); }
        }
        public string EnglishText
        {
            get { return (string)GetValue(EnglishTextProperty); }
            set { SetValue(EnglishTextProperty, value); }
        }

        public string LocalText
        {
            get { return (string)GetValue(LocalTextProperty); }
            set { SetValue(LocalTextProperty, value); }
        }

        public bool IsInline
        {
            get { return (bool)GetValue(IsInlineProperty); }
            set { SetValue(IsInlineProperty, value); }
        }

        public string TextResourceKey
        {
            get { return (string)GetValue(TextResourceKeyProperty); }
            set { SetValue(TextResourceKeyProperty, value); }
        }

        public string TextResourceArgument
        {
            get { return (string)GetValue(TextResourceArgumentProperty); }
            set { SetValue(TextResourceArgumentProperty, value); }
        }

        public Brush Foreground
        {
            get { return (Brush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }


        #endregion

        public BilingualText()
        {
            InitializeComponent();
        }

        #region methods
        private static void UpdateControl(BilingualText instance, string language)
        {
            if (language == AppLanguage.multi.ToString())
            {
                instance.topText.Visibility = Visibility.Visible;
                instance.bottomText.Visibility = Visibility.Visible;
            }
            else if (language != AppLanguage.en.ToString())
            {
                instance.topText.Visibility = Visibility.Collapsed;
                instance.bottomText.Visibility = Visibility.Visible;
            }
            else
            {
                instance.topText.Visibility = Visibility.Visible;
                instance.bottomText.Visibility = Visibility.Collapsed;
            }
        }


        private static void SetTopAndBottomText(BilingualText instance, string DataToShow)
        {
            if (DataToShow != null)
            {
                instance.EnglishText = AppResources.ResourceManager.GetString(DataToShow, new CultureInfo("en"));
                instance.LocalText = AppResources.ResourceManager.GetString(DataToShow, new CultureInfo("ar"));
            }
        }

        private static void AddArgumentStringWithTopAndBottomText(BilingualText instance, string argument)
        {
            if (argument != null)
            {
                instance.EnglishText = string.Format(instance.EnglishText, argument);
                instance.LocalText = string.Format(instance.LocalText, argument);
            }
        }

        private static void SetForeGroundColor(BilingualText instance, Brush TextColor)
        {
            instance.topText.Foreground = TextColor;
            instance.bottomText.Foreground = TextColor;
        }

        #endregion
    }
}
