using System.Globalization;
using System.Windows.Data;

namespace AppGestionDeVM.Converters
{
    public class EstadoEncendibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? estado = value?.ToString()?.ToLowerInvariant();
            return estado is "apagada" or "encendida";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
