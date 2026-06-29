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
            Triangles,
            Faces
        }

        // constants

        public static readonly GridLength rightPanelWidth = new GridLength(500);

        public const double DparamMax = 1.5f;
        public const double DparamMin = 0;
        public double WparamMax { get; } = 10000;
        public double WparamMin { get; } = 1;
        public double TparamMax { get; } = 10;
        public double TparamMin { get; } = 0;
        public double SparamMax { get; } = 5;
        public double SparamMin { get; }  = 0;

        public int RevolutionsMax { get; } = 10;
        public int RevolutionsMin { get; } = 1;
        public int ResolutionMax { get; } = 180;
        public int ResolutionMin { get; } = 3;
        public int RingsRevMax { get; } = 360;
        public int RingsRevMin { get; } = 1;

        public double XOrientationMin { get; } = -360;
        public double XOrientationMax { get; } = 360;
        public double YOrientationMin { get; } = -360;
        public double YOrientationMax { get; } = 360;
        public double ZOrientationMin { get; } = -360;
        public double ZOrientationMax { get; } = 360;
        
        public double ViewDistanceMin { get; } = 0;
        public double ViewDistanceMax { get; } = 10;
        public double LightDistanceMin { get; } = 0;
        public double LightDistanceMax { get; } = 10;

        // properties

        public tGeometryStyleEnum geometryStyle = tGeometryStyleEnum.Triangles;

        public bool bRequireToReRender = true; // flag that indicates that has been any change in a parameter that requires to redner the model again.
        public bool bRequireToRegenerate = true; // flag that indicates that has been any change in a parameter that requires to recalculate the Raup model geometry
        public bool bRequireToTransform = true; // flag that indicates that has been any change in a transformation parameter that requires to apply the transformation to the model

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
                    // bRequireToTransform = true;
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
                    // bRequireToTransform = true;
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
                    // bRequireToTransform = true;
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
                    // bRequireToTransform = true;
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

        // Faces flag
        bool bFaces;
        public bool Faces {
            get { return bFaces; }
            set {
                if (bFaces != value) {
                    if (value == true) {
                        bFaces = true;
                        geometryStyle = tGeometryStyleEnum.Faces;
                    } else {
                        bFaces = false;
                    }
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("Faces");
                }//if
            }//set
        }//bFaces

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

        // Autoscale flag to enable or disable scaling the shell model to the frustrum volume ( x,y,z: -1 x,y,z: +1 )
        bool bAutoscale;
        public bool Autoscale {
            get { return bAutoscale; }
            set {
                if (bAutoscale != value) {
                    bAutoscale = value;
                    bRequireToRegenerate = true;
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("Autoscale");
                }//if
            }//set
        }//Autoscale

        // UnitCube flag to enable or disable showing the unitary cube that should contain teh shell model when autoscale mode is enabled
        bool bUnitCube;
        public bool UnitCube {
            get { return bUnitCube; }
            set {
                if (bUnitCube != value) {
                    bUnitCube = value;
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("UnitCube");
                }//if
            }//set
        }//UnitCube

        // Mesh parameters

        // Shell initial radius value
        double dIniRadius;
        public double IniRadius {
            get { return dIniRadius; }
            set {
                if (dIniRadius != value) {
                    // check that received value is in range
                    if (value > 0.5) {
                        dIniRadius = SparamMax;
                    } else if (value < 0.0001) {
                        dIniRadius = 0.0001;
                    } else {
                        dIniRadius = value;
                    }
                    bRequireToRegenerate = true;
                    // bRequireToTransform = true;
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("IniRadius");
                }
            }//set
        }//IniRadius        

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
                    // bRequireToTransform = true;
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
                    // bRequireToTransform = true;
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
                    bRequireToRegenerate = true;
                    // bRequireToTransform = true;
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

        // Model transformation parameters
        // Model rotation arround X axis
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
                    bRequireToTransform = true;
                    this.NotifyMyPropertyChanged("Xorientation");
                }
            }//set
        }//Xorientation

        // Y rotation
        // Model rotation arround Y axis
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
                    bRequireToTransform = true;
                    this.NotifyMyPropertyChanged("Yorientation");
                }
            }//set
        }//Yorientation

        // Z rotation
        // Model rotation arround Z axis
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
                    bRequireToTransform = true;
                    this.NotifyMyPropertyChanged("Zorientation");
                }
            }//set
        }//Zorientation

        // The view vector, points from the coordinates defined by the Spheric
        // coordinates (Ro,Theta,Phi) to the origin (0,0,0)
        // View vector coordinates: radius / distance to the 0,0,0 origin
        double dViewRoDistance;
        public double ViewRoDistance {
            get { return dViewRoDistance; }
            set {
                if (dViewRoDistance != value) {
                    // check that received value is in range
                    if (value > ViewDistanceMax) {
                        dViewRoDistance = ViewDistanceMax;
                    } else if (value < ViewDistanceMin) {
                        dViewRoDistance = ViewDistanceMin;
                    } else {
                        dViewRoDistance = value;
                    }
                    if (bLinkLightToView) LightRoDistance = dViewRoDistance;
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("ViewRoDistance");
                }
            }//set
        }//ViewRoDistance

        // View vector coordinates: polar/zenith angle
        double dViewPhiZenithAngle;
        public double ViewPhiZenithAngle {
            get { return dViewPhiZenithAngle; }
            set {
                if (dViewPhiZenithAngle != value) {
                    // check that received value is in range
                    if (value > XOrientationMax) {
                        dViewPhiZenithAngle = XOrientationMax;
                    } else if (value < XOrientationMin) {
                        dViewPhiZenithAngle = XOrientationMin;
                    } else {
                        dViewPhiZenithAngle = value;
                    }
                    if (bLinkLightToView) LightPhiZenithAngle = dViewPhiZenithAngle;
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("ViewPhiZenithAngle");
                }
            }//set
        }//ViewPhiPolarAngle

        // View vector coordinates: azimuth angle
        double dViewThetaAzimuthAngle;
        public double ViewThetaAzimuthAngle {
            get { return dViewThetaAzimuthAngle; }
            set {
                if (dViewThetaAzimuthAngle != value) {
                    // check that received value is in range
                    if (value > YOrientationMax) {
                        dViewThetaAzimuthAngle = YOrientationMax;
                    } else if (value < YOrientationMin) {
                        dViewThetaAzimuthAngle = YOrientationMin;
                    } else {
                        dViewThetaAzimuthAngle = value;
                    }
                    if (bLinkLightToView) LightThetaAzimuthAngle = dViewThetaAzimuthAngle;
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("ViewThetaAzimuthAngle");
                }
            }//set
        }//ViewThetaAzimuthAngle

        // The light vector, points from the coordinates defined by the Spheric
        // coordinates (Ro,Theta,Phi) to the origin (0,0,0)
        // Light vector coordinates: radius / distance to the 0,0,0 origin
        double dLightRoDistance;
        public double LightRoDistance {
            get { return dLightRoDistance; }
            set {
                if (dLightRoDistance != value) {
                    // check that received value is in range
                    if (value > LightDistanceMax) {
                        dLightRoDistance = LightDistanceMax;
                    } else if (value < LightDistanceMin) {
                        dLightRoDistance = LightDistanceMin;
                    } else {
                        dLightRoDistance = value;
                    }
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("LightRoDistance");
                }
            }//set
        }//ViewRoDistance

        // Light vector coordinates: polar/zenith angle
        double dLightPhiZenithAngle;
        public double LightPhiZenithAngle {
            get { return dLightPhiZenithAngle; }
            set {
                if (dLightPhiZenithAngle != value) {
                    // check that received value is in range
                    if (value > XOrientationMax) {
                        dLightPhiZenithAngle = XOrientationMax;
                    } else if (value < XOrientationMin) {
                        dLightPhiZenithAngle = XOrientationMin;
                    } else {
                        dLightPhiZenithAngle = value;
                    }
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("LightPhiZenithAngle");
                }
            }//set
        }//LightPhiPolarAngle

        // Light vector coordinates: azimuth angle
        double dLightThetaAzimuthAngle;
        public double LightThetaAzimuthAngle {
            get { return dLightThetaAzimuthAngle; }
            set {
                if (dLightThetaAzimuthAngle != value) {
                    // check that received value is in range
                    if (value > YOrientationMax) {
                        dLightThetaAzimuthAngle = YOrientationMax;
                    } else if (value < YOrientationMin) {
                        dLightThetaAzimuthAngle = YOrientationMin;
                    } else {
                        dLightThetaAzimuthAngle = value;
                    }
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("LightThetaAzimuthAngle");
                }
            }//set
        }//LightThetaAzimuthAngle

        // Axis flag
        bool bLinkLightToView;
        public bool LinkLightToView {
            get { return bLinkLightToView; }
            set {
                if (bLinkLightToView != value) {
                    bLinkLightToView = value;
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("LinkLightToView");
                }//if
            }//set
        }//LinkLightToView

        // mesh color code
        Color colMeshColor;
        public Color MeshColor {
            get { return colMeshColor; }
            set {
              if (colMeshColor != value) {
                colMeshColor = value;
                bRequireToTransform = true;
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
                    bRequireToTransform = true;
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
                    bRequireToTransform = true;
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
                    bRequireToTransform = true;
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("BackgroundColor");
                }
            }//set
        }//BackgroundColor

        // mesh transparency percentage
        int iMeshTransparency;
        public int MeshTransparency {
            get { return iMeshTransparency; }
            set {
                if (iMeshTransparency != value) {
                    iMeshTransparency = value;
                    bRequireToTransform = true;
                    bRequireToReRender = true;
                    this.NotifyMyPropertyChanged("MeshTransparency");
                }
            }//set
        }//MeshTransparency

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

            Dparam = 0.25f;
            Wparam = 1.8f;
            Tparam = 0.8f;
            Sparam = 1.0f;

            geometryStyle = tGeometryStyleEnum.Triangles;

            NoMesh = false;
            Vertexs = false;
            Rings = false;
            Quads = false;
            Triangles = false;
            Faces = true;
            Spiral = false;
            Axis = true;
            Autoscale = true;

            Axis3D = true;

            Revolutions = 10;
            Resolution = 36;
            RingsRev = 36;
            IniRadius = 0.0001;

            XOrientation = 0;
            YOrientation = 0;
            ZOrientation = 0;

            // set all the view vector parameters to its default value
            ViewPhiZenithAngle = 0;
            ViewThetaAzimuthAngle = 0;
            ViewRoDistance = 4;


            // set all the view vector parameters to its default value
            LightPhiZenithAngle = 0;
            LightThetaAzimuthAngle = 0;
            LightRoDistance = 4;

            bLinkLightToView = true;

            MeshColor = System.Windows.Media.Color.FromRgb(0, 255, 0);
            AxisColor = System.Windows.Media.Color.FromRgb(255, 0, 0);
            SpiralColor = System.Windows.Media.Color.FromRgb(255, 0, 0);
            BackgroundColor = System.Windows.Media.Color.FromRgb(0, 0, 0);
            MeshTransparency = 100;

        }//ViewModel

    }//ViewModel

}
