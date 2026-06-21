using Assimp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using OpenTK.Core;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Wpf;
using System.Diagnostics;
using System.Windows.Markup;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace RaupCoilerII {

    /*******************************************************************************
    * @brief base class that defines the common properties and methods of a 3D object
    *******************************************************************************/
    public class object3D {

        protected ViewModel refAppViewModel; // keep a reference to the ViewModel to execute some operations

        // OpenGL/Graphic card object data:
        // handlers of the OpenGL buffers that contain vertices and faces of that object
        public int objectVAO;
        protected int objectVBO;
        protected int objectEBO;

        // meshes that will keep the copy of the model geometry. The information will be copied to
        // from that buffers to the OpenGL buffers. Every time that the mesh requires to be transformed 
        // its TransformedMesh will be transformed and then copied to the OpnGL buffers
        protected List<Mesh> liOriginaldMesh = null;
        protected List<Mesh> liTransformedMesh = null;

        public Int32 iVerticesCter = 0; // the total number of vertex indices used to draw the geometry ( they may be repeated )
        public Int32 iFacesCter = 0; // the total number of faces in the geometry
        public Int32 iIndicesFacesCter = 0; // the total number of vertex INDEXs used to the define the object faces, some of the index may be repeated, because different faces may share some vertices. It is required by the DrawElements to know how many face indexs is going to process in the EBO.

        // model parameters
        public float objModelAngleX = 0f;
        public float objModelAngleY = 0f;
        public float objModelAngleZ = 0f;
        public float objModelScale = 1f;
        public float objModelTransX = 0f;
        public float objModelTransY = 0f;
        public float objModelTransZ = 0f;

        public virtual void InitializeGeometry() { }
        public virtual void TransformUpdateVAO(){ }

        /*******************************************************************************
        * @brief Auxiliar method that clones the received mesh into a new mesh
        * @return the cloned mesh.
        *******************************************************************************/
        public static Mesh CloneMesh(Mesh source) {
            var dest = new Mesh(source.Name, source.PrimitiveType);

            // Vertexs
            dest.Vertices.AddRange(source.Vertices);

            // Normals
            dest.Normals.AddRange(source.Normals);

            // Tangent and bitangent
            dest.Tangents.AddRange(source.Tangents);
            dest.BiTangents.AddRange(source.BiTangents);

            // Texture coordinates
            for (int i = 0; i < source.TextureCoordinateChannelCount; i++) {
                dest.TextureCoordinateChannels[i] = new List<Assimp.Vector3D>(source.TextureCoordinateChannels[i]);
                dest.UVComponentCount[i] = source.UVComponentCount[i];
            }

            // Vertexs colors
            for (int i = 0; i < source.VertexColorChannelCount; i++) {
                dest.VertexColorChannels[i] = new List<Color4D>(source.VertexColorChannels[i]);
            }

            // Faces defeiniti (with vertex indexs)
            foreach (var face in source.Faces) {
                var newFace = new Face();
                newFace.Indices.AddRange(face.Indices);
                dest.Faces.Add(newFace);
            }

            // Material
            dest.MaterialIndex = source.MaterialIndex;

            return dest;
        }//CloneMesh

    }//object3D

    /*******************************************************************************
    * @brief class that implements the methods of the shell 3D model.
    *******************************************************************************/
    public class cShellModel : object3D {

        // the three vectors defined by the view vector
        public Vector3 cameraRightVector; // the Right Vector of the camera 
        public Vector3 cameraUpVector;// the Up Vector of the camera 
        public Vector3 cameraForward;

        // outer shell mesh indexes
        public int iOuterShellFirstVertexIdx;
        public int iOuterShellFirstNormalIdx;
        public int iOuterShellFirstFaceIdx;
        public int iOuterShellLastVertexIdx;
        public int iOuterShellLastNormalIdx;
        public int iOuterShellLastFaceIdx;
        // inner shell mesh indexes
        public int iInnerShellFirstVertexIdx;
        public int iInnerShellFirstNormalIdx;
        public int iInnerShellFirstFaceIdx;
        public int iInnerShellLastVertexIdx;
        public int iInnerShellLastNormalIdx;
        public int iInnerShellLastFaceIdx;
        // beginning cover indexes
        public int iShellBeginCoverFirstVertexIdx;
        public int iShellBeginCoverFirstNormalIdx;
        public int iShellBeginCoverFirstFaceIdx;
        public int iShellBeginCoverLastVertexIdx;
        public int iShellBeginCoverLastNormalIdx;
        public int iShellBeginCoverLastFaceIdx;
        // enidng cover indexes
        public int iShellEndCoverFirstVertexIdx;
        public int iShellEndCoverFirstNormalIdx;
        public int iShellEndCoverFirstFaceIdx;
        public int iShellEndCoverLastVertexIdx;
        public int iShellEndCoverLastNormalIdx;
        public int iShellEndCoverLastFaceIdx;

        // scale factors to apply to fit the shell model to the appliction
        // frustrum. They are applied when teh autoscale option is checked
        public float flAutoScaleX;
        public float flAutoScaleY;
        public float flAutoScaleZ;

        // list that stores the vertices of the generating ring/shape
        List<Assimp.Vector2D> li2DVertexsGenCurve;

        /*******************************************************************************
        * @brief Constructor with pararameters.
        * @param[in] _refViewModel reference to the VieModel object that will be used 
        * to retrive some information.
        *******************************************************************************/
        public cShellModel(ViewModel _refViewModel) {

            refAppViewModel = _refViewModel;
            cameraRightVector = new Vector3();
            cameraUpVector = new Vector3();
            cameraForward = new Vector3();

            iOuterShellFirstVertexIdx = 0;
            iOuterShellFirstNormalIdx = 0;
            iOuterShellFirstFaceIdx = 0;
            iInnerShellFirstVertexIdx = 0;
            iInnerShellFirstNormalIdx = 0;
            iInnerShellFirstFaceIdx = 0;

            iOuterShellLastVertexIdx = 0;
            iOuterShellLastNormalIdx = 0;
            iOuterShellLastFaceIdx = 0;
            iInnerShellLastVertexIdx = 0;
            iInnerShellLastNormalIdx = 0;
            iInnerShellLastFaceIdx = 0;

            iShellBeginCoverFirstVertexIdx = 0;
            iShellBeginCoverFirstNormalIdx = 0;
            iShellBeginCoverFirstFaceIdx = 0;
            iShellBeginCoverLastVertexIdx = 0;
            iShellBeginCoverLastNormalIdx = 0;
            iShellBeginCoverLastFaceIdx = 0;
            
            iShellEndCoverFirstVertexIdx = 0;
            iShellEndCoverFirstNormalIdx = 0;
            iShellEndCoverFirstFaceIdx = 0;
            iShellEndCoverLastVertexIdx = 0;
            iShellEndCoverLastNormalIdx = 0;
            iShellEndCoverLastFaceIdx = 0;

             flAutoScaleX = 1.0f;
             flAutoScaleY = 1.0f;
             flAutoScaleZ = 1.0f;

            li2DVertexsGenCurve = null;

         }//object3D


        /*******************************************************************************
        * @brief mehtod that updates the factors used to fit the shell model into the 
        * screen when the AutoScale option is checked. 
        * 
        * @note the shell generation curve should have been configured previsously calling
        * that mehtod because it is used here.
        *******************************************************************************/
        public void CalculateAutoScaleFactors() {
            Assimp.Vector3D vectorAux1;
            List<Assimp.Vector3D> liAuxVec3DSmall = null; // vertices of the smallest coil turn
            List<Assimp.Vector3D> liAuxVec3DBig = null; // vertices of the largest coil turn
            float fXAux, fYAux, fZAux;
            float fXAux2, fYAux2, fZAux2;
            int iRing = 0;
            int iVertexIndex = 0;
            int iVerticesPerGenCurve = 0; // the number of vertices on each shell ring
            float flThetaRad = 0;// theta angle used to generate the complete shell model, in radians
            float flThetatLastTurn = 0;// the Theta angle at which starts the last coil turn
            float flRc = 0;
            float fAux = 0;
            // auxiliar variables used to get the maximum and minimum value of the smallest turn
            float flSmallMinX, flSmallMaxX;
            float flSmallMinY, flSmallMaxY;
            float flSmallMinZ, flSmallMaxZ;
            float flSmallXRange; // difference between flSmallMaxX and flSmallMinX
            float flSmallYRange; // difference between flSmallMaxY and flSmallMinY
            float flSmallZRange; // difference between flSmallMaxZ and flSmallMinZ
            // auxiliar variables used to get the maximum and minimum value of the largest turn
            float flBigMinX, flBigMaxX;
            float flBigMinY, flBigMaxY;
            float flBigMinZ, flBigMaxZ;
            float flBigXRange; // difference between flSmallMaxX and flSmallMinX
            float flBigYRange; // difference between flSmallMaxY and flSmallMinY
            float flBigZRange; // difference between flSmallMaxZ and flSmallMinZ

            if (refAppViewModel.Autoscale == false) {

                // no autoscale applied
                flAutoScaleX = 1.0f;
                flAutoScaleY = 1.0f;
                flAutoScaleZ = 1.0f;

            } else {

                // get the maximum and minimum X,Y,Z coordinates of the first (smallest)
                // and last(biggest) generating curves. To get the max and min coordinates
                // simpli rotate 90º the generating curve on the first ant the last turn
                liAuxVec3DSmall = new List<Assimp.Vector3D>();
                liAuxVec3DBig = new List<Assimp.Vector3D>();

                // calculate some parameters that will be used to generate the shell 3D model
                iVerticesPerGenCurve = li2DVertexsGenCurve.Count();

                // calculate the Rc value according to the confiugred D and fRadius parameter
                flRc = (float)refAppViewModel.IniRadius * (1.0f + (float)refAppViewModel.Dparam) / (1.0f - (float)refAppViewModel.Dparam);

                // calucalte the theta angle (in rads) at which starts the last ( largest turn )
                flThetatLastTurn = (refAppViewModel.Revolutions - 1) * (2.0f * (float)Math.PI);

                // get the X,Y,Z of 4 rings of the first turn (smallest) separated on 90º each one
                flThetaRad = 0;
                for (iRing = 0; iRing < 4; iRing++) {

                    // caluclate the W^(𝜃/2π) for the current Theta value
                    fAux = (float)Math.Pow((double)refAppViewModel.Wparam, (double)(flThetaRad / (double)(2.0f * Math.PI)));// W^(1/refAppViewModel.RingsRev)

                    // scale the current "flThetaRad" ring: calculate all the vertices of the current ring of the shell
                    for (iVertexIndex = 0; iVertexIndex < iVerticesPerGenCurve; iVertexIndex++) {

                        // apply the "Raup & Michelson" model parameters on the generating curve
                        fXAux = ((float)li2DVertexsGenCurve[iVertexIndex].X + flRc) * fAux;
                        fYAux = ((float)li2DVertexsGenCurve[iVertexIndex].Y * fAux) + (flRc * (float)refAppViewModel.Tparam * (fAux - 1));
                        fZAux = 0;

                        // apply the corresponding "flThetaRad" rotation over the transformed generating curve. This 
                        // converts from cylindrical coordinates to cartesian coordinates ( r,y,theta -> x,y,z )
                        fXAux2 = (fXAux * (float)Math.Cos(flThetaRad)) + (fZAux * (float)Math.Sin(flThetaRad));
                        fYAux2 = fYAux;
                        fZAux2 = -(fXAux * (float)Math.Sin(flThetaRad)) + (fZAux * (float)Math.Cos(flThetaRad));

                        // store the calculated vertex X,Y,Z inot the tempora list
                        vectorAux1 = new Assimp.Vector3D(fXAux2, fYAux2, fZAux2);
                        liAuxVec3DSmall.Add(vectorAux1);

                    }//for

                    // increase the Theta angle on 90º ( π/2 in rads )
                    flThetaRad = flThetaRad + ((float)Math.PI / 2.0f);

                }//for (iRings=0;

                // get the X,Y,Z of 4 rings of the last (largest) turn separated on 90º each one
                flThetaRad = flThetatLastTurn + ((float)Math.PI / 2.0f) * (iRing % 4); // we are processing the last turn ( the largest )
                for (iRing = 0; iRing < 4; iRing++) {

                    // caluclate the W^(𝜃/2π) for the current Theta value
                    fAux = (float)Math.Pow((double)refAppViewModel.Wparam, (double)(flThetaRad / (double)(2.0f * Math.PI)));// W^(1/refAppViewModel.RingsRev)

                    // scale the current "flThetaRad" ring: calculate all the vertices of the current ring of the shell
                    for (iVertexIndex = 0; iVertexIndex < iVerticesPerGenCurve; iVertexIndex++) {

                        // apply the "Raup & Michelson" model parameters on the generating curve
                        fXAux = ((float)li2DVertexsGenCurve[iVertexIndex].X + flRc) * fAux;
                        fYAux = ((float)li2DVertexsGenCurve[iVertexIndex].Y * fAux) + (flRc * (float)refAppViewModel.Tparam * (fAux - 1));
                        fZAux = 0;

                        // apply the corresponding "flThetaRad" rotation over the transformed generating curve. This 
                        // converts from cylindrical coordinates to cartesian coordinates ( r,y,theta -> x,y,z )
                        fXAux2 = (fXAux * (float)Math.Cos(flThetaRad)) + (fZAux * (float)Math.Sin(flThetaRad));
                        fYAux2 = fYAux;
                        fZAux2 = -(fXAux * (float)Math.Sin(flThetaRad)) + (fZAux * (float)Math.Cos(flThetaRad));

                        // store the calculated vertex X,Y,Z inot the tempora list
                        vectorAux1 = new Assimp.Vector3D(fXAux2, fYAux2, fZAux2);
                        liAuxVec3DBig.Add(vectorAux1);

                    }//for

                    // increase the Theta angle on 90º ( π/2 in rads )
                    flThetaRad = flThetaRad + ((float)Math.PI / 2.0f);

                }//for (iRings=0;

                // calculate the scale factors
                flAutoScaleX = 1.0f;
                flAutoScaleY = 1.0f;
                flAutoScaleZ = 1.0f;
              
                // get the max and mins of the smallest coil trun
                flSmallMaxX = liAuxVec3DSmall.Max(v => v.X);
                flSmallMinX = liAuxVec3DSmall.Min(v => v.X);
                flSmallMaxY = liAuxVec3DSmall.Max(v => v.Y);
                flSmallMinY = liAuxVec3DSmall.Min(v => v.Y);
                flSmallMaxZ = liAuxVec3DSmall.Max(v => v.Z);
                flSmallMinZ = liAuxVec3DSmall.Min(v => v.Z);

                flSmallXRange = flSmallMaxX - flSmallMinX;
                flSmallYRange = flSmallMaxY - flSmallMinY;
                flSmallZRange = flSmallMaxZ - flSmallMinZ;

                // get the max and mins of the largest coil trun
                flBigMaxX = liAuxVec3DBig.Max(v => v.X);
                flBigMinX = liAuxVec3DBig.Min(v => v.X);
                flBigMaxY = liAuxVec3DBig.Max(v => v.Y);
                flBigMinY = liAuxVec3DBig.Min(v => v.Y);
                flBigMaxZ = liAuxVec3DBig.Max(v => v.Z);
                flBigMinZ = liAuxVec3DBig.Min(v => v.Z);

                flBigXRange = flBigMaxX - flBigMinX;
                flBigYRange = flBigMaxY - flBigMinY;
                flBigZRange = flBigMaxZ - flBigMinZ;

                flAutoScaleX = flSmallXRange / flBigXRange;
                flAutoScaleY = flSmallYRange / flBigYRange;
                flAutoScaleZ = flSmallZRange / flBigZRange;

            }//if

        }//CalculateAutoScaleFactors

        /*******************************************************************************
        * @brief initializes the list of the vertices of the generating curve/ring 
        * used to generate the shell 3D model. The generating curve/ring is initialized
        * according to the configured parameters.
        *******************************************************************************/
        public void GetGeneratingCurveVertices() {
            Assimp.Vector2D vector2DAux1;
            float fCurrRingVertexAngleRad = 0;
            float fXAux, fYAux, fZAux;
            float fXAux2, fYAux2, fZAux2;
            float flAngleVerticesRad = 0; // angle between successive vertices of the ring, in radians
            int iVertexIndex = 0;
            int iNumVerticesGenCurve = 0;

            // create the generating curve according to the selected method
            li2DVertexsGenCurve = new List<Vector2D>();
            
            // original method
            iNumVerticesGenCurve = refAppViewModel.Resolution;
            flAngleVerticesRad = (2.0f * (float)Math.PI) / (float)refAppViewModel.Resolution;

            fCurrRingVertexAngleRad = 0;
            for (iVertexIndex = 0; iVertexIndex < iNumVerticesGenCurve; iVertexIndex++) {
               
                // generate current ring vertex X, and Y coordinates for the genrating shape
                fXAux = ((float)Math.Cos((double)fCurrRingVertexAngleRad) * (float)refAppViewModel.IniRadius) ;
                fYAux = ((float)Math.Sin((double)fCurrRingVertexAngleRad) * (float)refAppViewModel.IniRadius * (float)refAppViewModel.Sparam);
                
                // store the current vertex of the generated shape into the list
                vector2DAux1 = new Assimp.Vector2D(fXAux, fYAux);
                li2DVertexsGenCurve.Add(vector2DAux1);

                fCurrRingVertexAngleRad = fCurrRingVertexAngleRad + flAngleVerticesRad;

            }//for

        }//GetGeneratingCurveVertices

        /*******************************************************************************
        * @brief generates all the vertexs of the outer shell geometry ( the faces that
        * point outside ).
        * 
        * @param[in|out] model_mesh with all the generated vertexs are stored into that object.
        *******************************************************************************/
        public void GenerateOuterShellVertexs(ref Mesh model_mesh) {
            Assimp.Vector3D vectorAux1;
            float fXAux, fYAux, fZAux;
            float fXAux2, fYAux2, fZAux2;
            int iRing = 0;
            int iVertexIndex = 0;
            int iVerticesPerGenCurve = 0; // the number of vertices on each shell ring
            int iTotalNumberOfRings = 0; // the total number of rings in the shell model
            float flAngleRingsRad = 0; // angle between each successive ring/generating curve, in radians
            float flThetaRad = 0;// theta angle used to generate the complete shell model, in radians
            float flRc = 0;

            float fAux = 0;


            // calculate some parameters that will be used to generate the shell 3D model
            iTotalNumberOfRings = refAppViewModel.Revolutions * refAppViewModel.RingsRev; // rings in the complete model: is the number of rings per revolution multiplied by the number of revolutions
            flAngleRingsRad = ((float)refAppViewModel.Revolutions * (2.0f * (float)Math.PI)) / (float)iTotalNumberOfRings;
            iVerticesPerGenCurve = li2DVertexsGenCurve.Count();

            // keep the index of the shell outer mesh first stored vertex
            iOuterShellFirstVertexIdx = model_mesh.Vertices.Count();

            // calculate the Rc value according to the confiugred D and fRadius parameter
            flRc = (float)refAppViewModel.IniRadius * (1.0f + (float)refAppViewModel.Dparam) / (1.0f - (float)refAppViewModel.Dparam);

            flThetaRad = 0;
            for (iRing = 0; iRing < iTotalNumberOfRings; iRing++) {

                // caluclate the W^(𝜃/2π) for the current Theta value
                fAux = (float)Math.Pow((double)refAppViewModel.Wparam, (double)(flThetaRad / (double)(2.0f * Math.PI)));// W^(1/refAppViewModel.RingsRev)

                // scale the current "flThetaRad" ring: calculate all the vertices of the current ring of the shell
                for (iVertexIndex = 0; iVertexIndex < iVerticesPerGenCurve; iVertexIndex++) {

                    // apply the "Raup & Michelson" model parameters on the generating curve
                    fXAux = ((float)li2DVertexsGenCurve[iVertexIndex].X + flRc) * fAux;
                    fYAux = ((float)li2DVertexsGenCurve[iVertexIndex].Y * fAux) + (flRc * (float)refAppViewModel.Tparam * (fAux - 1));
                    fZAux = 0;

                    // apply the corresponding "flThetaRad" rotation over the transformed generating curve. This converts
                    // from cylindrica coordinates to cartesian coordinates (theta,r,y -> x,y,z ).
                    fXAux2 = (fXAux * (float)Math.Cos(flThetaRad)) + (fZAux * (float)Math.Sin(flThetaRad));
                    fYAux2 = fYAux;
                    fZAux2 = -(fXAux * (float)Math.Sin(flThetaRad)) + (fZAux * (float)Math.Cos(flThetaRad));

                    // store the calculated vertex inoto the mesh structure
                    vectorAux1 = new Assimp.Vector3D(fXAux2, fYAux2, fZAux2);
                    model_mesh.Vertices.Add(vectorAux1);

                }//for

                // increase the Theta angle of the following shell model ring
                flThetaRad = flThetaRad + flAngleRingsRad;

                // apply the W param on the next ring radius
                // fCurrRingRadius = fCurrRingRadius * flWRing;

            }//for (iRings=0;

            // keep the index of the shell inner mesh last stored vertex
            iOuterShellLastVertexIdx = model_mesh.Vertices.Count() - 1;

        }//GenerateOuterShellVertexs

        /*******************************************************************************
        * @brief generates all the vertexs of the inner shell geometry ( the faces that
        * point inside ).
        * 
        * @param[in|out] model_mesh with all the generated vertexs are stored into that object.
        *******************************************************************************/
        public void GenerateInnerShellVertexs(ref Mesh model_mesh) {
            Assimp.Vector3D vectorAux1;
            float fAngle = 0;
            float fAngleRads = 0;
            float fAngleDelta = 0;
            float fXAux, fYAux, fZAux;
            float fRadius = 0;
            int iRing = 0;
            int iVertexIndex = 0;
            int iVertexsPerRing = 0;
            int iNumberOfRings = 0;

            iVertexsPerRing = refAppViewModel.Resolution;
            iNumberOfRings = refAppViewModel.Revolutions * refAppViewModel.RingsRev;

            // keep the index of the shell inner mesh first stored vertex
            iInnerShellFirstVertexIdx = model_mesh.Vertices.Count();

            fAngleDelta = 360f / (float)iVertexsPerRing;

            fZAux = 0;
            fRadius = 0.45f;
            for (iRing = 0; iRing < iNumberOfRings; iRing++) {

                // first initialize all the vertices
                fRadius = fRadius + 0.1f;
                fAngle = 0;
                for (iVertexIndex = 0; iVertexIndex < iVertexsPerRing; iVertexIndex++) {

                    fAngleRads = MathHelper.DegreesToRadians((float)fAngle);

                    fXAux = (float)Math.Cos((double)fAngleRads) * fRadius;
                    fYAux = (float)Math.Sin((double)fAngleRads) * fRadius;

                    vectorAux1 = new Assimp.Vector3D(fXAux, fYAux, fZAux);

                    model_mesh.Vertices.Add(vectorAux1);

                    fAngle = fAngle + fAngleDelta;

                }//for

                fZAux = fZAux + 0.15f;

            }//for (iRings=0;

            // keep the index of the shell inner mesh last stored vertex
            iInnerShellLastVertexIdx = model_mesh.Vertices.Count()-1;

        }//GenerateInnerShellVertexs

        /*******************************************************************************
        * @brief creates the vertex used to define the beggining edge of the shell that
        * covers the space between the the outer and the inner shell. The vertexs of the 
        * begining cover are the same vertexs than the first ring of the outer and inner
        * shell mesh.
        * 
        * @param[in|out] model_mesh with all the generated vertexs are stored into that object.
        *******************************************************************************/
        public void GenerateShellBeginCoverVertexs(ref Mesh model_mesh) {
            int iVertexIndex = 0;
            int iAux = 0;
            Assimp.Vector3D v3Daux;
            int iVertexsPerRing = 0;
            int iNumberOfRings = 0;

            iVertexsPerRing = refAppViewModel.Resolution;
            iNumberOfRings = refAppViewModel.Revolutions * refAppViewModel.RingsRev;

            // keep the index of the shell begin cover first stored vertex
            iShellBeginCoverFirstVertexIdx = model_mesh.Vertices.Count();

            // store the vertexs of the first ring of the outer shell mesh in the list of vertexs
            // to use them to define the outer side of the faces of the begin cover
            iAux = iOuterShellFirstVertexIdx;
            for (iVertexIndex = 0; iVertexIndex < iVertexsPerRing; iVertexIndex++) {

                v3Daux = model_mesh.Vertices[iAux + iVertexIndex];
                model_mesh.Vertices.Add(v3Daux);

            }//for

            // store the vertexs of the first ring of the inner shell mesh in the list of vertexs
            // to use them to define the innser side of the faces of the begin cover
            iAux = iInnerShellFirstVertexIdx;
            for (iVertexIndex = 0; iVertexIndex < iVertexsPerRing; iVertexIndex++) {

                v3Daux = model_mesh.Vertices[iAux + iVertexIndex];
                model_mesh.Vertices.Add(v3Daux);

            }//for

            // keep the index of the shell begin cover last stored vertex
            iShellBeginCoverLastVertexIdx = model_mesh.Vertices.Count()-1;

        }//GenerateShellBeginCoverVertexs

        /*******************************************************************************
        * @brief creates the vertex used to define the ending edge of the shell that
        * covers the space between the the outer and the inner shell. The vertexs of the 
        * ending cover are the same vertexs than the last ring of the outer and inner
        * shell mesh.
        * 
        * @param[in|out] model_mesh with all the generated vertexs are stored into that object.
        *******************************************************************************/
        public void GenerateShellEndCoverVertexs(ref Mesh model_mesh) {
            int iVertexIndex = 0;
            int iAux = 0;
            Assimp.Vector3D v3Daux;
            int iVertexsPerRing = 0;
            int iNumberOfRings = 0;

            iVertexsPerRing = refAppViewModel.Resolution;
            iNumberOfRings = refAppViewModel.Revolutions * refAppViewModel.RingsRev;

            // keep the index of the shell end cover first stored vertex
            iShellEndCoverFirstVertexIdx = model_mesh.Vertices.Count();

            // store the vertexs of the last ring of the outer shell mesh in the list of vertexs
            // to use them to define the outer side of the faces of the end cover
            iAux = iOuterShellLastVertexIdx - iVertexsPerRing + 1;
            for (iVertexIndex = 0; iVertexIndex < iVertexsPerRing; iVertexIndex++) {

                v3Daux = model_mesh.Vertices[iAux + iVertexIndex];
                model_mesh.Vertices.Add(v3Daux);

            }//for

            // store the vertexs of the last ring of the inner shell mesh in the list of vertexs
            // to use them to define the innser side of the faces of the end cover
            iAux = iInnerShellLastVertexIdx - iVertexsPerRing + 1;
            for (iVertexIndex = 0; iVertexIndex < iVertexsPerRing; iVertexIndex++) {

                v3Daux = model_mesh.Vertices[iAux + iVertexIndex];
                model_mesh.Vertices.Add(v3Daux);

            }//for

            // keep the index of the shell end cover last stored vertex
            iShellEndCoverLastVertexIdx = model_mesh.Vertices.Count() - 1;

        }//GenerateShellEndCoverVertexs

        /*******************************************************************************
        * @brief generates the normals for all the vertices of the outer shell geometry.
        * ( the faces that point outside ).
        * 
        * @param[in|out] model_mesh with all the geometry information. Here is where
        * the generated vertex, normals, and faces information is stored
        *******************************************************************************/
        public void GenerateOuterShellNormals(ref Mesh model_mesh) {
            Assimp.Vector3D vertexAux1;
            Assimp.Vector3D vertexAux2;
            Assimp.Vector3D vectorEdge1;
            Assimp.Vector3D vectorEdge2;
            Assimp.Vector3D vertexNormal;
            int iAux = 0;
            int iRing = 0;
            int iVertexIndex = 0;
            int iVertexsPerRing = 0;
            int iNumberOfRings = 0;

            iVertexsPerRing = refAppViewModel.Resolution;
            iNumberOfRings = refAppViewModel.Revolutions * refAppViewModel.RingsRev;

            // keep the index of the shell outer mesh first stored normal
            iOuterShellFirstNormalIdx = model_mesh.Normals.Count();

            for (iRing = 0; iRing < (iNumberOfRings - 1); iRing++) {

                iAux = iOuterShellFirstVertexIdx + iRing * iVertexsPerRing;

                // calculate all the normals of vertexs of the ring
                for (iVertexIndex = 0; iVertexIndex < iVertexsPerRing; iVertexIndex++) {

                    // Indexs of a regular face of the ring:
                    // iAux = iRing * iVertexsPerRing            +                    +
                    //                   iAux + iVertexIndex>  + O ------------------ 1 + <iAux + iVertexIndex + 1
                    //                                           |  +                 +
                    //                                           |      +     face 1  +
                    //                                           |          +         + 
                    //                                           |  face 2      +     +
                    //                                           |                 +  +
                    //  iAux + iVertexIndex + iVertexsPerRing> + 2  +  +  +  +  +  +  3 + <iAux + iVertexIndex + 1 + iVertexsPerRing 
                    //                                           +                    +

                    vertexAux1 = model_mesh.Vertices[iAux + iVertexIndex];

                    // get the vector that joins the current vertex with the equivalente vertex in the next face
                    vertexAux2 = model_mesh.Vertices[iAux + ((iVertexIndex + 1) % iVertexsPerRing)];

                    vectorEdge1 = vertexAux1 - vertexAux2;

                    // get the vector that joins the current vertex with the equivalente vertex in the next face
                    vertexAux2 = model_mesh.Vertices[iAux + iVertexIndex + iVertexsPerRing];

                    vectorEdge2 = vertexAux1 - vertexAux2;

                    // calculate the normal of that vertex and add it to the vertex list
                    vertexNormal = new Assimp.Vector3D();
                    vertexNormal = Assimp.Vector3D.Cross(vectorEdge1, vectorEdge2);
                    vertexNormal.Normalize();

                    model_mesh.Normals.Add(vertexNormal);

                }// for (iVertexIndex = 0

            }//for (iRing = 0;

            iAux = iOuterShellFirstVertexIdx + iRing * iVertexsPerRing;

            // calculate all the normals of vertexs of the last ring
            for (iVertexIndex = 0; iVertexIndex < iVertexsPerRing; iVertexIndex++) {

                // Indexs of the last face of the ring:
                // iAux = iRing * iVertexsPerRing              +                    +
                //   iAux + iVertexIndex - iVertexsPerRing > + O  +  +  +  +  +  +  1 + < iAux + iVertexIndex + 1 - iVertexsPerRing 
                //                                             |  +                 +
                //                                             |      +     face 1  +
                //                                             |          +         + 
                //                                             |  face 2      +     +
                //                                             |                 +  +
                //                     iAux + iVertexIndex > + 2  ------------------  3 + < iAux + iVertexIndex + 1
                //     

                vertexAux1 = model_mesh.Vertices[iAux + iVertexIndex];

                // get the vector that joins the current vertex with the equivalente vertex in the next face
                vertexAux2 = model_mesh.Vertices[iAux + ((iVertexIndex + 1) % iVertexsPerRing)];

                vectorEdge1 = vertexAux1 - vertexAux2;

                // get the vector that joins the current vertex with the equivalente vertex in the next face
                vertexAux2 = model_mesh.Vertices[iAux + iVertexIndex - iVertexsPerRing];

                vectorEdge2 = vertexAux2 - vertexAux1;

                // calculate the normal of that vertex and add it to the vertex list
                vertexNormal = new Assimp.Vector3D();
                vertexNormal = Assimp.Vector3D.Cross(vectorEdge1, vectorEdge2);
                vertexNormal.Normalize();

                model_mesh.Normals.Add(vertexNormal);

            }//for

            // keep the index of the shell outer mesh last stored normal
            iOuterShellLastNormalIdx = model_mesh.Normals.Count()-1;

        }//GenerateOuterShellNormals

        /*******************************************************************************
        * @brief generates the normals for all the vertices of the inner shell geometry.
        * ( the faces that point inside ).
        * 
        * @param[in|out] model_mesh with all the geometry information. Here is where
        * the generated vertex, normals, and faces information is stored
        *******************************************************************************/
        public void GenerateInnerShellNormals(ref Mesh model_mesh) {
            Assimp.Vector3D vertexAux1;
            Assimp.Vector3D vertexAux2;
            Assimp.Vector3D vectorEdge1;
            Assimp.Vector3D vectorEdge2;
            Assimp.Vector3D vertexNormal;
            int iAux = 0;
            int iRing = 0;
            int iVertexIndex = 0;
            int iVertexsPerRing = 0;
            int iNumberOfRings = 0;

            iVertexsPerRing = refAppViewModel.Resolution;
            iNumberOfRings = refAppViewModel.Revolutions * refAppViewModel.RingsRev;

            // keep the index of the shell inner mesh first stored normal
            iInnerShellFirstNormalIdx = model_mesh.Normals.Count();

            for (iRing = 0; iRing < (iNumberOfRings - 1) ; iRing++) {

                iAux = iInnerShellFirstVertexIdx + iRing * iVertexsPerRing;

                // calculate all the normals of vertexs of the ring
                for (iVertexIndex = 0; iVertexIndex < iVertexsPerRing; iVertexIndex++) {

                    // Indexs of a regular face of the ring:
                    // iAux = iRing * iVertexsPerRing            +                    +
                    //                   iAux + iVertexIndex>  + O ------------------ 1 + <iAux + iVertexIndex + 1
                    //                                           |  +                 +
                    //                                           |      +     face 1  +
                    //                                           |          +         + 
                    //                                           |  face 2      +     +
                    //                                           |                 +  +
                    //  iAux + iVertexIndex + iVertexsPerRing> + 2  +  +  +  +  +  +  3 + <iAux + iVertexIndex + 1 + iVertexsPerRing 
                    //                                           +                    +

                    vertexAux1 = model_mesh.Vertices[iAux + iVertexIndex];

                    // get the vector that joins the current vertex with the equivalente vertex in the next face
                    vertexAux2 = model_mesh.Vertices[iAux + ((iVertexIndex + 1) % iVertexsPerRing)];

                    vectorEdge1 = vertexAux1 - vertexAux2;

                    // get the vector that joins the current vertex with the equivalente vertex in the next face
                    vertexAux2 = model_mesh.Vertices[iAux + iVertexIndex + iVertexsPerRing];

                    vectorEdge2 = vertexAux1 - vertexAux2;

                    // calculate the normal of that vertex and add it to the vertex list
                    vertexNormal = new Assimp.Vector3D();
                    vertexNormal = Assimp.Vector3D.Cross(vectorEdge1, vectorEdge2);
                    vertexNormal.Normalize();

                    model_mesh.Normals.Add(-vertexNormal); // inverted normal

                 }// for (iVertexIndex = 0

            }//for (iRing = 0;

            iAux = iInnerShellFirstVertexIdx + iRing * iVertexsPerRing;

            // calculate all the normals of vertexs of the last ring
            for (iVertexIndex = 0; iVertexIndex < iVertexsPerRing; iVertexIndex++) {

                // Indexs of the last face of the ring:
                // iAux = iRing * iVertexsPerRing              +                    +
                //   iAux + iVertexIndex - iVertexsPerRing > + O  +  +  +  +  +  +  1 + < iAux + iVertexIndex + 1 - iVertexsPerRing 
                //                                             |  +                 +
                //                                             |      +     face 1  +
                //                                             |          +         + 
                //                                             |  face 2      +     +
                //                                             |                 +  +
                //                     iAux + iVertexIndex > + 2 ------------------ 3 + < iAux + iVertexIndex + 1
                //     

                vertexAux1 = model_mesh.Vertices[iAux + iVertexIndex];

                // get the vector that joins the current vertex with the equivalente vertex in the next face
                vertexAux2 = model_mesh.Vertices[iAux + ((iVertexIndex + 1) % iVertexsPerRing)];

                vectorEdge1 = vertexAux1 - vertexAux2;

                // get the vector that joins the current vertex with the equivalente vertex in the next face
                vertexAux2 = model_mesh.Vertices[iAux + iVertexIndex - iVertexsPerRing];

                vectorEdge2 = vertexAux2 - vertexAux1;

                // calculate the normal of that vertex and add it to the vertex list
                vertexNormal = new Assimp.Vector3D();
                vertexNormal = Assimp.Vector3D.Cross(vectorEdge1, vectorEdge2);
                vertexNormal.Normalize();

                model_mesh.Normals.Add(-vertexNormal); // inverted normal

            }//for

            // keep the index of the shell inner mesh last stored normal
            iInnerShellLastNormalIdx = model_mesh.Normals.Count()-1;

        }//GenerateInerShellNormals

        /*******************************************************************************
        * @brief generates the normals for all the vertices of the shell begin cover
        * 
        * @param[in|out] model_mesh with all the geometry information. Here is where
        * the generated vertex, normals, and faces information is stored
        *******************************************************************************/
        public void GenerateShellBeginCoverNormals(ref Mesh model_mesh) {
            Assimp.Vector3D vertexAux1;
            Assimp.Vector3D vertexAux2;
            Assimp.Vector3D vectorEdge1;
            Assimp.Vector3D vectorEdge2;
            Assimp.Vector3D vertexNormal;
            int iAux = 0;
            int iVertexIndex = 0;
            int iVertexsPerRing = 0;
            int iNumberOfRings = 0;

            iVertexsPerRing = refAppViewModel.Resolution;
            iNumberOfRings = refAppViewModel.Revolutions * refAppViewModel.RingsRev;

            // keep the index of the shell begin cover first stored normal
            iShellBeginCoverFirstNormalIdx = model_mesh.Normals.Count();

            // calculate all the normals of the vertexs of the outer ring of the begin cover
            iAux = iShellBeginCoverFirstVertexIdx;
            for (iVertexIndex = 0; iVertexIndex < iVertexsPerRing; iVertexIndex++) {

                // Indexs of a regular face of the ring:
                // iAux = iRing * iVertexsPerRing            +                    +
                //                   iAux + iVertexIndex>  + O ------------------ 1 + <iAux + iVertexIndex + 1
                //                                           |  +                 +
                //                                           |      +     face 1  +
                //                                           |          +         + 
                //                                           |  face 2      +     +
                //                                           |                 +  +
                //  iAux + iVertexIndex + iVertexsPerRing> + 2  +  +  +  +  +  +  3 + <iAux + iVertexIndex + 1 + iVertexsPerRing 
                //                                           +                    +

                vertexAux1 = model_mesh.Vertices[iAux + iVertexIndex];

                // get the vector that joins the current vertex with the equivalente vertex in the next face
                vertexAux2 = model_mesh.Vertices[iAux + ((iVertexIndex + 1) % iVertexsPerRing)];

                vectorEdge1 = vertexAux1 - vertexAux2;

                // get the vector that joins the current vertex with the equivalente vertex in the next face
                vertexAux2 = model_mesh.Vertices[iAux + iVertexIndex + iVertexsPerRing];

                vectorEdge2 = vertexAux1 - vertexAux2;

                // calculate the normal of that vertex and add it to the vertex list
                vertexNormal = new Assimp.Vector3D();
                vertexNormal = Assimp.Vector3D.Cross(vectorEdge1, vectorEdge2);
                vertexNormal.Normalize();

                model_mesh.Normals.Add(vertexNormal);

            }// for (iVertexIndex = 0

            // calculate all the normals of the vertexs of the outer ring of the begin cover. The vertexs of the 
            // inner ring of the begin cover are stored just after the vertex of the
            iAux = iShellBeginCoverFirstVertexIdx + iVertexsPerRing;
            for (iVertexIndex = 0; iVertexIndex < iVertexsPerRing; iVertexIndex++) {

                // Indexs of the last face of the ring:
                // iAux = iRing * iVertexsPerRing              +                    +
                //   iAux + iVertexIndex - iVertexsPerRing > + O  +  +  +  +  +  +  1 + < iAux + iVertexIndex + 1 - iVertexsPerRing 
                //                                             |  +                 +
                //                                             |      +     face 1  +
                //                                             |          +         + 
                //                                             |  face 2      +     +
                //                                             |                 +  +
                //                     iAux + iVertexIndex > + 2 ------------------ 3 + < iAux + iVertexIndex + 1
                //     

                vertexAux1 = model_mesh.Vertices[iAux + iVertexIndex];

                // get the vector that joins the current vertex with the equivalente vertex in the next face
                vertexAux2 = model_mesh.Vertices[iAux + iVertexIndex - iVertexsPerRing];

                vectorEdge1 = vertexAux2 - vertexAux1;

                // get the vector that joins the current vertex with the equivalente vertex in the next face
                vertexAux2 = model_mesh.Vertices[iAux + ((iVertexIndex + 1) % iVertexsPerRing)];

                vectorEdge2 = vertexAux2 - vertexAux1;

                // calculate the normal of that vertex and add it to the vertex list
                vertexNormal = new Assimp.Vector3D();
                vertexNormal = Assimp.Vector3D.Cross(vectorEdge2, vectorEdge1);
                vertexNormal.Normalize();

                model_mesh.Normals.Add(vertexNormal);

            }// for (iVertexIndex = 0

            // keep the index of the shell begin cover last stored normal
            iShellBeginCoverLastNormalIdx = model_mesh.Normals.Count()-1;

        }//GenerateShellBeginCoverNormals

        /*******************************************************************************
        * @brief generates the normals for all the vertices of the shell end cover
        * 
        * @param[in|out] model_mesh with all the geometry information. Here is where
        * the generated vertex, normals, and faces information is stored
        *******************************************************************************/
        public void GenerateShellEndCoverNormals(ref Mesh model_mesh) {
            Assimp.Vector3D vertexAux1;
            Assimp.Vector3D vertexAux2;
            Assimp.Vector3D vectorEdge1;
            Assimp.Vector3D vectorEdge2;
            Assimp.Vector3D vertexNormal;
            int iAux = 0;
            int iVertexIndex = 0;
            int iVertexsPerRing = 0;
            int iNumberOfRings = 0;

            iVertexsPerRing = refAppViewModel.Resolution;
            iNumberOfRings = refAppViewModel.Revolutions * refAppViewModel.RingsRev;

            // keep the index of the shell end cover first stored normal
            iShellEndCoverFirstNormalIdx = model_mesh.Normals.Count();

            // calculate all the normals of vertexs of the outer ring of the end cover
            iAux = iShellEndCoverFirstVertexIdx;
            for (iVertexIndex = 0; iVertexIndex < iVertexsPerRing; iVertexIndex++) {

                // Indexs of a regular face of the ring:
                // iAux = iRing * iVertexsPerRing            +                    +
                //                   iAux + iVertexIndex>  + O ------------------ 1 + <iAux + iVertexIndex + 1
                //                                           |  +                 +
                //                                           |      +     face 1  +
                //                                           |          +         + 
                //                                           |  face 2      +     +
                //                                           |                 +  +
                //  iAux + iVertexIndex + iVertexsPerRing> + 2  +  +  +  +  +  +  3 + <iAux + iVertexIndex + 1 + iVertexsPerRing 
                //                                           +                    +

                vertexAux1 = model_mesh.Vertices[iAux + iVertexIndex];

                // get the vector that joins the current vertex with the equivalente vertex in the next face
                vertexAux2 = model_mesh.Vertices[iAux + ((iVertexIndex + 1) % iVertexsPerRing)];

                vectorEdge1 = vertexAux2 - vertexAux1;

                // get the vector that joins the current vertex with the equivalente vertex in the next face
                vertexAux2 = model_mesh.Vertices[iAux + iVertexIndex + iVertexsPerRing];

                vectorEdge2 = vertexAux2 - vertexAux1;

                // calculate the normal of that vertex and add it to the vertex list
                vertexNormal = new Assimp.Vector3D();
                vertexNormal = Assimp.Vector3D.Cross(vectorEdge1, vectorEdge2);
                vertexNormal.Normalize();

                model_mesh.Normals.Add(-vertexNormal);

            }// for (iVertexIndex = 0

            // calculate all the normals of the vertexs of the inner ring of the end cover. The vertexs of the 
            // inner ring of the begin cover are stored just after the vertex of the
            iAux = iShellEndCoverFirstVertexIdx + iVertexsPerRing;
            for (iVertexIndex = 0; iVertexIndex < iVertexsPerRing; iVertexIndex++) {

                // Indexs of the last face of the ring:
                // iAux = iRing * iVertexsPerRing              +                    +
                //   iAux + iVertexIndex - iVertexsPerRing > + O  +  +  +  +  +  +  1 + < iAux + iVertexIndex + 1 - iVertexsPerRing 
                //                                             |  +                 +
                //                                             |      +     face 1  +
                //                                             |          +         + 
                //                                             |  face 2      +     +
                //                                             |                 +  +
                //                     iAux + iVertexIndex > + 2 ------------------ 3 + < iAux + iVertexIndex + 1
                //     

                vertexAux1 = model_mesh.Vertices[iAux + iVertexIndex];

                // get the vector that joins the current vertex with the equivalente vertex in the next face
                vertexAux2 = model_mesh.Vertices[iAux + iVertexIndex - iVertexsPerRing];

                vectorEdge1 = vertexAux1 - vertexAux2;

                // get the vector that joins the current vertex with the equivalente vertex in the next face
                vertexAux2 = model_mesh.Vertices[iAux + ((iVertexIndex + 1) % iVertexsPerRing)];

                vectorEdge2 = vertexAux2 - vertexAux1;

                // calculate the normal of that vertex and add it to the vertex list
                vertexNormal = new Assimp.Vector3D();
                vertexNormal = Assimp.Vector3D.Cross(vectorEdge1, vectorEdge2);
                vertexNormal.Normalize();

                model_mesh.Normals.Add(-vertexNormal);

            }// for (iVertexIndex = 0

            // keep the index of the shell end cover last stored normal
            iShellEndCoverLastNormalIdx = model_mesh.Normals.Count()-1;

        }//GenerateShellEndCoverNormals

        /*******************************************************************************
        * @brief fills the faces information by setting the indexs of the vertex that 
        * define all the faces of the outer shell geometry ( the faces that point outside ).
        * 
        * IMPORTANT: when using flat face coluring, OpenGL uses the color of the last 
        * vertex that defines the face to assign the color to the triangle. That vertex 
        * is called the  "provoking index" because is the one that "provokes" the color 
        * in the flat face. To grant that faces are properly coloured when using flat 
        * ilumination the "provoking index" must be properly set!. In order that the 
        * two triangles that defien a quad faces are shown with the same color both 
        * triangle/faces should use the same vertex as provoking index.
        * 
        * @param[in|out] model_mesh with all the geometry information. Here is where
        * the generated vertex, normals, and faces information is stored
        *******************************************************************************/
        public void GenerateOuterShellFaces(ref Mesh model_mesh) {
            Assimp.Face faceAux;
            int iRing = 0;
            int iVertexIndex = 0;
            int iAux = 0;
            int iVertexsPerRing = 0;
            int iNumberOfRings = 0;

            iVertexsPerRing = refAppViewModel.Resolution;
            iNumberOfRings = refAppViewModel.Revolutions * refAppViewModel.RingsRev;

            // Indexs of a regular face of the ring:
            // iAux = iRing * iVertexsPerRing            +                    +
            //                   iAux + iVertexIndex>  + O  +  +  +  +  +  +  1 + <iAux + iVertexIndex + 1
            //                                           +  +                 +
            //                                           +      +     face 1  +
            //                                           +          +         + 
            //                                           +  face 2      +     +
            //                                           +                 +  +
            //  iAux + iVertexIndex + iVertexsPerRing> + 2  +  +  +  +  +  +  3 + <iAux + iVertexIndex + 1 + iVertexsPerRing 
            //                                           +                    +
            //  Faces must be defined in this order:
            //   face 1: 0 1 3
            //   face 2: 2 0 3
            // IMPORTANT: "provoking index" is the last index of the face, and is the one that "provokes" the color of the face
            // when using flat colouring / ilumination.To grant that faces are properly coloured when using flat ilumination
            // the "provoking index" of the two triangles of the face must me the same, in that case Vertex 3. Or at less they
            // should have the same normal value.

            // keep the index of the shell outer mesh first stored face
            iOuterShellFirstFaceIdx = model_mesh.Faces.Count();

            // generate the faces index
            for (iRing = 0; iRing < (iNumberOfRings - 1); iRing++) {

                iAux = iOuterShellFirstVertexIdx + iRing * iVertexsPerRing;

                for (iVertexIndex = 0; iVertexIndex < (iVertexsPerRing - 1); iVertexIndex++) {

                    // face 1 
                    faceAux = new Face();
                    faceAux.Indices.Add(iAux + iVertexIndex); // v 0 
                    faceAux.Indices.Add(iAux + iVertexIndex + 1); // v 1
                    faceAux.Indices.Add(iAux + iVertexIndex + 1 + iVertexsPerRing); // v 3  <= "provoking index"
                    model_mesh.Faces.Add(faceAux);

                    // face 2 
                    faceAux = new Face();
                    faceAux.Indices.Add(iAux + iVertexIndex + iVertexsPerRing); // v 2
                    faceAux.Indices.Add(iAux + iVertexIndex ); // v 0                    
                    faceAux.Indices.Add(iAux + iVertexIndex + 1 + iVertexsPerRing); // v 3  <= "provoking index"
                    model_mesh.Faces.Add(faceAux);

                }//for (iRings=0;

                // Indexs of the last face of the ring:
                // iAux = iRing * iVertexsPerRing;           +                    +
                //                   iAux + iVertexIndex>  + O  +  +  +  +  +  +  1 +   <iAux
                //                                           +  +                 +
                //                                           +      +    face 1   +
                //                                           +          +         + 
                //                                           +  face 2      +     +
                //                                           +                 +  +
                //  iAux + iVertexIndex + iVertexsPerRing> + 2  +  +  +  +  +  +  3 +   <iAux +  iVertexsPerRing 
                //                                           +                    +
                //  Faces must be defined in this order:
                //   face 1: 0 1 3
                //   face 2: 2 0 3
                // IMPORTANT: "provoking index" is the last index of the face, and is the one that "provokes" the color of the face
                // when using flat colouring / ilumination.To grant that faces are properly coloured when using flat ilumination
                // the "provoking index" of the two triangles of the face must me the same, in that case Vertex 3. Or at less they
                // should have the same normal value.

                // last face 1 of the ring
                faceAux = new Face();
                faceAux.Indices.Add(iAux + iVertexIndex);// v 0
                faceAux.Indices.Add(iAux);// v 1
                faceAux.Indices.Add(iAux + iVertexsPerRing);// v 3  <= "provoking index"
                model_mesh.Faces.Add(faceAux);

                // last face 2 of the ring
                faceAux = new Face();
                faceAux.Indices.Add(iAux + iVertexIndex + iVertexsPerRing);// v 2 
                faceAux.Indices.Add(iAux + iVertexIndex);// v 0
                faceAux.Indices.Add(iAux + iVertexsPerRing);// v 3  <= "provoking index"
                model_mesh.Faces.Add(faceAux);

            }//for (iRing = 0; iRing < iNumberOfRings; iRing++) {

            // keep the index of the shell outer mesh last stored face
            iOuterShellLastFaceIdx = model_mesh.Faces.Count()-1;

        }//GenerateOuterShellFaces

        /*******************************************************************************
        * @brief fills the faces information by setting the indexs of the vertex that 
        * define all the faces of the inner shell geometry ( the faces that point inside ).
        * 
        * IMPORTANT: when using flat face coluring, OpenGL uses the color of the last 
        * vertex that defines the face to assign the color to the triangle. That vertex 
        * is called the  "provoking index" because is the one that "provokes" the color 
        * in the flat face. To grant that faces are properly coloured when using flat 
        * ilumination the "provoking index" must be properly set!. In order that the 
        * two triangles that defien a quad faces are shown with the same color both 
        * triangle/faces should use the same vertex as provoking index.
        * 
        * @param[in|out] model_mesh with all the geometry information. Here is where
        * the generated vertex, normals, and faces information is stored
        *******************************************************************************/
        public void GenerateInnerShellFaces(ref Mesh model_mesh) {
            Assimp.Face faceAux;
            int iRing = 0;
            int iVertexIndex = 0;
            int iAux = 0;
            int iVertexsPerRing = 0;
            int iNumberOfRings = 0;

            iVertexsPerRing = refAppViewModel.Resolution;
            iNumberOfRings = refAppViewModel.Revolutions * refAppViewModel.RingsRev;

            // Indexs of a regular face of the ring:
            // iAux = iRing * iVertexsPerRing            +                    +
            //                   iAux + iVertexIndex>  + O  +  +  +  +  +  +  1 + <iAux + iVertexIndex + 1
            //                                           +  +                 +
            //                                           +      +     face 1  +
            //                                           +          +         + 
            //                                           +  face 2      +     +
            //                                           +                 +  +
            //  iAux + iVertexIndex + iVertexsPerRing> + 2  +  +  +  +  +  +  3 + <iAux + iVertexIndex + 1 + iVertexsPerRing 
            //                                           +                    +
            //  Faces must be defined in this order:
            //   face 1: 3 1 0
            //   face 2: 3 0 2
            // IMPORTANT: "provoking index" is the last index of the face, and is the one that "provokes" the color of the face
            // when using flat colouring / ilumination.To grant that faces are properly coloured when using flat ilumination
            // the "provoking index" of the two triangles of the face must me the same, in that case Vertex 3. Or at less they
            // should have the same normal value.

            // keep the index of the shell inner mesh first stored face
            iInnerShellFirstFaceIdx = model_mesh.Faces.Count();

            // generate the faces index
            for (iRing = 0; iRing < (iNumberOfRings - 1); iRing++) {

                iAux = iInnerShellFirstVertexIdx + iRing * iVertexsPerRing;

                for (iVertexIndex = 0; iVertexIndex < (iVertexsPerRing - 1); iVertexIndex++) {

                    // face 1 
                    faceAux = new Face();
                    faceAux.Indices.Add(iAux + iVertexIndex + 1 + iVertexsPerRing); // v 3  
                    faceAux.Indices.Add(iAux + iVertexIndex + 1); // v 1
                    faceAux.Indices.Add(iAux + iVertexIndex); // v 0 <= "provoking index" ( despite idx 0 and idx 2 are not the same index, they have the same normal, so the color of the faces is properly shown )

                    model_mesh.Faces.Add(faceAux);

                    // face 2 
                    faceAux = new Face();
                    faceAux.Indices.Add(iAux + iVertexIndex + 1 + iVertexsPerRing); // v 3 
                    faceAux.Indices.Add(iAux + iVertexIndex); // v 0
                    faceAux.Indices.Add(iAux + iVertexIndex + iVertexsPerRing); // v 2  <= "provoking index" ( despite idx 0 and idx 2 are not the same index, they have the same normal, so the color of the faces is properly shown )

                    model_mesh.Faces.Add(faceAux);

                }//for (iRings=0;

                // Indexs of the last face of the ring:
                // iAux = iRing * iVertexsPerRing;           +                    +
                //                   iAux + iVertexIndex>  + O  +  +  +  +  +  +  1 +   <iAux
                //                                           +  +                 +
                //                                           +      +    face 1   +
                //                                           +          +         + 
                //                                           +  face 2      +     +
                //                                           +                 +  +
                //  iAux + iVertexIndex + iVertexsPerRing> + 2  +  +  +  +  +  +  3 +   <iAux +  iVertexsPerRing 
                //                                           +                    +
                //  Faces must be defined in this order:
                //   face 1: 3 1 0
                //   face 2: 3 0 2
                // IMPORTANT: "provoking index" is the last index of the face, and is the one that "provokes" the color of the face
                // when using flat colouring / ilumination.To grant that faces are properly coloured when using flat ilumination
                // the "provoking index" of the two triangles of the face must me the same, in that case Vertex 3. Or at less they
                // should have the same normal value.

                // last face 1 of the ring
                faceAux = new Face();
                faceAux.Indices.Add(iAux + iVertexsPerRing);// v 3
                faceAux.Indices.Add(iAux);// v 1
                faceAux.Indices.Add(iAux + iVertexIndex);// v 0 <= "provoking index" ( despite idx 0 and idx 2 are not the same index, they have the same normal, so the color of the faces is properly shown )
                model_mesh.Faces.Add(faceAux);

                // last face 2 of the ring
                faceAux = new Face();
                faceAux.Indices.Add(iAux + iVertexsPerRing);// v 3 
                faceAux.Indices.Add(iAux + iVertexIndex);// v 0
                faceAux.Indices.Add(iAux + iVertexIndex + iVertexsPerRing);// v 2  <= "provoking index" ( despite idx 0 and idx 2 are not the same index, they have the same normal, so the color of the faces is properly shown )
                model_mesh.Faces.Add(faceAux);

            }//for (iRing = 0; iRing < iNumberOfRings; iRing++) {

            // keep the index of the shell inner mesh last stored face
            iInnerShellLastFaceIdx = model_mesh.Faces.Count() - 1;

        }//GenerateInnerShellFaces

        /*******************************************************************************
         * @brief defines the faces indices of the shell begin cover.
         * 
         * IMPORTANT: when using flat face coluring, OpenGL uses the color of the last 
         * vertex that defines the face to assign the color to the triangle. That vertex 
         * is called the  "provoking index" because is the one that "provokes" the color 
         * in the flat face. To grant that faces are properly coloured when using flat 
         * ilumination the "provoking index" must be properly set!. In order that the 
         * two triangles that defien a quad faces are shown with the same color both 
         * triangle/faces should use the same vertex as provoking index.
         * 
         * @param[in|out] model_mesh with all the geometry information. Here is where
         * the generated vertex, normals, and faces information is stored
         *******************************************************************************/
        public void GenerateShellBeginCoverFaces(ref Mesh model_mesh) {
            Assimp.Face faceAux;
            int iVertexIndex = 0;
            int iAux = 0;
            int iVertexsPerRing = 0;
            int iNumberOfRings = 0;

            iVertexsPerRing = refAppViewModel.Resolution;
            iNumberOfRings = refAppViewModel.Revolutions * refAppViewModel.RingsRev;

            // Indexs of a regular face of the ring:
            // iAux = iRing * iVertexsPerRing            +                    +
            //                   iAux + iVertexIndex>  + O  +  +  +  +  +  +  1 + <iAux + iVertexIndex + 1
            //                                           +  +                 +
            //                                           +      +     face 1  +
            //                                           +          +         + 
            //                                           +  face 2      +     +
            //                                           +                 +  +
            //  iAux + iVertexIndex + iVertexsPerRing> + 2  +  +  +  +  +  +  3 + <iAux + iVertexIndex + 1 + iVertexsPerRing 
            //                                           +                    +
            //  Faces must be defined in this order:
            //   face 1: 1 0 3
            //   face 2: 0 2 3
            // IMPORTANT: "provoking index" is the last index of the face, and is the one that "provokes" the color of the face
            // when using flat colouring / ilumination.To grant that faces are properly coloured when using flat ilumination
            // the "provoking index" of the two triangles of the face must me the same, in that case Vertex 3. Or at less they
            // should have the same normal value.

            // keep the index of the shell begin cover first stored face
            iShellBeginCoverFirstFaceIdx = model_mesh.Faces.Count();

            iAux = iShellBeginCoverFirstVertexIdx;

            // generate the faces index
            for (iVertexIndex = 0; iVertexIndex < iVertexsPerRing ; iVertexIndex++) {

                // face 1 
                faceAux = new Face();                
                faceAux.Indices.Add(iAux + ((iVertexIndex + 1) % iVertexsPerRing)); // v 1                
                faceAux.Indices.Add(iAux + iVertexIndex); // v 0
                faceAux.Indices.Add(iAux + ((iVertexIndex+1)% iVertexsPerRing) + iVertexsPerRing); // v 3  
                model_mesh.Faces.Add(faceAux);

                // face 2 
                faceAux = new Face();
                faceAux.Indices.Add(iAux + iVertexIndex); // v 0
                faceAux.Indices.Add(iAux + iVertexIndex + iVertexsPerRing); // v  2                
                faceAux.Indices.Add(iAux + ((iVertexIndex + 1) % iVertexsPerRing) + iVertexsPerRing); // v 3
                model_mesh.Faces.Add(faceAux);

            }//for (iRings=0;

            // keep the index of the shell begin cover last stored face
            iShellBeginCoverLastFaceIdx = model_mesh.Faces.Count() - 1;

        }//GenerateShellBeginCoverFaces

        /*******************************************************************************
        * @brief defines the faces indices of the shell end cover.
        * 
        * IMPORTANT: when using flat face coluring, OpenGL uses the color of the last 
        * vertex that defines the face to assign the color to the triangle. That vertex 
        * is called the  "provoking index" because is the one that "provokes" the color 
        * in the flat face. To grant that faces are properly coloured when using flat 
        * ilumination the "provoking index" must be properly set!. In order that the 
        * two triangles that defien a quad faces are shown with the same color both 
        * triangle/faces should use the same vertex as provoking index.
        * 
        * @param[in|out] model_mesh with all the geometry information. Here is where
        * the generated vertex, normals, and faces information is stored
        *******************************************************************************/
        public void GenerateShellEndCoverFaces(ref Mesh model_mesh) {
            Assimp.Face faceAux;
            int iVertexIndex = 0;
            int iAux = 0;
            int iVertexsPerRing = 0;
            int iNumberOfRings = 0;

            iVertexsPerRing = refAppViewModel.Resolution;
            iNumberOfRings = refAppViewModel.Revolutions * refAppViewModel.RingsRev;

            // Indexs of a regular face of the ring:
            // iAux = iRing * iVertexsPerRing            +                    +
            //                   iAux + iVertexIndex>  + O  +  +  +  +  +  +  1 + <iAux + iVertexIndex + 1
            //                                           +  +                 +
            //                                           +      +     face 1  +
            //                                           +          +         + 
            //                                           +  face 2      +     +
            //                                           +                 +  +
            //  iAux + iVertexIndex + iVertexsPerRing> + 2  +  +  +  +  +  +  3 + <iAux + iVertexIndex + 1 + iVertexsPerRing 
            //                                           +                    +
            //  Faces must be defined in this order:
            //   face 1: 1 0 3
            //   face 2: 0 2 3
            // IMPORTANT: "provoking index" is the last index of the face, and is the one that "provokes" the color of the face
            // when using flat colouring / ilumination.To grant that faces are properly coloured when using flat ilumination
            // the "provoking index" of the two triangles of the face must me the same, in that case Vertex 3. Or at less they
            // should have the same normal value.

            // keep the index of the shell end cover first stored face
            iShellEndCoverFirstFaceIdx = model_mesh.Faces.Count();

            iAux = iShellEndCoverFirstVertexIdx;

            // generate the faces index
            for (iVertexIndex = 0; iVertexIndex < iVertexsPerRing; iVertexIndex++) {

                // face 1 
                faceAux = new Face();
                faceAux.Indices.Add(iAux + iVertexIndex); // v 0
                faceAux.Indices.Add(iAux + ((iVertexIndex + 1) % iVertexsPerRing)); // v 1                
                
                faceAux.Indices.Add(iAux + ((iVertexIndex + 1) % iVertexsPerRing) + iVertexsPerRing); // v 3  
                model_mesh.Faces.Add(faceAux);

                // face 2 
                faceAux = new Face();
                faceAux.Indices.Add(iAux + iVertexIndex + iVertexsPerRing); // v  2                
                faceAux.Indices.Add(iAux + iVertexIndex); // v 0
                
                faceAux.Indices.Add(iAux + ((iVertexIndex + 1) % iVertexsPerRing) + iVertexsPerRing); // v 3
                model_mesh.Faces.Add(faceAux);

            }//for (iRings=0;

            // keep the index of the shell end cover last stored face
            iShellEndCoverLastFaceIdx = model_mesh.Faces.Count() - 1;

        }//GenerateShellEndCoverFaces


        /*******************************************************************************
        * @brief method that generates/loads the shell model according to the parameters
        * set by the user
        *******************************************************************************/
        public void GenerateShellModel(ref Scene scene) {
            Mesh model_mesh;
            
            model_mesh = new Mesh("Shell", Assimp.PrimitiveType.Triangle);

            scene.Meshes.Add(model_mesh);

            // reset all the counters
            iVerticesCter = 0;
            iFacesCter = 0;
            iIndicesFacesCter = 0;

            // generate the shell generating curve vertices according to the configured parameters
            GetGeneratingCurveVertices();

            // in case AutoScale option is checked calculate the scale factors to apply
            CalculateAutoScaleFactors();

            // generate the vertexs and add them to the geometry
            GenerateOuterShellVertexs(ref model_mesh);
            // GenerateInnerShellVertexs(ref model_mesh);
            // GenerateShellBeginCoverVertexs(ref model_mesh);
            // GenerateShellEndCoverVertexs(ref model_mesh);

            // generate the normals and add them to the geometry
            GenerateOuterShellNormals(ref model_mesh);
            // GenerateInnerShellNormals(ref model_mesh);
            // GenerateShellBeginCoverNormals(ref model_mesh);
            // GenerateShellEndCoverNormals(ref model_mesh);

            // generate the faces combining their vertex indexs
            GenerateOuterShellFaces(ref model_mesh);
            // GenerateInnerShellFaces(ref model_mesh);
            // GenerateShellBeginCoverFaces(ref model_mesh);
            // GenerateShellEndCoverFaces(ref model_mesh);

        }//GenerateShellModel

        /*******************************************************************************
        * @brief exports the mesh of the object into a file with the specified name.
        * 
        * @param[in] strFileName with the name of the file where the object geometry data
        * will be exported.
        *******************************************************************************/
        public int export(string strFileName) {
            int retVal = 0;

            var context = new AssimpContext();

            // 1. Crear escena
            var scene = new Scene();

            // 2. Añadir la malla
            scene.Meshes.Add(liTransformedMesh[0]);

            // 3. Crear nodo raíz
            scene.RootNode = new Node("RootNode");

            // Asociar la malla al nodo (índice 0 porque es la primera)
            scene.RootNode.MeshIndices.Add(0);

            // 4. (Opcional pero recomendable) añadir un material
            var material = new Assimp.Material();
            scene.Materials.Add(material);
            liTransformedMesh[0].MaterialIndex = 0;

            // 5. Exportar (elige formato: "obj", "fbx", "stl", etc.)
            context.ExportFile(scene, strFileName, "obj");
           

            return retVal;

        }//export

        /*******************************************************************************
        * @brief method that generates/loads the object geometry and stores it into a 
        * mesh object
        *******************************************************************************/
        public override void InitializeGeometry() {
            var importer = new AssimpContext();
            // Acceder al primer Mesh (malla) del archivo
            int iAux = 0;
            // maximum and minimum coordinates values used to center the model
            float flMaxX = -10000, flMaxY = -10000, flMaxZ = -10000; // set a very small value in order to update it when a bigger value is found
            float flMinX = 10000, flMinY = 10000, flMinZ = 10000; // set a very big value in order to update it when a smalle value is found
            Assimp.Matrix4x4 tranlsationMatrix;
            Scene scene = null;
           
            // Configuración de carga (Post-Procesamiento)
            // * Triangulate: Convierte todo a triángulos (vital para OpenGL)
            // * FlipUVs: Voltea las texturas (OpenGL tiene el origen de textura abajo-izq,
            // la mayoría de formatos arriba-izq)
            // * GenerateNormals: Si el modelo no tiene normales, las calcula.
            var postProcessSteps = PostProcessSteps.Triangulate |
                                    PostProcessSteps.FlipUVs |
                                    PostProcessSteps.GenerateNormals;

            /*
            // use the AssimpNet Scheme object to load the model from the file
            //scene = importer.ImportFile("cono.obj", postProcessSteps); // 290
            scene = importer.ImportFile("TestOGL.obj", postProcessSteps); // 290

            if (scene == null || scene.SceneFlags.HasFlag(SceneFlags.Incomplete) || scene.RootNode == null) {
                Console.WriteLine("Error al cargar el modelo");
                return;
            }
            */

            scene = new Scene();
            GenerateShellModel(ref scene);

            objModelScale = 1.0f;

            liOriginaldMesh = new List<Mesh>();
            liOriginaldMesh.Add(new Mesh()); // create a new empty mesh object at index 0
            liOriginaldMesh[0] = object3D.CloneMesh(scene.Meshes[0]);// clone the object in the index 0 mesh

            liTransformedMesh = new List<Mesh>();
            liTransformedMesh.Add(new Mesh()); // create a new empty mesh object at index 0
            liTransformedMesh[0] = object3D.CloneMesh(liOriginaldMesh[0]);// clone the object in the index 0 mesh

            // process the model mesh to center it on 0,0,0 

            // get the Max and Min coordinates of the model vertices
            iVerticesCter = liTransformedMesh[0].Vertices.Count();
            for (iAux = 0; iAux < iVerticesCter; iAux++) {
                // update the maximum and mínimum coordinates values if necessary
                if (liTransformedMesh[0].Vertices[iAux].X > flMaxX) {
                    flMaxX = liTransformedMesh[0].Vertices[iAux].X;
                } else if (liTransformedMesh[0].Vertices[iAux].X < flMinX) {
                    flMinX = liTransformedMesh[0].Vertices[iAux].X;
                }
                if (liTransformedMesh[0].Vertices[iAux].Y > flMaxY) {
                    flMaxY = liTransformedMesh[0].Vertices[iAux].Y;
                } else if (liTransformedMesh[0].Vertices[iAux].Y < flMinY) {
                    flMinY = liTransformedMesh[0].Vertices[iAux].Y;
                }
                if (liTransformedMesh[0].Vertices[iAux].Z > flMaxZ) {
                    flMaxZ = liTransformedMesh[0].Vertices[iAux].Z;
                } else if (liTransformedMesh[0].Vertices[iAux].Z < flMinZ) {
                    flMinZ = liTransformedMesh[0].Vertices[iAux].Z;
                }
            }//for

            // use the Max and Min coordinatens to calculate the translation to center the model on
            // 0,0,0 and then configure the corresponding model translation matrix
            objModelTransX = -(flMaxX + flMinX) / 2;
            objModelTransY = -(flMaxY + flMinY) / 2;
            objModelTransZ = -(flMaxZ + flMinZ) / 2;

            tranlsationMatrix = Assimp.Matrix4x4.FromTranslation(new Assimp.Vector3D(objModelTransX, objModelTransY, objModelTransZ));

            // apply the transformations over each vertex of the mesh and store it in the list that will be used to update the VAO ( VBO and EBO )
            for (iAux = 0; iAux < iVerticesCter; iAux++) {
                // first apply the transformations
                liTransformedMesh[0].Vertices[iAux] = tranlsationMatrix * liTransformedMesh[0].Vertices[iAux];
                liTransformedMesh[0].Normals[iAux] = tranlsationMatrix * liTransformedMesh[0].Normals[iAux];
            }//for

        }//Initialize

        /*******************************************************************************
        * @brief applies the transformation over the object mesh vertices and faces and 
        * then updates that information in the VAO ( VBO and EBO )
        *******************************************************************************/
        public override void TransformUpdateVAO() {

            // maximum and minimum coordinates values used to center the model
            // float flMaxX = -10000, flMaxY = -10000, flMaxZ = -10000; // set a very small value in order to update it when a bigger value is found
            // float flMinX = 10000, flMinY = 10000, flMinZ = 10000; // set a very big value in order to update it when a smalle value is found
            int iStride = 0;
            Assimp.Matrix4x4 rotationMatrix;
            List<float> modelCoordNColors;
            List<uint> modelFaceIdxs;
            int iAux = 0;

            // ### update the content of the lists that will be used to intialize the VAO ( VBO and EBO ) 
            // with the model data while apply the transformations set by the user

            // calculate the model rotation matrix that allows to rotate the model arround the current view vector with the 
            // angles set by the user when clicking an draggin over OGL render control. The view vector is defined by the CameraUp
            // and the CameraRight vectors. The rotation is achived using Quaternions from the OpenTK.Mathematics lib wich also
            // allow rotating the object arround each axis and then combining then in a single rotation matrix.
            // OpenTK.Mathematics.Quaternion rotArroundCamUpAxis = OpenTK.Mathematics.Quaternion.FromAxisAngle(cameraUpVector, MathHelper.DegreesToRadians((float)-refAppViewModel.YOrientation));
            // OpenTK.Mathematics.Quaternion rotArroundCamRightAxis = OpenTK.Mathematics.Quaternion.FromAxisAngle(cameraRightVector, MathHelper.DegreesToRadians((float)-refAppViewModel.XOrientation));
            // commbine both axis rotations into a single Quaternion
            // OpenTK.Mathematics.Quaternion rotCombined = rotArroundCamUpAxis * rotArroundCamRightAxis;
            // nomralize the combined Quaternion
            // rotCombined = rotCombined.Normalized();
            // get the rotation matrix that corresponds to the obtained Quaternion
            // OpenTK.Mathematics.Matrix4 rotMatrix = Matrix4.CreateFromQuaternion(rotCombined);

            // to multiply the OpenTK Rotation Matrix4 with the Assimp.net vertices we must convert the
            // obtained OpenTK.Mathematics.Matrix4 matrix to an Assimp.Matrix4x4 matrix.            
            // rotationMatrix = new Assimp.Matrix4x4(
            //     rotMatrix.M11, rotMatrix.M12, rotMatrix.M13, rotMatrix.M14,
            //     rotMatrix.M21, rotMatrix.M22, rotMatrix.M23, rotMatrix.M24,
            //     rotMatrix.M31, rotMatrix.M32, rotMatrix.M33, rotMatrix.M34,
            //     rotMatrix.M41, rotMatrix.M42, rotMatrix.M43, rotMatrix.M44
            // );
            // 
            // // reset the rotation angles previously set by the user when clickng and draggin on the screen.
            // refAppViewModel.XOrientation = 0;
            // refAppViewModel.YOrientation = 0;
            // refAppViewModel.ZOrientation = 0;

            // 3 vertex floats   + 3 color floats  + 3 normal floats
            modelCoordNColors = new List<float>();

            // apply the obtained rotation matrix over the vertices and the normals of the model           
            // iVerticesCter = liTransformedMesh[0].Vertices.Count();
            // for (iAux = 0; iAux < iVerticesCter; iAux++) {
            //     // first apply the transformations
            //     liTransformedMesh[0].Vertices[iAux] = rotationMatrix * liTransformedMesh[0].Vertices[iAux];
            //     liTransformedMesh[0].Normals[iAux] = rotationMatrix * liTransformedMesh[0].Normals[iAux];
            // }//for

            // get the Max and Min coordinates of the model vertices
            // for (iAux = 0; iAux < iModelTotalVertices; iAux++) {
            //     // update the maximum and mínimum coordinates values if necessary
            //     if (modelTransformedMesh.Vertices[iAux].X > flMaxX) {
            //         flMaxX = modelTransformedMesh.Vertices[iAux].X;
            //     } else if (modelTransformedMesh.Vertices[iAux].X < flMinX) {
            //         flMinX = modelTransformedMesh.Vertices[iAux].X;
            //     }
            //     if (modelTransformedMesh.Vertices[iAux].Y > flMaxY) {
            //         flMaxY = modelTransformedMesh.Vertices[iAux].Y;
            //     } else if (modelTransformedMesh.Vertices[iAux].Y < flMinY) {
            //         flMinY = modelTransformedMesh.Vertices[iAux].Y;
            //     }
            //     if (modelTransformedMesh.Vertices[iAux].Z > flMaxZ) {
            //         flMaxZ = modelTransformedMesh.Vertices[iAux].Z;
            //     } else if (modelTransformedMesh.Vertices[iAux].Z < flMinZ) {
            //         flMinZ = modelTransformedMesh.Vertices[iAux].Z;
            //     }
            // }//for

            // use the Max and Min coordinatens to calculate the center of the model and the corresponding model translation matrix
            // objModelTransX = -((flMaxX + flMinX) / 2f);
            // objModelTransY = -((flMaxY + flMinY) / 2f);
            // objModelTransZ = -((flMaxZ + flMinZ) / 2f);

            // Assimp.Matrix4x4 tranlsationMatrix = Matrix4x4.FromTranslation(new Vector3D(objModelTransX, objModelTransY, objModelTransZ));

            // assign the mesh vertices to the list that will be used to initialize the VAO 
            for (iAux = 0; iAux < iVerticesCter; iAux++) {
                // first apply the translation transformation
                // modelTransformedMesh.Vertices[iAux] = tranlsationMatrix * modelTransformedMesh.Vertices[iAux];
                // modelTransformedMesh.Normals[iAux] = tranlsationMatrix * modelTransformedMesh.Normals[iAux];

                // each face has 3 vertices + 3 colors + 3 normals
                // vertex coords centered in the middle (flMax... - flMin...) / 2;
                modelCoordNColors.Add((float)liTransformedMesh[0].Vertices[iAux].X);
                modelCoordNColors.Add((float)liTransformedMesh[0].Vertices[iAux].Y);
                modelCoordNColors.Add((float)liTransformedMesh[0].Vertices[iAux].Z);
                // color
                modelCoordNColors.Add(((float)refAppViewModel.MeshColor.R) / 255f);
                modelCoordNColors.Add(((float)refAppViewModel.MeshColor.G) / 255f);
                modelCoordNColors.Add(((float)refAppViewModel.MeshColor.B) / 255f);

                // normal
                modelCoordNColors.Add((float)liTransformedMesh[0].Normals[iAux].X);
                modelCoordNColors.Add((float)liTransformedMesh[0].Normals[iAux].Y);
                modelCoordNColors.Add((float)liTransformedMesh[0].Normals[iAux].Z);
            }//for

            // faces indexs list for the EBO
            // add to the list of the indexs of the vertexs that define the faces of the mode
            modelFaceIdxs = new List<uint>();
            iFacesCter = liTransformedMesh[0].Faces.Count();
            foreach (var face in liTransformedMesh[0].Faces) {
                // each face has 3 index because we us PostProcessSteps.Triangulate
                modelFaceIdxs.Add((uint)face.Indices[0]);
                modelFaceIdxs.Add((uint)face.Indices[1]);
                modelFaceIdxs.Add((uint)face.Indices[2]);

                iIndicesFacesCter = iIndicesFacesCter + 3;

            }
          
            // #### initalize the model VAO (VBO and EBO)  with the model attributes, vertices and faces in the lists

            objectVAO = GL.GenVertexArray();
            objectVBO = GL.GenBuffer();
            objectEBO = GL.GenBuffer();

            // set the VAO in context
            GL.BindVertexArray(objectVAO);

            // set in context and initalize the VBO
            GL.BindBuffer(BufferTarget.ArrayBuffer, objectVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, modelCoordNColors.Count() * sizeof(float), modelCoordNColors.ToArray(), BufferUsageHint.StaticDraw);

            // set in context and initalize the EBO
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, objectEBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, modelFaceIdxs.Count() * sizeof(uint), modelFaceIdxs.ToArray(), BufferUsageHint.StaticDraw);

            iStride = (3 + 3 + 3) * sizeof(float); // pos (3) + color (3) + normal (3)

            // configure the position atributes
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, iStride, 0);
            GL.EnableVertexAttribArray(0);

            // configure the color atributes
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, iStride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            // configure the attributes of the normals
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, iStride, 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);

            // clear the context
            GL.BindVertexArray(0);

        }//TransformUpdateVAO

    }//cShellModel

    /*******************************************************************************
    * @brief class that implements the methods of the shell 3D model.
    *******************************************************************************/
    public class cZYXaxis : object3D {

        // the three vectors defined by the view vector
        public Vector3 cameraRight;
        public Vector3 cameraUp;
        public Vector3 cameraForward;

        /*******************************************************************************
        * @brief Constructor with pararameters.
        * @param[in] _refViewModel reference to the VieModel object that will be used 
        * to retrive some information.
        *******************************************************************************/
        public cZYXaxis(ViewModel _refViewModel) {

            refAppViewModel = _refViewModel;

        }//object3D

        /*******************************************************************************
        * @brief method that generates/loads the object geometry and stores it into a 
        * mesh object
        *******************************************************************************/
        public override void InitializeGeometry() {
            var importer = new AssimpContext();
            // Mesh mesh = null;
            Scene scene = null;

            // Configuración de carga (Post-Procesamiento)
            // * Triangulate: Convierte todo a triángulos (vital para OpenGL)
            // * FlipUVs: Voltea las texturas (OpenGL tiene el origen de textura abajo-izq,
            // la mayoría de formatos arriba-izq)
            // * GenerateNormals: Si el modelo no tiene normales, las calcula.
            var postProcessSteps = PostProcessSteps.Triangulate |
                                    PostProcessSteps.FlipUVs |
                                    PostProcessSteps.GenerateNormals;

            // use the AssimpNet Scheme object to load the model from the file
            scene = importer.ImportFile("Axis.obj", postProcessSteps);

            if (scene == null || scene.SceneFlags.HasFlag(SceneFlags.Incomplete) || scene.RootNode == null) {
                Console.WriteLine("Error loading the XYZ axis model.");
                return;
            }

            // modelOriginalZYXAxis is a list of 3 mehses. Each mesh corresponds to an axis. 
            liOriginaldMesh = scene.Meshes;

            objModelScale = 0.15f;
            objModelTransX = -0.5f;
            objModelTransY = -0.5f;
            objModelTransZ = -0.5f;

         }//Initialize

        /*******************************************************************************
        * @brief applies the transformation over the object mesh vertices and faces and 
        * then updates that information in the VAO ( VBO and EBO )
        *******************************************************************************/
        public override void TransformUpdateVAO() {
            int iStride = 0;
            float fR = 0, fG = 0, fB = 0;
            List<float> axisCoordNColors = null;
            List<uint> axisFaceIdxs = null;
            Mesh mesh;
            int iVertexCounterAux = 0; // used to calculate and initialize the face index of the faces of the different meshes in a single mesh
            int iAux = 0;
            int iAux2 = 0;


            // ### update the content of the lists that will be used to intialize the VAO ( VBO and EBO )

            // 3 vertex floats   + 3 color floats  + 3 normal floats
            // add to the list of vertexs the coordinates of the 3D XYZ Axis
            axisCoordNColors = new List<float>();
            iVerticesCter = 0;
            for (iAux = 0; iAux < 3; iAux++) {

                mesh = liOriginaldMesh[iAux];

                // assign a different color to each mesh of the object ( each mesh is an arrow of the axis)
                if (iAux == 0) { fR = 0; fG = 0; fB = 1; }
                if (iAux == 1) { fR = 0; fG = 1; fB = 0; }
                if (iAux == 2) { fR = 1; fG = 0; fB = 0; }

                for (iAux2 = 0; iAux2 < mesh.Vertices.Count(); iAux2++) {
                    // each face has 3 vertices + 3 colors + 3 normals
                    // vertex coords
                    axisCoordNColors.Add((float)mesh.Vertices[iAux2].X);
                    axisCoordNColors.Add((float)mesh.Vertices[iAux2].Y);
                    axisCoordNColors.Add((float)mesh.Vertices[iAux2].Z);
                    // color
                    axisCoordNColors.Add((float)fR);
                    axisCoordNColors.Add((float)fG);
                    axisCoordNColors.Add((float)fB);
                    // normal
                    axisCoordNColors.Add((float)mesh.Normals[iAux2].X);
                    axisCoordNColors.Add((float)mesh.Normals[iAux2].Y);
                    axisCoordNColors.Add((float)mesh.Normals[iAux2].Z);
                }//for

                iVerticesCter = iVerticesCter + mesh.Vertices.Count();

            }//for

            // faces indexs
            // add to the list the indexs that define the faces of the 3D XYZ Axis
            axisFaceIdxs = new List<uint>();
            iFacesCter = 0; 
            iIndicesFacesCter = 0;
            iVertexCounterAux = 0;
            for (iAux = 0; iAux < 3; iAux++) {

                mesh = liOriginaldMesh[iAux];
                iFacesCter = iFacesCter + mesh.Faces.Count();

                for (iAux2 = 0; iAux2 < mesh.Faces.Count(); iAux2++) {

                    // each face has 3 vertex indexs because we us PostProcessSteps.Triangulate
                    axisFaceIdxs.Add((uint)(iVertexCounterAux + mesh.Faces[iAux2].Indices[0]));
                    axisFaceIdxs.Add((uint)(iVertexCounterAux + mesh.Faces[iAux2].Indices[1]));
                    axisFaceIdxs.Add((uint)(iVertexCounterAux + mesh.Faces[iAux2].Indices[2]));

                    iIndicesFacesCter = iIndicesFacesCter + 3;

                }//for

                iVertexCounterAux = iVertexCounterAux + mesh.Vertices.Count();

            }//for
  
            // #### initalize the ZYXaxis VAO (VBO and EBO)  with the attributes, vertices and faces in the lists

            objectVAO = GL.GenVertexArray();
            objectVBO = GL.GenBuffer();
            objectEBO = GL.GenBuffer();

            // set the VAO in context
            GL.BindVertexArray(objectVAO);

            // set in context and initalize the VBO
            GL.BindBuffer(BufferTarget.ArrayBuffer, objectVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, axisCoordNColors.Count * sizeof(float), axisCoordNColors.ToArray(), BufferUsageHint.StaticDraw);

            // set in context and initalize the EBO
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, objectEBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, axisFaceIdxs.Count * sizeof(uint), axisFaceIdxs.ToArray(), BufferUsageHint.StaticDraw);

            iStride = (3 + 3 + 3) * sizeof(float); // pos (3) + color (3) + normal (3)

            // configure the position atributes
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, iStride, 0);
            GL.EnableVertexAttribArray(0);

            // configure the color atributes
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, iStride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            // configure the attributes of the normals
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, iStride, 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);

            // clear the context
            GL.BindVertexArray(0);

        }//TransformUpdateVAO

    }//cZYXaxis

}
