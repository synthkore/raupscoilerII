using Microsoft.Win32;
using OpenTK.Graphics.ES11;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Wpf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
   Se puede quitar ¿ bRequireToUpdateVAO ?
   Para calcular el máximo y minimo no hace falta calcular todas las anillas de la 1a y utlima vuelta basta con hacer 4 por vuelta a 90º cada una
   Parece que el autoscale no va bien, pues al generar un bivalvo con W=10000 contra el cubo unitario este sale fuera del cubo. Quizas sea por la resolcuion de la malla utilizada al hacer el GetScaleFactors
   Poder sobreimponer en render valores de parámetros D,W,T,S para facilitar capturas.
   Al rotar llega un momento que ya no deja rotar más, estaria bien utilizar el % modulo del angulo en lugar del valor absoluto.
   La iluminacion a veces parece que no funciona bien y quedan zonas muy oscuras ¿ hay que poner algun tipo de ilumniación global, omni ?
   Ahora el control GlWPFControl se refresca cauando el decide, pero para mejorar la fluidez del renderizado lo suyo sería que lo controlásemos nosotros desde la propia aplicacion poniendo RenderContinuously a false en GLWpfControlSettings y llamando a OpenTkControl.InvalidateVisual(); 
Hecho:
   Meter transparencia de la concha
   Situar siempre y con la misma escala el eje de coordenadas en una esquina
   Espiral
   Eje
   Seleccionar proyecccion en perspectiva o isométrica.
   Porder sobreponer Faces con Triangles / Quads o Vertexs.
   Mostrar plano horizontal para ver como se desplaza en Y (T)
   Botón de reset o desplegable con parámetros por defeto >> Lo hacemos con la función "New"
   Corregir sombreado triangulos de la cara interior
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
    * @brief Converter used to transform a bool to its inverted value.
    *******************************************************************************/
    public class InverseBoolConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value is bool b && !b;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value is bool b && !b;
        }

    }//InverseBoolConverter

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

            ZYXGizmo = new cGizmo(appViewModel);
            ZYXGizmo.InitializeGeometry();
            ZYXGizmo.UpdateVAO();

            ShellModel = new cShellModel(appViewModel);
            ShellModel.InitializeGeometry();
            ShellModel.UpdateVAO();

            UnitContainerCube = new cUnitCube(appViewModel);
            UnitContainerCube.InitializeGeometry();
            UnitContainerCube.UpdateVAO();

            ZXPlane = new cPlane(appViewModel);
            ZXPlane.InitializeGeometry();
            ZXPlane.UpdateVAO();

            Spiral = new cSpiral(appViewModel);
            Spiral.InitializeGeometry();
            Spiral.UpdateVAO();

            CoilingAxis = new cAxis(appViewModel);
            CoilingAxis.InitializeGeometry();
            CoilingAxis.UpdateVAO();

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
                appViewModel.BackgroundColor = color;

            }//if

        }//btnBackColor_Click               


        /*******************************************************************************
        * @brief delegate that manages the event that occurs when the user clicks over
        * the unity cube color selecction button.
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void btnUintCubeColor_Click(object sender, RoutedEventArgs e) {
            var dialog = new ColorPickerWindow(appViewModel.UCubeColor);
            dialog.Owner = this;

            bool? resultado = dialog.ShowDialog();

            if (resultado == true && dialog.SelectedColor.HasValue) {

                Color color = dialog.SelectedColor.Value;
                appViewModel.UCubeColor = color;

            }//if

        }//btnUintCubeColor_Click        

        /*******************************************************************************
        * @brief delegate that manages the event that occurs when the user clicks over
        * the ZXplane color selecction button.
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void btnZXPlaneColor_Click(object sender, RoutedEventArgs e) {
            var dialog = new ColorPickerWindow(appViewModel.ZXPlaneColor);
            dialog.Owner = this;

            bool? resultado = dialog.ShowDialog();

            if (resultado == true && dialog.SelectedColor.HasValue) {

                Color color = dialog.SelectedColor.Value;
                appViewModel.ZXPlaneColor = color;

            }//if

        }//btnZXPlane_Click    

        /*******************************************************************************
        * @brief delegate that manages the event that occurs when the user clicks over
        * the reset view to X axis
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void btnResetViewOrientationToYZ_Click(object sender, RoutedEventArgs e) {

            // set all the view vector parameters to its default value
            appViewModel.ViewPhiZenithAngle = 0;
            appViewModel.ViewThetaAzimuthAngle = 0;
            // appViewModel.ViewRoDistance = 4;

        }//btnResetViewOrientationToYZ_Click

        /*******************************************************************************
        * @brief delegate that manages the event that occurs when the user clicks over
        * the reset view to Y axis
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void btnResetViewOrientationToZX_Click(object sender, RoutedEventArgs e) {

            // set all the view vector parameters to its default value
            appViewModel.ViewPhiZenithAngle = 90;
            appViewModel.ViewThetaAzimuthAngle = 0;
            // appViewModel.ViewRoDistance = 4;

        }//btnResetViewOrientationToZX_Click

        /*******************************************************************************
        * @brief delegate that manages the event that occurs when the user clicks over
        * the reset view to Z axis
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void btnResetViewOrientationToYX_Click(object sender, RoutedEventArgs e) {

            // set all the view vector parameters to its default value
            appViewModel.ViewPhiZenithAngle = 0;
            appViewModel.ViewThetaAzimuthAngle = 90;
            // appViewModel.ViewRoDistance = 4;

        }//btnResetViewOrientationToYX_Click

        /*******************************************************************************
        * @brief delegate that manages the event that occurs when the user clicks over
        * the reset view to 45 45 axis
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void btnResetOrientationTo4545_Click(object sender, RoutedEventArgs e) {

            // set all the view vector parameters to its default value
            appViewModel.ViewPhiZenithAngle = 45;
            appViewModel.ViewThetaAzimuthAngle = 45;
            // appViewModel.ViewRoDistance = 4;

        }//btnResetOrientationTo4545

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
        * @brief delegate that manages the event that occurs when the user requests to 
        * close the window
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void mainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) {

            MessageBoxResult result = MessageBox.Show(
                    "Do you want to leave Raup Coiler II application?",
                    "Confirm leave application",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
            );

            e.Cancel = (result!= MessageBoxResult.Yes) && (result != MessageBoxResult.OK);

        }//mainWindow_Closing

        /*******************************************************************************
        * @brief delegate that manages the event that occurs when the user clicks over
        * the Exit menu option to leave the application.
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void menuExitItem_Click(object sender, RoutedEventArgs e) {

            // close the main form (it callse the mainWindow_Closing event )
            Close();

        }//menuExitItem_Click

        /*******************************************************************************
        * @brief delegate that manages the event that occurs when the user clicks over
        * the export menu option.
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void menuExportItem_Click(object sender, RoutedEventArgs e) {
            string strExportFormat = "";

            SaveFileDialog saveFileDialog = new SaveFileDialog {
                Title = "Export shell model",
                Filter = "STL file (*.stl)|*.stl|" +
                         "OBJ file (*.obj)|*.obj|" +
                         "All files (*.*)|*.*",
                DefaultExt = ".obj",
                AddExtension = true,
                OverwritePrompt = true
            };

            bool? result = saveFileDialog.ShowDialog();

            if (result == true) {
                string fileName = saveFileDialog.FileName;

                strExportFormat = System.IO.Path.GetExtension(fileName);
                strExportFormat = strExportFormat.Replace(".","");

                // call the method that exports the 3D model
                if (ShellModel.export(fileName, strExportFormat) >= 0) {
                    MessageBox.Show("The current shell model has been succesfully exported to \"" + fileName + "\" file.", "Export shell model");
                } else {
                    MessageBox.Show("The current shell model could not be exported to \"" + fileName + "\" file.", "Export shell model");
                }

            }//if

        }//menuExportItem_Click

        /*******************************************************************************
        * @brief delegate that manages the event that occurs when the user clicks over
        * the New menu option.
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void menuNewShell_Click(object sender, RoutedEventArgs e) {

            MessageBoxResult result = MessageBox.Show(
                "All shell parameters will be lost. Start a new shell model?",
                "New shell model",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.OK) {

                // El usuario ha pulsado Aceptar
                appViewModel.setDefaultValues();

            } else {
                // El usuario ha pulsado Cancelar
            }

        }//menuNewShell_Click

        /*******************************************************************************
        * @brief delegate that manages the event that occurs when the user clicks over
        * the Load Shell menu option to load the Shell model information from a file.
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void menuLoadShell_Click(object sender, RoutedEventArgs e) {

            OpenFileDialog openFileDialog = new OpenFileDialog {
                Title = "Open shell model",
                Filter = "Raup coiler II file (*.rii)|*.rii|" +
                         "All files (*.*)|*.*",
                DefaultExt = ".rii",
                CheckFileExists = true,
                Multiselect = false
            };

            bool? result = openFileDialog.ShowDialog();

            if (result == true) {
                string fileName = openFileDialog.FileName;

                // call the method that loads the project parameters from the file
                if (appViewModel.loadFromDisc(fileName) >= 0) {
                    MessageBox.Show("Shell model has been succesfully loaded from \"" + fileName + "\" file.", "Load shell model");
                } else {
                    MessageBox.Show("Could not load the shell model in \"" + fileName + "\" file.", "Load shell model");
                }

            }//if

        }//menuLoadShell_Click

        /*******************************************************************************
        * @brief delegate that manages the event that occurs when the user clicks over
        * the Save Shell model menu option to save model information into a file.
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void menuSaveShell_Click(object sender, RoutedEventArgs e) {

            SaveFileDialog saveFileDialog = new SaveFileDialog {
                Title = "Save shell model",
                Filter = "Raup coiler II file (*.rii)|*.rii|" +
                         "All files (*.*)|*.*",
                DefaultExt = ".rii",
                AddExtension = true,
                OverwritePrompt = true
            };

            bool? result = saveFileDialog.ShowDialog();

            if (result == true) {
                string fileName = saveFileDialog.FileName;

                // call the method that saves the project parameters into a file
                if (appViewModel.saveToDisc(fileName) >= 0) {
                    MessageBox.Show("The current shell model has been succesfully saved to \"" + fileName + "\" file.", "Save shell model");
                } else {
                    MessageBox.Show("The current shell model could not be saved to \"" + fileName + "\" file.", "Save shell model");
                }

            }//if

        }//menuSaveShell_Click

    }
}
