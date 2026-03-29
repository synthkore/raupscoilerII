using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Data;

namespace RaupCoilerII {

    /*******************************************************************************
    * @brief the view model object used to share data between the View and the DataModel.
    * All the public attributes are implemented with as properties with a setter 
    * and getter to allow binding them to the user interface. It also impelements
    * the INotifyPropertyChanged methods to send changes to the interface when changes
    * the value of any of its properties.
    *******************************************************************************/
    public class ViewModel : INotifyPropertyChanged {

        public enum tGeometryStyleEnum {
            NoMesh = 0,
            Vertexs,
            Rings,
            Quads,
            Triangles
        }

        // constants

        public static readonly GridLength rightPanelWidth = new GridLength(500);

        public const double DparamMax = 1.5f;
        public const double DparamMin = 0;
        public double WparamMax { get; } = 10000;
        public double WparamMin { get; } = 1;
        public double TparamMax { get; } = 10;
        public double TparamMin { get; } = 0;
        public double SparamMax { get; } = 2;
        public double SparamMin { get; }  = 0;

        public int RevolutionsMax { get; } = 36;
        public int RevolutionsMin { get; } = 1;
        public int ResolutionMax { get; } = 144;
        public int ResolutionMin { get; } = 3;
        public int RingsRevMax { get; } = 360;
        public int RingsRevMin { get; } = 4;

        public double XOrientationMin { get; } = -360;
        public double XOrientationMax { get; } = 360;
        public double YOrientationMin { get; } = -360;
        public double YOrientationMax { get; } = 360;
        public double ZOrientationMin { get; } = -360;
        public double ZOrientationMax { get; } = 360;

        // properties

        tGeometryStyleEnum geometryStyle = tGeometryStyleEnum.Triangles;

        public bool bRequireToReRender = true; // flag that indicates that has been any change in a parameter that requires to redner the model again.
        public bool bRequireToRegenerate = true; // flag that indicates that has been any change in a parameter that requires to recalculate the Raup model geometry

        // Raup model parameters
        // Raup model D parameter
        double dDparam;
        public double Dparam {
            get { return dDparam; }
            set { 
                if (dDparam != value) {
                    // check that received value is in range
                    if (value > DparamMax) {
                        dDparam = DparamMax;
                    } else if (value < DparamMin) {
                        dDparam = DparamMin;
                    } else {
                        dDparam = value;
                    }
                    bRequireToRegenerate = true;
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("Dparam");
                }
            }//set
        }//Dparam

        // Raup model W parameter
        double dWparam;
        public double Wparam {
            get { return dWparam; }
            set {
                if (dWparam != value) {
                    // check that received value is in range
                    if (value > WparamMax) {
                        dWparam = WparamMax;
                    } else if (value < WparamMin) {
                        dWparam = WparamMin;
                    } else {
                        dWparam = value;
                    }
                    bRequireToRegenerate = true;
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("Wparam");
                }
            }//set
        }//Wparam

        // Raup model T parameter
        double dTparam;
        public double Tparam {
            get { return dTparam; }
            set {
                if (dTparam != value) {
                    // check that received value is in range
                    if (value > TparamMax) {
                        dTparam = TparamMax;
                    } else if (value < TparamMin) {
                        dTparam = TparamMin;
                    } else {
                        dTparam = value;
                    }
                    bRequireToRegenerate = true;
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("Tparam");
                }
            }//set
        }//Tparam

        // Raup model S parameter
        double dSparam;
        public double Sparam {
            get { return dSparam; }
            set {
                if (dSparam != value) {
                    // check that received value is in range
                    if (value > SparamMax) {
                        dSparam = SparamMax;
                    } else if (value < SparamMin) {
                        dSparam = SparamMin;
                    } else {
                        dSparam = value;
                    }
                    bRequireToRegenerate = true;
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("Sparam");
                }
            }//set
        }//Sparam

        // Geometry parameters and flags
        // NoMesh flag
        bool bNoMesh;
        public bool NoMesh {
            get { return bNoMesh; }
            set {
                if (bNoMesh != value) {
                    if (value == true) {
                        bNoMesh = true;
                        geometryStyle = tGeometryStyleEnum.NoMesh;
                    } else {
                        bNoMesh = false;
                    }
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("NoMesh");
                }//if
            }//set
        }//NoMesh

        // Vertexs flag
        bool bVertexs;
        public bool Vertexs {
            get { return bVertexs; }
            set {
                if (bVertexs != value) {
                    if (value == true) {
                        bVertexs = true;
                        geometryStyle = tGeometryStyleEnum.Vertexs;
                    } else {
                        bVertexs = false;
                    }
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("Vertexs");
                }//if
            }//set
        }//Vertexs

        // Rings flag
        bool bRings;
        public bool Rings {
            get { return bRings; }
            set {
                if (bRings != value) {
                    if (value == true) {
                        bRings = true;
                        geometryStyle = tGeometryStyleEnum.Rings;
                    } else {
                        bRings = false;
                    }
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("Rings");
                }//if
            }
        }//Rings

        // Quads flag
        bool bQuads;
        public bool Quads {
            get { return bQuads; }
            set {
                if (bQuads != value) {
                    if (value == true) {
                        bQuads = true;
                        geometryStyle = tGeometryStyleEnum.Quads;
                    } else {
                        bQuads = false;
                    }
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("Quads");
                }//if
            }//set
        }//Quads

        // Triangles flag
        bool bTriangles;
        public bool Triangles {
            get { return bTriangles; }
            set {
                if (bTriangles != value) {
                    if (value == true) {
                        bTriangles = true;
                        geometryStyle = tGeometryStyleEnum.Triangles;
                    } else {
                        bTriangles = false;
                    }
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("Triangles");
                }//if
            }//set
        }//Triangles

        // Spiral flag
        bool bSpiral;
        public bool Spiral {
            get { return bSpiral; }
            set {
                if (bSpiral != value) {
                    bSpiral = value;
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("Spiral");
                }//if
            }//set
        }//Spiral

        // Axis flag
        bool bAxis;
        public bool Axis {
            get { return bAxis; }
            set {
                if (bAxis != value) {
                    bAxis = value;
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("Axis");
                }//if
            }//set
        }//Axis

        // Mesh parameters
        // Revolutions
        int iRevolutions;
        public int Revolutions {
            get { return iRevolutions; }
            set {
                if (iRevolutions != value) {
                    // check that received value is in range
                    if (value > RevolutionsMax) {
                        iRevolutions = RevolutionsMax;
                    } else if (value < RevolutionsMin) {
                        iRevolutions = RevolutionsMin;
                    } else {
                        iRevolutions = value;
                    }
                    bRequireToRegenerate = true;
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("Revolutions");
                }
            }//set
        }//Revolutions

        // Resolution
        int iResolution;
        public int Resolution {
            get { return iResolution; }
            set {
                if (iResolution != value) {
                    // check that received value is in range
                    if (value > ResolutionMax) {
                        iResolution = ResolutionMax;
                    } else if (value < ResolutionMin) {
                        iResolution = ResolutionMin;
                    } else {
                        iResolution = value;
                    }
                    bRequireToRegenerate = true;
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("Resolution");
                }
            }//set
        }// Resolution

        // Rings per revolution
        int iRingsRev;
        public int RingsRev {
            get { return iRingsRev; }
            set {
                if (iRingsRev != value) {
                    // check that received value is in range
                    if (value > RingsRevMax) {
                        iRingsRev = RingsRevMax;
                    } else if (value < RingsRevMin) {
                        iRingsRev = RingsRevMin;
                    } else {
                        iRingsRev = value;
                    }
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("RingsRev");
                }
            }//set
        }// Resolution

        // Axis flag
        bool bAxis3D;
        public bool Axis3D {
            get { return bAxis3D; }
            set {
                if (bAxis3D != value) {
                    bAxis3D = value;
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("Axis3D");
                }//if
            }//set
        }//Axis

        // Model orientation parameters
        // X rotation
        double dXOrientation;
        public double XOrientation {
            get { return dXOrientation; }
            set {
                if (dXOrientation != value) {
                    // check that received value is in range
                    if (value > XOrientationMax) {
                        dXOrientation = XOrientationMax;
                    } else if (value < XOrientationMin) {
                        dXOrientation = XOrientationMin;
                    } else {
                        dXOrientation = value;
                    }
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("Xorientation");
                }
            }//set
        }//Xorientation

        // Y rotation
        double dYOrientation;
        public double YOrientation {
            get { return dYOrientation; }
            set {
                if (dYOrientation != value) {
                    // check that received value is in range
                    if (value > YOrientationMax) {
                        dYOrientation = YOrientationMax;
                    } else if (value < YOrientationMin) {
                        dYOrientation = YOrientationMin;
                    } else {
                        dYOrientation = value;
                    }
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("Yorientation");
                }
            }//set
        }//Yorientation

        // Z rotation
        double dZOrientation;
        public double ZOrientation {
            get { return dZOrientation; }
            set {
                if (dZOrientation != value) {
                    // check that received value is in range
                    if (value > ZOrientationMax) {
                        dZOrientation = ZOrientationMax;
                    } else if (value < ZOrientationMin) {
                        dZOrientation = ZOrientationMin;
                    } else {
                        dZOrientation = value;
                    }
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("Zorientation");
                }
            }//set
        }//Zorientation

        // mesh color code
        Color colMeshColor;
        public Color MeshColor {
            get { return colMeshColor; }
            set {
              if (colMeshColor != value) {
                colMeshColor = value;
                bRequireToRegenerate = true;
                bRequireToReRender = true;
                this.NotifyMyPropertyChanged("MeshColor");
              }
            }//set
        }//MeshColor

        // axis color code
        Color colAxisColor;
        public Color AxisColor {
            get { return colAxisColor; }
            set {
                if (colAxisColor != value) {
                    colAxisColor = value;
                    bRequireToRegenerate = true;
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("AxisColor");
                }
            }//set
        }//AxisColor

        // spiral color code
        Color colSpiralColor;
        public Color SpiralColor {
            get { return colSpiralColor; }
            set {
                if (colSpiralColor != value) {
                    colSpiralColor = value;
                    bRequireToRegenerate = true;
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("SpiralColor");
                }
            }//set
        }//SpiralColor

        // background color code
        Color colBackgroundColor;
        public Color BackgroundColor {
            get { return colBackgroundColor; }
            set {
                if (colBackgroundColor != value) {
                    bRequireToRegenerate = true;
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("BackgroundColor");
                }
            }//set
        }//BackgroundColor

        // required on classes that implement the INotifyPropertyChanged interface to notify changes to the user interface
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyMyPropertyChanged(string propName) {
            if (this.PropertyChanged != null) {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        /*******************************************************************************
        * @brief Default constructor
        *******************************************************************************/
        public ViewModel() {

            Dparam = 0.025f;
            Wparam = 1;
            Tparam = 0;
            Sparam = 1;

            geometryStyle = tGeometryStyleEnum.Triangles;

            NoMesh = false;
            Vertexs = false;
            Rings = false;
            Quads = false;
            Triangles = true;
            Spiral = false;
            Axis = true;

            Revolutions = 6;
            Resolution = 36;
            RingsRev = 16;

            XOrientation = 0;
            YOrientation = 90;
            ZOrientation = 0;

            MeshColor = System.Windows.Media.Color.FromRgb(0, 255, 0);
            AxisColor = System.Windows.Media.Color.FromRgb(255, 0, 0);
            SpiralColor = System.Windows.Media.Color.FromRgb(255, 0, 0);
            BackgroundColor = System.Windows.Media.Color.FromRgb(0, 0, 0);

        }//ViewModel

    }//ViewModel

}
