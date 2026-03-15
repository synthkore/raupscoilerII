using OpenTK.Graphics.ES11;
using OpenTK.Mathematics;
using OpenTK.Wpf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RaupCoilerII {


    public class stringToFloatConverter: IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null) {
                return string.Empty;
            } else {
                string strAux = "";
                strAux = ((float)value).ToString("0.####");
                return strAux;
            }
            
        }//Convert

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (float.TryParse(value as string, NumberStyles.Float, culture, out var result))
                return result;

            return Binding.DoNothing;

        }//ConvertBack

    }//stringToFloatConverter 

    public class stringToDoubleConverter : IValueConverter {

            // @param[in] parameter is used to specify the number of decimals to place in conversion
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
                if (value is double d) {
                    int decimals = 2; // default value

                    if (parameter != null)
                        int.TryParse(parameter.ToString(), out decimals);

                    return d.ToString($"F{decimals}", culture);
                }

                return ""; ;
            }//Convert

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
                // De string a double (cuando el usuario escribe)
                if (value is string s) {
                    if (double.TryParse(s, NumberStyles.Any, culture, out double result))
                        return result;
                }

                return 0.0;
            }//ConvertBack

    }//stringToDoubleConverter


    public class doubleToIntConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {

            return System.Convert.ToInt32(value);
        
        }//Convert

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {

            return System.Convert.ToDouble(value);

        }//ConvertBack

    }//doubleToIntConverter 

    /*******************************************************************************
    * @brief Main window Form class
    *******************************************************************************/
    public partial class MainWindow : Window {

        ViewModel appViewModel;

        public MainWindow() {

            InitializeComponent();

            InitializeOpenTK();

            // force "international" culture
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("EN-US");

            // create the ViewModel data structure and set it into the user interface data context
            appViewModel = new ViewModel();
            this.DataContext = appViewModel;


        }//MainWindow

        /*******************************************************************************
        * @brief delegate that manages the event that occurs when the user float clicks over
        * the the button used to separate the right and the left grid columns ( right and left
        * panels ).
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void PanelSplitButton_Click(object sender, RoutedEventArgs e) {

            if (rigthPanel.Width.Value > 10) {
                rigthPanel.Width = new GridLength(5);
            } else {
                rigthPanel.Width = ViewModel.rightPanelWidth;
            }

        }//PanelSplitButton_Click


    }
}
