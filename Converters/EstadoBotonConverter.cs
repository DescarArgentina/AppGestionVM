using System.Globalization;
using System.Windows.Data;

namespace AppGestionDeVM.Converters
{
    public class EstadoBotonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString()?.ToLowerInvariant() switch
            {
                "encendida" => "Apagar",
                "apagada" => "Encender",
                _ => "..."
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
