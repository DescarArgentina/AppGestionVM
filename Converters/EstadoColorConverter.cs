using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AppGestionDeVM.Converters
{
    public class EstadoColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string estado = value?.ToString()?.ToLowerInvariant() ?? "";

            return estado switch
            {
                "encendida" or "running" or "poweredon" or "on" or "iniciada" or "activa"
                    => new SolidColorBrush(Color.FromRgb(46, 125, 50)),

                "apagada" or "off" or "poweredoff" or "stopped" or "detenida" or "inactiva"
                    => new SolidColorBrush(Color.FromRgb(183, 28, 28)),

                "pendiente" or "pending" or "iniciando" or "starting" or
                "apagando" or "stopping" or "suspendida" or "suspended" or "reiniciando"
                    => new SolidColorBrush(Color.FromRgb(230, 126, 34)),

                _ => new SolidColorBrush(Color.FromRgb(80, 80, 80))
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
