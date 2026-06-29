using OpenTK.Graphics.ES11;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
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


/* JBR notas
   Botón de reset o desplegable con parámetros por defeto
   Situar siempre y con la misma escala el eje de coordenadas en una esquina
   Seleccionar proyecccion en perspectiva o isométrica.
   Mostrar plano horizontal para ver como se desplaza en Y (T)
 
Hecho:
   Meter transparencia de la concha
 */

/*******************************************************************************
* @note All the applicatoin 3D objects are implemented using the "object3D" class. The
* 3d objects in the application are:
*  - Shell model mesh
*  - Shell axis
*  - Shell spiral
*  - ZYX axis

* Geometric Analysis of Shell Coiling: General Problems
*******************************************************************************/


namespace RaupCoilerII {

    /*******************************************************************************
    * @brief Converter used to transform string values in some controls to float
    * values in the ViewModel and viceversa.
    *******************************************************************************/
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

    /*******************************************************************************
    * @brief Converter used to transform string values in some controls to double
    * values in the ViewModel and viceversa.
    *******************************************************************************/
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

    /*******************************************************************************
    * @brief Converter used to transform brush values in some controls to color
    * values in the ViewModel and viceversa.
    *******************************************************************************/
    public class colorToBrushConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is Color color)
                return new SolidColorBrush(color);

            return null;
        }//Convert

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is SolidColorBrush brush)
                return brush.Color;

            return null;
        }//ConvertBack

    }//colorToBrushConverter

    /*******************************************************************************
    * @brief Converter used to transform double values in some controls to int
    * values in the ViewModel and viceversa.
    *******************************************************************************/
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

            // create the ViewModel data structure and set it into the user interface data context
            appViewModel = new ViewModel();
            this.DataContext = appViewModel;

            InitializeOpenTK();

            InitializeOpenGL();

            ZYXaxis = new cZYXaxis(appViewModel);
            ZYXaxis.InitializeGeometry();
            ZYXaxis.TransformUpdateVAO();

            ShellModel = new cShellModel(appViewModel);
            ShellModel.InitializeGeometry();
            ShellModel.TransformUpdateVAO();

            UnitContainerCube = new cUnityCube(appViewModel);
            UnitContainerCube.InitializeGeometry();
            UnitContainerCube.TransformUpdateVAO();

            // force "international" culture
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("EN-US");

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

        private bool isDragging = false;// flat that inidcates if the user is pressing the mouse button or not
        private Point lastMousePosition;// used to calculate the displecement of the mouse while the user presses the mouse button
        // private double dTotalPercentMoveX = 0;// the percentage of the control width that the user has moved pressing the mouse button 
        // private double dTotalPercentMoveY = 0;// the percentage of the control height that the user has moved pressing the mouse button 

        /*******************************************************************************
        * @brief delegate that manages the event that occurs when the user presses the 
        * mouse left button over the Open GL control
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void OpenTkControl_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {

            if (e.ClickCount == 2) {

                // double click
                if (rigthPanel.Width.Value > 10) {
                    // hide the tools panel
                    rigthPanel.Width = new GridLength(5);
                } else {
                    // show the tools panel
                    rigthPanel.Width = ViewModel.rightPanelWidth;
                }

            } else {

                // single click
                isDragging = true;
                lastMousePosition = e.GetPosition(OpenTkControl);
                OpenTkControl.CaptureMouse();

            }// if (e.ClickCount == 2)


        }//OpenTkControl_MouseLeftButtonDown

        /*******************************************************************************
        * @brief delegate that manages the event that occurs when the user releases the 
        * mouse left button over the Open GL control
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void OpenTkControl_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {

            isDragging = false;
            OpenTkControl.ReleaseMouseCapture();

        }//OpenTkControl_MouseLeftButtonUp

        /*******************************************************************************
        * @brief delegate that manages the event that occurs when the user moves the mouse
        * over the Open GL control.
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void OpenTkControl_MouseMove(object sender, MouseEventArgs e) {
            double dWidth = OpenTkControl.ActualWidth;
            double dHeight = OpenTkControl.ActualHeight;
            double dYPercentVar = 0;
            double dXPercentVar = 0;
            double deltaX = 0;
            double deltaY = 0;

            // only update rotation angles if the user is pressing the mouse button
            if (!isDragging)
                return;

            Point currentPosition = e.GetPosition(OpenTkControl);

            // calculate the mouse position variation since the previous function call
            deltaX = currentPosition.X - lastMousePosition.X;
            deltaY = currentPosition.Y - lastMousePosition.Y;

            // calculate the mouse potition variation in percentaje of the control width and height
            dYPercentVar = deltaY / dHeight;
            dXPercentVar = deltaX / dWidth;

            // apply the percentaje of mouse position variation to acumulate a proportional variatione in the 
            // ViewPhiZenithAngle and ViewThetaAzimuthAngle angles
            appViewModel.ViewPhiZenithAngle = appViewModel.ViewPhiZenithAngle + (dYPercentVar * 360);
            appViewModel.ViewThetaAzimuthAngle = appViewModel.ViewThetaAzimuthAngle + (dXPercentVar * 360);

            lastMousePosition = currentPosition;

        }//OpenTkControl_MouseMove

        /*******************************************************************************
        * @brief delegate that manages the event that occurs when the user moves the mouse
        * wheel over the Open GL control.
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void OpenTkControl_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e) {
            int delta = e.Delta;
            double dMaxVariation;
            double dVariation;
            double dWheelClickVariation = 3;

            // a delta value of 120 corresponds to a single mouse wheel click, so 240 means to clicks. 
            // other advanced mouse models have greater ressolution and the click value may be lower
            // than 120, like 100, 80, 60...

            // each mouse whell click will be translated to a dWheelClickVariation percentaje of the
            // distance range between the maximum ViewDistanceMax and minimum ViewDistanceMin
            dMaxVariation = (appViewModel.ViewDistanceMax - appViewModel.ViewDistanceMin);
            dVariation = dMaxVariation * ((double)delta * dWheelClickVariation / (double)(120.0f * 100.0f));
            appViewModel.ViewRoDistance = appViewModel.ViewRoDistance - dVariation;

        }//OpenTkControl_MouseWheel

        /*******************************************************************************
        * @brief delegate that manages the event that occurs when the user clicks over
        * the mesh color selecction button.
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void btnMeshColor_Click(object sender, RoutedEventArgs e) {
                var dialog = new ColorPickerWindow(appViewModel.MeshColor);
                dialog.Owner = this;

                bool? resultado = dialog.ShowDialog();

                if (resultado == true && dialog.SelectedColor.HasValue) {

                    Color color = dialog.SelectedColor.Value;
                    appViewModel.MeshColor = color;

            }//if

        }//btnMeshColor_Click

        /*******************************************************************************
        * @brief delegate that manages the event that occurs when the user clicks over
        * the mesh spiral color selecction button.
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void btnSpiralColor_Click(object sender, RoutedEventArgs e) {
            var dialog = new ColorPickerWindow(appViewModel.SpiralColor);
            dialog.Owner = this;

            bool? resultado = dialog.ShowDialog();

            if (resultado == true && dialog.SelectedColor.HasValue) {

                Color color = dialog.SelectedColor.Value;
                appViewModel.SpiralColor = color;

            }//if

        }//btnSpiralColor_Click

        /*******************************************************************************
        * @brief delegate that manages the event that occurs when the user clicks over
        * the axis color selecction button.
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void btnAxisColor_Click(object sender, RoutedEventArgs e) {
            var dialog = new ColorPickerWindow(appViewModel.AxisColor);
            dialog.Owner = this;

            bool? resultado = dialog.ShowDialog();

            if (resultado == true && dialog.SelectedColor.HasValue) {

                Color color = dialog.SelectedColor.Value;
                appViewModel.AxisColor = color;

            }//if

        }//btnAxisColor_Click

        /*******************************************************************************
        * @brief delegate that manages the event that occurs when the user clicks over
        * the background color selecction button.
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void btnBackColor_Click(object sender, RoutedEventArgs e) {
            var dialog = new ColorPickerWindow(appViewModel.BackgroundColor);
            dialog.Owner = this;

            bool? resultado = dialog.ShowDialog();

            if (resultado == true && dialog.SelectedColor.HasValue) {

                Color color = dialog.SelectedColor.Value;

                // Aquí usas el color
                appViewModel.BackgroundColor = color;

            }//if

        }//btnBackColor_Click               

        /*******************************************************************************
        * @brief delegate that manages the event that occurs when the user clicks over
        * the reset view to X axis
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void btnResetViewOrientationToX_Click(object sender, RoutedEventArgs e) {

            // set all the view vector parameters to its default value
            appViewModel.ViewPhiZenithAngle = 0;
            appViewModel.ViewThetaAzimuthAngle = 0;
            appViewModel.ViewRoDistance = 4;

        }//btnResetViewOrientationToX_Click

        /*******************************************************************************
        * @brief delegate that manages the event that occurs when the user clicks over
        * the reset view to Y axis
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void btnResetViewOrientationToY_Click(object sender, RoutedEventArgs e) {

            // set all the view vector parameters to its default value
            appViewModel.ViewPhiZenithAngle = 90;
            appViewModel.ViewThetaAzimuthAngle = 0;
            appViewModel.ViewRoDistance = 4;

        }//btnResetViewOrientationToY_Click

        /*******************************************************************************
        * @brief delegate that manages the event that occurs when the user clicks over
        * the reset view to Z axis
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void btnResetViewOrientationToZ_Click(object sender, RoutedEventArgs e) {

            // set all the view vector parameters to its default value
            appViewModel.ViewPhiZenithAngle = 0;
            appViewModel.ViewThetaAzimuthAngle = 90;
            appViewModel.ViewRoDistance = 4;

        }//btnResetViewOrientationToZ_Click

        /*******************************************************************************
        * @brief delegate that manages the event that occurs when the user clicks over
        * the reset light vector parameters
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void btnResetLightOrientation_Click(object sender, RoutedEventArgs e) {

            // set all the view vector parameters to its default value
            appViewModel.LightPhiZenithAngle = 0;
            appViewModel.LightThetaAzimuthAngle = 0;
            appViewModel.LightRoDistance = 4;

        }//btnResetLightOrientation_Click

        /*******************************************************************************
        * @brief delegate that manages the event that occurs when the user clicks over
        * the export menu option.
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void menuExportItem_Click(object sender, RoutedEventArgs e) {

            ShellModel.export("exportado.obj");

        }//menuExportItem_Click

    }
}
