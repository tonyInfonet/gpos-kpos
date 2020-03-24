using InfonetPOS.Core;
using InfonetPOS.Core.Resources;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace InfonetPOS.WPF.MarkupExtensions
{
    /// <summary>
    /// Helper class to allow directly translate a string using the resource files
    // The 'Extension' suffix can be omitted when using in XAML
    // ex: {ext: Translate Login}
    /// </summary>
    [ContentProperty("Text")]
    public class TranslateExtension : MarkupExtension
    {
        public string Culture { get; set; }
        public string Key { get; set; }

        public TranslateExtension(string key)
        {
            Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (Key == null)
            {
                return null;
            }

            var culture = Culture == null ? App.Culture : new CultureInfo(Culture);
            var translation = AppResources.ResourceManager.GetString(Key, culture);

#if DEBUG
            if (translation == null)
            {
                throw new ArgumentException(string.Format("Key {0} was not found for culture {1}.", Key, App.Culture));
            }
#endif
            return translation;
        }
    }
}
