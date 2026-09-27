using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Core;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Wpf;
using System.Diagnostics;
using System.Windows.Markup;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Assimp;

namespace RaupCoilerII {

    /*******************************************************************************
    * @brief class that implements the methods of the shell 3D model. 
    *******************************************************************************/
    public class cShellModel : object3D {

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

        // variables used to store the maximum and minimum values of the X,Y,Z coordinates to 
        // calculate the scale factor and the translation to fit and center the model in the screen
        public float flMinX, flMaxX;
        public float flMinY, flMaxY;
        public float flMinZ, flMaxZ;
        public float flRangeX, flRangeY, flRangeZ;
        public float flBigestRange;

        // list that stores the vertices of the generating ring/shape
        List<Assimp.Vector2D> li2DVertexsGenCurve;

        /*******************************************************************************
        * @brief Constructor with pararameters.
        * @param[in] _refViewModel reference to the VieModel object that will be used 
        * to retrive and update information to the user interface and program data
        *******************************************************************************/
        public cShellModel(ViewModel _refViewModel) {

            refAppViewModel = _refViewModel;

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

            li2DVertexsGenCurve = null;

        }//cShellModel

        /*******************************************************************************
        * @brief initializes the list of the vertices of the generating curve/ring 
        * used to generate the shell 3D model. The generating curve/ring is initialized
        * according to the configured parameters ( S, Resolution ... )
        *******************************************************************************/
        private void GetGeneratingCurveVertices() {
            Assimp.Vector2D vector2DAux1;
            float fCurrRingVertexAngleRad = 0;
            float fXAux, fYAux, fZAux;
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
                fXAux = ((float)Math.Cos((double)fCurrRingVertexAngleRad) * (float)refAppViewModel.IniRadius);
                fYAux = ((float)Math.Sin((double)fCurrRingVertexAngleRad) * (float)refAppViewModel.IniRadius * (float)refAppViewModel.Sparam);

                // store the current vertex of the generated shape into the list
                vector2DAux1 = new Assimp.Vector2D(fXAux, fYAux);
                li2DVertexsGenCurve.Add(vector2DAux1);

                fCurrRingVertexAngleRad = fCurrRingVertexAngleRad + flAngleVerticesRad;

            }//for

        }//GetGeneratingCurveVertices

        /*******************************************************************************
        * @brief method that calculates and updates the scale factor and translation vector
        * used to fit and center the shell model into the screen when the AutoScale option 
        * is checked. It generates a simplified version of the shell model with the configured
        * Raup parameters in order to get the maximum and minimum X,Y,Z coordinates values.
        * 
        * @note the shell Raup Michelson and the other genration parameters should have 
        * been set before calling that method because they are necessary to get the 
        * dimensions of the model.
        *******************************************************************************/
        private void CalculateTranslationAndScaleFactors() {
            // variables used to calculate the generating curve
            List<Assimp.Vector2D> liBasicGenCurveVertices; // vertices of the simple generating curve used to calculate the maximum and minimum X,Y and Z coordinates
            float flAngleVerticesRad = 0; // angle between successive vertices of the ring, in radians
            Assimp.Vector2D vector2DAux1;
            int iNumVerticesGenCurve = 4;

            // variables used to generate the simple shell model            
            int iRingsPerRevolution = 36;
            int iRing = 0;
            int iVertexIndex = 0;
            int iTotalNumberOfRings = 0; // the total number of rings in the shell model
            float fCurrRingVertexAngleRad = 0;
            double dAngleRingsRad = 0; // angle between each successive ring/generating curve, in radians
            double dThetaRad = 0;// theta angle used to generate the complete shell model, in radians
            double dRc = 0;

            double dAux = 0;
            float fXAux, fYAux, fZAux;
            double dXAux, dYAux, dZAux;

            // ## 2D Simplified generating curve

            // create and calculate the generating curve according to the selected Raup & Michelson parameters
            liBasicGenCurveVertices = new List<Vector2D>();

            flAngleVerticesRad = (2.0f * (float)Math.PI) / (float)iNumVerticesGenCurve;
            fCurrRingVertexAngleRad = 0;
            for (iVertexIndex = 0; iVertexIndex < iNumVerticesGenCurve; iVertexIndex++) {

                // generate current ring vertex X, and Y coordinates for the genrating shape
                fXAux = ((float)Math.Cos((double)fCurrRingVertexAngleRad) * (float)refAppViewModel.IniRadius);
                fYAux = ((float)Math.Sin((double)fCurrRingVertexAngleRad) * (float)refAppViewModel.IniRadius * (float)refAppViewModel.Sparam);

                // store the current vertex of the generated shape into the list
                vector2DAux1 = new Assimp.Vector2D(fXAux, fYAux);
                liBasicGenCurveVertices.Add(vector2DAux1);

                fCurrRingVertexAngleRad = fCurrRingVertexAngleRad + flAngleVerticesRad;

            }//for

            // ## 3D Simplified shell model

            // create and calculate the simplified shell 3D model according to the selected Raup & Michelson
            // parameters and get the maximum and minimum X, Y, Z values 

            // initialize the variables used to get the maximum and minimum values
            flMaxX = -10000; flMaxY = -10000; flMaxZ = -10000; // set a very small value in order to update it when a bigger value is found
            flMinX = 10000; flMinY = 10000; flMinZ = 10000; // set a very big value in order to update it when a smalle value is found
            flBigestRange = 0;

            // calculate some of the parameters needed to generate the shell 3D model
            iTotalNumberOfRings = refAppViewModel.Revolutions * iRingsPerRevolution; // rings in the complete model: is the number of rings  per revolution multiplied by the number of revolutions
            dAngleRingsRad = ((double)refAppViewModel.Revolutions * (2.0d * (double)Math.PI)) / (double)iTotalNumberOfRings;

            // calculate the Rc value according to the confiugred D and fRadius parameter
            dRc = (double)refAppViewModel.IniRadius * (1.0d + (double)refAppViewModel.Dparam) / (1.0d - (double)refAppViewModel.Dparam);

            dThetaRad = 0;
            for (iRing = 0; iRing < iTotalNumberOfRings; iRing++) {

                // calculate the W^(𝜃/2π) for the current Theta value
                dAux = (double)Math.Pow((double)refAppViewModel.Wparam, (double)(dThetaRad / (double)(2.0d * Math.PI)));// W^(1/refAppViewModel.RingsRev)

                // scale the current "dThetaRad" ring: calculate all the vertices of the current ring of the shell
                for (iVertexIndex = 0; iVertexIndex < iNumVerticesGenCurve; iVertexIndex++) {

                    // apply the "Raup & Michelson" model parameters on the generating curve
                    dXAux = ((double)liBasicGenCurveVertices[iVertexIndex].X + dRc) * dAux;
                    dYAux = ((double)liBasicGenCurveVertices[iVertexIndex].Y * dAux) - (dRc * (double)refAppViewModel.Tparam * (dAux - 1.0d));
                    dZAux = 0;

                    // apply the corresponding "dThetaRad" rotation over the transformed generating curve. This 
                    // converts from cylindrical coordinates to cartesian coordinates ( r,y,theta -> x,y,z )
                    fXAux = (float)((dXAux * Math.Cos(-dThetaRad)) + (dZAux * Math.Sin(-dThetaRad)));
                    fYAux = (float)dYAux;
                    fZAux = (float)(-(dXAux * Math.Sin(-dThetaRad)) + (dZAux * Math.Cos(-dThetaRad)));

                    // check if the processed vertex coordinates correspond to a maximum or minimum X,Y or Z value
                    if (fXAux < flMinX) { flMinX = fXAux; }
                    if (fXAux > flMaxX) { flMaxX = fXAux; }
                    if (fYAux < flMinY) { flMinY = fYAux; }
                    if (fYAux > flMaxY) { flMaxY = fYAux; }
                    if (fZAux < flMinZ) { flMinZ = fZAux; }
                    if (fZAux > flMaxZ) { flMaxZ = fZAux; }

                }//for

                // increase the Theta angle of the following shell model ring
                dThetaRad = dThetaRad + dAngleRingsRad;
            
            }//for

            // calculate the scale factor to apply to the shell model to fit in the frustrum/screen
            if (!refAppViewModel.Autoscale) {
                objModelScale = 1.0f;
            } else {
                // use the biggest range to calculate the model scale factor
                flRangeX = flMaxX - flMinX;
                flRangeY = flMaxY - flMinY;
                flRangeZ = flMaxZ - flMinZ;
                // get the biggest of the range
                flBigestRange = flRangeX;
                if (flRangeY > flBigestRange) flBigestRange = flRangeY;
                if (flRangeZ > flBigestRange) flBigestRange = flRangeZ;
                // calculate the scale factor to fit the shell model coordinates into a 2x2x2 cube
                objModelScale = (float)(2.0f / flBigestRange);
            }//if

            // calculate the translation vector: use the max and min coordinatens to
            // calculate the translation necessary to center  the model on 0,0,0 and
            // then configure the corresponding model translation matrix
            objModelTransX = (-(flMaxX + flMinX) / 2.0f) * objModelScale;
            objModelTransY = (-(flMaxY + flMinY) / 2.0f) * objModelScale;
            objModelTransZ = (-(flMaxZ + flMinZ) / 2.0f) * objModelScale;

        }//CalculateTranslationAndScaleFactors


        /*******************************************************************************
        * @brief generates all the vertexs of the outer shell geometry ( the faces that
        * point outside ).
        * 
        * @param[in|out] model_mesh with all the generated vertexs are stored into that object.
        *******************************************************************************/
        private void GenerateOuterShellVertexs(ref Mesh model_mesh) {           
            int iRing = 0;
            int iVertexIndex = 0;
            int iVerticesPerGenCurve = 0; // the number of vertices on each shell ring
            int iTotalNumberOfRings = 0; // the total number of rings in the shell model
            double dAngleRingsRad = 0; // angle between each successive ring/generating curve, in radians
            double dThetaRad = 0;// theta angle used to generate the complete shell model, in radians
            double dRc = 0;
            // auxiliar variables
            Assimp.Vector3D vectorAux1;
            float fXAux, fYAux, fZAux;
            double dXAux, dYAux, dZAux;
            double dFactAuxA = 0;
            double dFactAuxB = 0;
            double dFactAuxC = 0;


            // calculate some parameters that will be used to generate the shell 3D model
            iTotalNumberOfRings = refAppViewModel.Revolutions * refAppViewModel.RingsRev; // rings in the complete model: is the number of rings per revolution multiplied by the number of revolutions
            dAngleRingsRad = ((double)refAppViewModel.Revolutions * (2.0d * (double)Math.PI)) / (double)iTotalNumberOfRings;
            iVerticesPerGenCurve = li2DVertexsGenCurve.Count();

            // keep the index of the shell outer mesh first stored vertex
            iOuterShellFirstVertexIdx = model_mesh.Vertices.Count();

            // calculate the Rc value according to the confiugred D and fRadius parameter
            dRc = (double)refAppViewModel.IniRadius * (1.0d + (double)refAppViewModel.Dparam) / (1.0d - (double)refAppViewModel.Dparam);

            dThetaRad = 0;
            for (iRing = 0; iRing < iTotalNumberOfRings; iRing++) {

                // precalculate various factors to simplify computations within the next ring loop
                // calculate the W^(𝜃/2π) for the current Theta value
                dFactAuxA = (double)Math.Pow((double)refAppViewModel.Wparam, (double)(dThetaRad / (double)(2.0d * Math.PI)));// W^(1/refAppViewModel.RingsRev)
                dFactAuxB = dFactAuxA * dRc;
                dFactAuxC = (dRc * (double)refAppViewModel.Tparam * (dFactAuxA - 1.0d));

                // scale the current "dThetaRad" ring: calculate all the vertices of the current ring of the shell
                for (iVertexIndex = 0; iVertexIndex < iVerticesPerGenCurve; iVertexIndex++) {

                    // apply the "Raup & Michelson" model parameters on the generating curve
                    dXAux = ((double)li2DVertexsGenCurve[iVertexIndex].X * dFactAuxA) + dFactAuxB;// optimization of: ((double)li2DVertexsGenCurve[iVertexIndex].X + dRc) * dFactAuxA;
                    dYAux = ((double)li2DVertexsGenCurve[iVertexIndex].Y * dFactAuxA) - dFactAuxC;// optimization of: ((double)li2DVertexsGenCurve[iVertexIndex].Y * dFactAuxA) - (dRc * (double)refAppViewModel.Tparam * (dFactAuxA - 1.0d));
                    dZAux = 0;

				    // apply the corresponding "dThetaRad" rotation over the transformed generating curve. This 
				    // converts from cylindrical coordinates to cartesian coordinates ( r,y,theta -> x,y,z )
				    fXAux = (float)((dXAux * Math.Cos(-dThetaRad)) + (dZAux * Math.Sin(-dThetaRad)));
				    fYAux = (float)dYAux;
				    fZAux = (float)(-(dXAux * Math.Sin(-dThetaRad)) + (dZAux * Math.Cos(-dThetaRad)));

                    // apply the previously calculated translation and scale factor
                    fXAux = fXAux * objModelScale + objModelTransX;
                    fYAux = fYAux * objModelScale + objModelTransY;
                    fZAux = fZAux * objModelScale + objModelTransZ;

                    // store the calculated vertex coordinates into the mesh structure
                    vectorAux1 = new Assimp.Vector3D(fXAux, fYAux, fZAux);
                    model_mesh.Vertices.Add(vectorAux1);

                }//for

                // increase the Theta angle of the following shell model ring
                dThetaRad = dThetaRad + dAngleRingsRad;

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
        private void GenerateInnerShellVertexs(ref Mesh model_mesh) {
            int iRing = 0;
            int iVertexIndex = 0;
            int iVerticesPerGenCurve = 0; // the number of vertices on each shell ring
            int iTotalNumberOfRings = 0; // the total number of rings in the shell model
            double dAngleRingsRad = 0; // angle between each successive ring/generating curve, in radians
            double dThetaRad = 0;// theta angle used to generate the complete shell model, in radians
            double dRc = 0;
            // auxiliar variables
            double dThicknessFactor = 1.0d;
            Assimp.Vector3D vectorAux1;
            float fXAux, fYAux, fZAux;
            double dXAux, dYAux, dZAux;
            double dFactAuxA = 0;
            double dFactAuxB = 0;
            double dFactAuxC = 0;

            // calculate some parameters that will be used to generate the shell 3D model
            iTotalNumberOfRings = refAppViewModel.Revolutions * refAppViewModel.RingsRev; // rings in the complete model: is the number of rings per revolution multiplied by the number of revolutions
            dAngleRingsRad = ((double)refAppViewModel.Revolutions * (2.0d * (double)Math.PI)) / (double)iTotalNumberOfRings;
            iVerticesPerGenCurve = li2DVertexsGenCurve.Count();
            dThicknessFactor = (double)((100.0d - refAppViewModel.ShellWidthPercent) / 100.0d);

            // keep the index of the shell inner mesh first stored vertex
            iInnerShellFirstVertexIdx = model_mesh.Vertices.Count();

            // calculate the Rc value according to the confiugred D and fRadius parameter
            dRc = (double)refAppViewModel.IniRadius * (1.0d + (double)refAppViewModel.Dparam) / (1.0d - (double)refAppViewModel.Dparam);

            dThetaRad = 0;
            for (iRing = 0; iRing < iTotalNumberOfRings; iRing++) {

                // precalculate various factors to simplify computations within the next ring loop
                // calculate the W^(𝜃/2π) for the current Theta value
                dFactAuxA = (double)Math.Pow((double)refAppViewModel.Wparam, (double)(dThetaRad / (double)(2.0d * Math.PI)));// W^(1/refAppViewModel.RingsRev)
                dFactAuxB = dFactAuxA * dRc;
                dFactAuxC = (dRc * (double)refAppViewModel.Tparam * (dFactAuxA - 1.0d));

                // scale the current "dThetaRad" ring: calculate all the vertices of the current ring of the shell
                for (iVertexIndex = 0; iVertexIndex < iVerticesPerGenCurve; iVertexIndex++) {

                    // apply the "Raup & Michelson" model parameters on the generating curve. In order to achive 
                    // the desired shell thickness we apply the configured thickness percentage over the generating curve.
                    dXAux = ((double)li2DVertexsGenCurve[iVertexIndex].X * dThicknessFactor * dFactAuxA) + dFactAuxB;// optimization of: ((double)li2DVertexsGenCurve[iVertexIndex].X* dThicknessFactor + dRc) * dFactAuxA;
                    dYAux = ((double)li2DVertexsGenCurve[iVertexIndex].Y * dThicknessFactor * dFactAuxA) - dFactAuxC;// optimization of: ((double)li2DVertexsGenCurve[iVertexIndex].Y * dThicknessFactor * dFactAuxA) - (dRc * (double)refAppViewModel.Tparam * (dFactAuxA - 1.0d));
                    dZAux = 0;

                    // apply the corresponding "dThetaRad" rotation over the transformed generating curve. This 
                    // converts from cylindrical coordinates to cartesian coordinates ( r,y,theta -> x,y,z )
                    fXAux = (float)((dXAux * Math.Cos(-dThetaRad)) + (dZAux * Math.Sin(-dThetaRad)));
                    fYAux = (float)dYAux;
                    fZAux = (float)(-(dXAux * Math.Sin(-dThetaRad)) + (dZAux * Math.Cos(-dThetaRad)));

                    // apply the previously calculated translation and scale factor
                    fXAux = fXAux * objModelScale + objModelTransX;
                    fYAux = fYAux * objModelScale + objModelTransY;
                    fZAux = fZAux * objModelScale + objModelTransZ;

                    // store the calculated vertex coordinates into the mesh structure
                    vectorAux1 = new Assimp.Vector3D(fXAux, fYAux, fZAux);
                    model_mesh.Vertices.Add(vectorAux1);

                }//for

                // increase the Theta angle of the following shell model ring
                dThetaRad = dThetaRad + dAngleRingsRad;

                // apply the W param on the next ring radius
                // fCurrRingRadius = fCurrRingRadius * flWRing;

            }//for (iRings=0;

            // keep the index of the shell inner mesh last stored vertex
            iInnerShellLastVertexIdx = model_mesh.Vertices.Count() - 1;

        }//GenerateInnerShellVertexs

        /*******************************************************************************
        * @brief creates the vertex used to define the beggining edge of the shell that
        * covers the space between the the outer and the inner shell. The vertexs of the 
        * begining cover are the same vertexs than the first ring of the outer and inner
        * shell mesh.
        * 
        * @param[in|out] model_mesh with all the generated vertexs are stored into that object.
        *******************************************************************************/
        private void GenerateShellBeginCoverVertexs(ref Mesh model_mesh) {
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
            iShellBeginCoverLastVertexIdx = model_mesh.Vertices.Count() - 1;

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
        private void GenerateOuterShellNormals(ref Mesh model_mesh) {
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

                iAux = iOuterShellFirstVertexIdx + iRing * iVertexsPerRing;// index of the first vertex of the current processed ring

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
            iOuterShellLastNormalIdx = model_mesh.Normals.Count() - 1;

        }//GenerateOuterShellNormals

        /*******************************************************************************
        * @brief generates the normals for all the vertices of the inner shell geometry.
        * ( the faces that point inside ).
        * 
        * @param[in|out] model_mesh with all the geometry information. Here is where
        * the generated vertex, normals, and faces information is stored
        *******************************************************************************/
        private void GenerateInnerShellNormals(ref Mesh model_mesh) {
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

            for (iRing = 0; iRing < (iNumberOfRings - 1); iRing++) {

                iAux = iInnerShellFirstVertexIdx + iRing * iVertexsPerRing;// index of the first vertex of the current processed ring

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

                    // get the vector that joins the current vertex with the equivalent vertex in the next face
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
            iInnerShellLastNormalIdx = model_mesh.Normals.Count() - 1;

        }//GenerateInerShellNormals

        /*******************************************************************************
        * @brief generates the normals for all the vertices of the shell begin cover
        * 
        * @param[in|out] model_mesh with all the geometry information. Here is where
        * the generated vertex, normals, and faces information is stored
        *******************************************************************************/
        private void GenerateShellBeginCoverNormals(ref Mesh model_mesh) {
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
            iShellBeginCoverLastNormalIdx = model_mesh.Normals.Count() - 1;

        }//GenerateShellBeginCoverNormals

        /*******************************************************************************
        * @brief generates the normals for all the vertices of the shell end cover
        * 
        * @param[in|out] model_mesh with all the geometry information. Here is where
        * the generated vertex, normals, and faces information is stored
        *******************************************************************************/
        private void GenerateShellEndCoverNormals(ref Mesh model_mesh) {
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
            iShellEndCoverLastNormalIdx = model_mesh.Normals.Count() - 1;

        }//GenerateSpiralEndCoverNormals

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
        private void GenerateOuterShellFaces(ref Mesh model_mesh) {
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
                    faceAux.Indices.Add(iAux + iVertexIndex); // v 0                    
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
            iOuterShellLastFaceIdx = model_mesh.Faces.Count() - 1;

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
        private void GenerateInnerShellFaces(ref Mesh model_mesh) {
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
                    faceAux.Indices.Add(iAux + iVertexIndex + 1); // v 1
                    faceAux.Indices.Add(iAux + iVertexIndex); // v 0                   
                    faceAux.Indices.Add(iAux + iVertexIndex + 1 + iVertexsPerRing); // v 3  <= "provoking index"
                    model_mesh.Faces.Add(faceAux);

                    // face 2 
                    faceAux = new Face();
                    faceAux.Indices.Add(iAux + iVertexIndex); // v 0 
                    faceAux.Indices.Add(iAux + iVertexIndex + iVertexsPerRing); // v 2                                       
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
                //   face 1: 3 1 0
                //   face 2: 3 0 2
                // IMPORTANT: "provoking index" is the last index of the face, and is the one that "provokes" the color of the face
                // when using flat colouring / ilumination.To grant that faces are properly coloured when using flat ilumination
                // the "provoking index" of the two triangles of the face must me the same, in that case Vertex 3. Or at less they
                // should have the same normal value.

                // last face 1 of the ring
                faceAux = new Face();
                faceAux.Indices.Add(iAux);// v 1
                faceAux.Indices.Add(iAux + iVertexIndex);// v 0 
                faceAux.Indices.Add(iAux + iVertexsPerRing);// v 3 <= "provoking index" ( despite idx 0 and idx 2 are not the same index, they have the same normal, so the color of the faces is properly shown )
                model_mesh.Faces.Add(faceAux);

                // last face 2 of the ring
                faceAux = new Face();
                faceAux.Indices.Add(iAux + iVertexIndex);// v 0
                faceAux.Indices.Add(iAux + iVertexIndex + iVertexsPerRing);// v 2 
                faceAux.Indices.Add(iAux + iVertexsPerRing);// v 3  <= "provoking index" ( despite idx 0 and idx 2 are not the same index, they have the same normal, so the color of the faces is properly shown )
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
        private void GenerateShellBeginCoverFaces(ref Mesh model_mesh) {
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
            for (iVertexIndex = 0; iVertexIndex < iVertexsPerRing; iVertexIndex++) {

                // face 1 
                faceAux = new Face();
                faceAux.Indices.Add(iAux + ((iVertexIndex + 1) % iVertexsPerRing)); // v 1                
                faceAux.Indices.Add(iAux + iVertexIndex); // v 0
                faceAux.Indices.Add(iAux + ((iVertexIndex + 1) % iVertexsPerRing) + iVertexsPerRing); // v 3  
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
        private void GenerateShellEndCoverFaces(ref Mesh model_mesh) {
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

        }//GenerateSpiralEndCoverFaces


        /*******************************************************************************
        * @brief method that programatically generates the shell model according to the 
        * parameters set by the user
        *******************************************************************************/
        private void GenerateShellModel(ref Mesh model_mesh) {

            model_mesh = new Mesh("Shell", Assimp.PrimitiveType.Triangle);

            // reset all the counters
            iVerticesCter = 0;
            iFacesCter = 0;
            iIndicesFacesCter = 0;

            // frist get the scale factor and translation vector that must be applied to
            // the shell geometry to fit it into the frustrum / screen.
            CalculateTranslationAndScaleFactors();

            // generate the shell generating curve (ring) vertices according to the configured parameters
            GetGeneratingCurveVertices();

            // generate and add to the geometry the vertexs 
            GenerateOuterShellVertexs(ref model_mesh);
            GenerateInnerShellVertexs(ref model_mesh);
            GenerateShellBeginCoverVertexs(ref model_mesh);
            GenerateShellEndCoverVertexs(ref model_mesh);

            // generate and add to the geometry the normals
            GenerateOuterShellNormals(ref model_mesh);
            GenerateInnerShellNormals(ref model_mesh);
            GenerateShellBeginCoverNormals(ref model_mesh);
            GenerateShellEndCoverNormals(ref model_mesh);

            // generate and add to the geometry the faces
            GenerateOuterShellFaces(ref model_mesh);
            GenerateInnerShellFaces(ref model_mesh);
            GenerateShellBeginCoverFaces(ref model_mesh);
            GenerateShellEndCoverFaces(ref model_mesh);

        }//GenerateSpiralModel

        /*******************************************************************************
        * @brief exports the mesh of the object into a file with the specified name.
        * 
        * @param[in] strFileName with the name of the file where the object geometry data
        * will be exported.
        * @param[in] strFormatId the export format for the scene ( "obj", "fbx", "stl" )
        *******************************************************************************/
        public int export(string strFileName, string strFormatId) {
            int retVal = 0;
            float fScaleFactor = 40.0f;
            Mesh scaledMeshToExport = null;
            var context = new AssimpContext();
            int iTotalVertices;
            int iAux;

            // before exporting it, clone and scale the mesh 
            scaledMeshToExport = new Mesh();

            scaledMeshToExport = object3D.CloneMesh(liMeshesOriginal[0]);

            iTotalVertices = scaledMeshToExport.Vertices.Count();
            for (iAux = 0; iAux < iTotalVertices; iAux++) {

                scaledMeshToExport.Vertices[iAux] = new Vector3D(
                    (float)(scaledMeshToExport.Vertices[iAux].X * fScaleFactor),
                    (float)(scaledMeshToExport.Vertices[iAux].Y * fScaleFactor),
                    (float)(scaledMeshToExport.Vertices[iAux].Z * fScaleFactor));

            }//for

            // Create the Scene() object needed to export the mesh
            var scene = new Scene();

            // Add the scaled mesh into the scene
            scene.Meshes.Add(scaledMeshToExport);

            // Crete the scene root node
            scene.RootNode = new Node("Shell");

            // Associate the sence to the node ( 0 because is the first )
            scene.RootNode.MeshIndices.Add(0);

            // Add the material ( optional but recomended )
            var material = new Assimp.Material();
            scene.Materials.Add(material);
            liMeshesOriginal[0].MaterialIndex = 0;

            // 5. Exportar (elige formato: "obj", "fbx", "stl", etc.)
            context.ExportFile(scene, strFileName, strFormatId);

            return retVal;

        }//export

        /*******************************************************************************
        * @brief initializes the object geometry ( vertex, normals, faces ) programatically
        * using the Raups model
        *******************************************************************************/
        public override void InitializeGeometry() {
            Mesh model_mesh = null;

            // call the method that procedurally generates the shell model geometry and stores it into the Scene object
            GenerateShellModel(ref model_mesh);

            liMeshesOriginal = new List<Mesh>();
            liMeshesOriginal.Add(model_mesh);

        }//Initialize

        /*******************************************************************************
        * @brief updates the VAO ( VBO and EBO ) with that data.
        *******************************************************************************/
        public override void UpdateVAO() {
            int iStride = 0;
            List<float> liCoordNColors;
            List<uint> liFaceIdxs;
            int iAux = 0;
             
            // VAO: VBO + EBO

            // VBO: create and initialize the list for the VBO: 3 vertex floats   + 3 color floats  + 3 normal floats
            liCoordNColors = new List<float>();
            iVerticesCter = liMeshesOriginal[0].Vertices.Count();
            for (iAux = 0; iAux < iVerticesCter; iAux++) {

                // each face has 3 vertices + 3 colors + 3 normals
                // vertex coords centered in the middle (flMax... - flMin...) / 2;
                liCoordNColors.Add((float)liMeshesOriginal[0].Vertices[iAux].X);
                liCoordNColors.Add((float)liMeshesOriginal[0].Vertices[iAux].Y);
                liCoordNColors.Add((float)liMeshesOriginal[0].Vertices[iAux].Z);
                // color
                liCoordNColors.Add(((float)refAppViewModel.MeshColor.R) / 255f);
                liCoordNColors.Add(((float)refAppViewModel.MeshColor.G) / 255f);
                liCoordNColors.Add(((float)refAppViewModel.MeshColor.B) / 255f);
                liCoordNColors.Add((float)(refAppViewModel.MeshTransparency / 100.0f));
                // normal
                liCoordNColors.Add((float)liMeshesOriginal[0].Normals[iAux].X);
                liCoordNColors.Add((float)liMeshesOriginal[0].Normals[iAux].Y);
                liCoordNColors.Add((float)liMeshesOriginal[0].Normals[iAux].Z);

            }//for

            // EBO: create and initialize the faces indexs list for the EBO:
            liFaceIdxs = new List<uint>();
            iFacesCter = liMeshesOriginal[0].Faces.Count();
            foreach (var face in liMeshesOriginal[0].Faces) {

                // each face has 3 index because we us PostProcessSteps.Triangulate
                liFaceIdxs.Add((uint)face.Indices[0]);
                liFaceIdxs.Add((uint)face.Indices[1]);
                liFaceIdxs.Add((uint)face.Indices[2]);

                iIndicesFacesCter = iIndicesFacesCter + 3;

            }//foreach

            // #### initalize the model VAO (VBO and EBO)  with the created lists

            objectVAO = GL.GenVertexArray();
            objectVBO = GL.GenBuffer();
            objectEBO = GL.GenBuffer();

            // set the VAO in context
            GL.BindVertexArray(objectVAO);

            // set in context and initalize the VBO
            GL.BindBuffer(BufferTarget.ArrayBuffer, objectVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, liCoordNColors.Count() * sizeof(float), liCoordNColors.ToArray(), BufferUsageHint.StaticDraw);

            // set in context and initalize the EBO
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, objectEBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, liFaceIdxs.Count() * sizeof(uint), liFaceIdxs.ToArray(), BufferUsageHint.StaticDraw);

            iStride = (3 + 4 + 3) * sizeof(float); // pos (3) + color (4=3+1) + normal (3)

            // configure the position atributes
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, iStride, 0);
            GL.EnableVertexAttribArray(0);

            // configure the color+transparency atributes
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, iStride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            // configure the attributes of the normals
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, iStride, 7 * sizeof(float));
            GL.EnableVertexAttribArray(2);

            // clear the context
            GL.BindVertexArray(0);

        }//UpdateVAO

    }//cShellModel

}//namespace RaupCoilerII
