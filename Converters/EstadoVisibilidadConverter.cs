using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AppGestionDeVM.Converters
{
    public class EstadoVisibilidadConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool encendida = value is string estado && estado == "Encendida";
            if (targetType == typeof(bool)) return encendida;
            return encendida ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
