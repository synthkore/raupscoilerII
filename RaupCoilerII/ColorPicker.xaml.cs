using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;

namespace RaupCoilerII{

    /*****************************************************************************
    * @brief Code of the color picker control used to select the desired color
    * from the list.
    ***************************************************************************/
    public partial class ColorPickerWindow : Window {

        public Color? SelectedColor { get; private set; }

        
        public ColorPickerWindow() {
            InitializeComponent();
        }

        /*****************************************************************************
        * @brief Constructor with the color parameter
        * @param[in] the color to show in the color picker when the control is created.
        ***************************************************************************/
        public ColorPickerWindow(Color? initialColor) {
            InitializeComponent();
            colorPicker.SelectedColor = initialColor;
        }

        private void acceptButton_Click(object sender, RoutedEventArgs e) {

            SelectedColor = colorPicker.SelectedColor;
            DialogResult = true; // clave para saber que aceptó
            Close();

        }//acceptButton_Click

        private void cancelButton_Click(object sender, RoutedEventArgs e) {

            DialogResult = false;
            Close();

        }//cancelButton_Click
    
    }

}
