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
    * @brief class that implements the methods related to the shell model 3D spiral.
    * It is used to render the 3D model of the spiral that shows the 3D coil
    *******************************************************************************/
    public class cSpiral : object3D {

        // spiral mesh indexes
        public int iSpiralFirstVertexIdx;
        public int iSpiralFirstNormalIdx;
        public int iSpiralFirstFaceIdx;
        public int iSpiralLastVertexIdx;
        public int iSpiralLastNormalIdx;
        public int iSpiralLastFaceIdx;
        // spiral beginning cover indexes
        public int iSpiralBeginCoverFirstVertexIdx;
        public int iSpiralBeginCoverFirstNormalIdx;
        public int iSpiralBeginCoverFirstFaceIdx;
        public int iSpiralBeginCoverLastVertexIdx;
        public int iSpiralBeginCoverLastNormalIdx;
        public int iSpiralBeginCoverLastFaceIdx;
        // spiral enidng cover indexes
        public int iSpiralEndCoverFirstVertexIdx;
        public int iSpiralEndCoverFirstNormalIdx;
        public int iSpiralEndCoverFirstFaceIdx;
        public int iSpiralEndCoverLastVertexIdx;
        public int iSpiralEndCoverLastNormalIdx;
        public int iSpiralEndCoverLastFaceIdx;

        int iVertexsPerRing = 16;

        /*******************************************************************************
        * @brief Constructor with pararameters.
        * @param[in] _refViewModel reference to the VieModel object that will be used 
        * to retrive some information.
        *******************************************************************************/
        public cSpiral(ViewModel _refViewModel) {

            refAppViewModel = _refViewModel;

        }//object3D

        /*******************************************************************************
        * @brief generates all the vertexs of the shell model spiral
        * @param[in|out] spiral_mesh with all the generated vertexs are stored into that object.
        *******************************************************************************/
        private void GenerateSpiralVertexs(ref Mesh spiral_mesh) {
            // variables used to calculate the generating circle used to generate the spiral
            List<Assimp.Vector2D> liBasicGenCurveVertices; // vertices of the simple generating cirlce
            float flAngleVerticesRad = 0; // angle between successive vertices of the ring, in radians
            Assimp.Vector2D vector2DAux1;
            float fCurrRingVertexAngleRad = 0;

            // variables used to generate the spiral model using
            int iRing = 0;
            int iVertexIndex = 0;
            int iTotalNumberOfRings = 0; // the total number of rings in the shell model
            double dAngleRingsRad = 0; // angle between each successive ring/generating curve, in radians
            double dThetaRad = 0;// theta angle used to generate the complete shell model, in radians
            double dRc = 0;
            // auxiliar variables
            Assimp.Vector3D vectorAux1;
            float fXAux, fYAux, fZAux;
            double dXAux, dYAux, dZAux;
            double dAux = 0;


            // ## 2D Generating circel 

            // create and calculate the generating ring used to generate the spiral tube, according
            // to the selected Raup & Michelson parameters
            liBasicGenCurveVertices = new List<Vector2D>();

            flAngleVerticesRad = (2.0f * (float)Math.PI) / (float)iVertexsPerRing;
            fCurrRingVertexAngleRad = 0;
            for (iVertexIndex = 0; iVertexIndex < iVertexsPerRing; iVertexIndex++) {

                // generate current ring vertex X, and Y coordinates for the genrating shape
                fXAux = ((float)Math.Cos((double)fCurrRingVertexAngleRad) * (float)refAppViewModel.IniRadius);
                fYAux = ((float)Math.Sin((double)fCurrRingVertexAngleRad) * (float)refAppViewModel.IniRadius);

                // store the current vertex of the generated shape into the list
                vector2DAux1 = new Assimp.Vector2D(fXAux, fYAux);
                liBasicGenCurveVertices.Add(vector2DAux1);

                fCurrRingVertexAngleRad = fCurrRingVertexAngleRad + flAngleVerticesRad;

            }//for

            // ## 3D spiral vertices

            // calculate some parameters that will be used to generate the shell 3D model
            iTotalNumberOfRings = refAppViewModel.Revolutions * refAppViewModel.RingsRev; // rings in the complete model: is the number of rings per revolution multiplied by the number of revolutions
            dAngleRingsRad = ((double)refAppViewModel.Revolutions * (2.0d * (double)Math.PI)) / (double)iTotalNumberOfRings;

            // keep the index of the shell outer mesh first stored vertex
            iSpiralFirstVertexIdx = spiral_mesh.Vertices.Count();

            // calculate the Rc value according to the confiugred D and fRadius parameter
            dRc = (double)refAppViewModel.IniRadius * (1.0d + (double)refAppViewModel.Dparam) / (1.0d - (double)refAppViewModel.Dparam);

            dThetaRad = 0;
            for (iRing = 0; iRing < iTotalNumberOfRings; iRing++) {

                // calculate the W^(𝜃/2π) for the current Theta value
                dAux =  (double)Math.Pow((double)refAppViewModel.Wparam, (double)(dThetaRad / (double)(2.0d * Math.PI)));// W^(1/refAppViewModel.RingsRev)

                // scale the current "dThetaRad" ring: calculate all the vertices of the current ring of the shell
                for (iVertexIndex = 0; iVertexIndex < iVertexsPerRing; iVertexIndex++) {

                    // apply the "Raup & Michelson" model parameters on the generating curve
                    dXAux = ((double)liBasicGenCurveVertices[iVertexIndex].X + dRc) * dAux;
                    dYAux = ((double)liBasicGenCurveVertices[iVertexIndex].Y * dAux) - (dRc * (double)refAppViewModel.Tparam * (dAux - 1.0d));
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
                    spiral_mesh.Vertices.Add(vectorAux1);

                }//for

                // increase the Theta angle of the following shell model ring
                dThetaRad = dThetaRad + dAngleRingsRad;

                // apply the W param on the next ring radius
                // fCurrRingRadius = fCurrRingRadius * flWRing;

            }//for (iRings=0;

            // keep the index of the spiral mesh last stored vertex
            iSpiralLastVertexIdx = spiral_mesh.Vertices.Count() - 1;

            // JBR 2026-08-21 Para depurar espiral
            int iAux = iSpiralLastVertexIdx - iVertexsPerRing + 1;
            Debug.WriteLine("GenerateSpiralVertexs Last ring");
            for (iVertexIndex = 0; iVertexIndex < iVertexsPerRing; iVertexIndex++) {
                Debug.WriteLine(iVertexIndex.ToString() + ": " + spiral_mesh.Vertices[iAux+ iVertexIndex].X.ToString() +","+ spiral_mesh.Vertices[iAux + iVertexIndex].Y.ToString() + ","+ spiral_mesh.Vertices[iAux + iVertexIndex].Z.ToString());
            }//for
            // FIN JBR 2026-08-21 Para depurar espiral

        }//GenerateSpiralVertexs

        /*******************************************************************************
          * @brief creates the vertex used to define the ending cover of the spiral. The
          * vertexs of the ending cover are the same vertexs than the last ring of the 
          * spiral.
          * 
          * @param[in|out] spiral_mesh with all the generated vertexs are stored into that object.
          *******************************************************************************/
        public void GenerateSpiralEndCoverVertexs(ref Mesh spiral_mesh) {
            int iVertexIndex = 0;
            int iAux = 0;
            Assimp.Vector3D v3Daux;
            int iNumberOfRings = 0;
            float fCenterX, fCenterY, fCenterZ;

            iNumberOfRings = refAppViewModel.Revolutions * refAppViewModel.RingsRev;

            // keep the index of the shell end cover first stored vertex
            iSpiralEndCoverFirstVertexIdx = spiral_mesh.Vertices.Count();

            // firt get the center of the last ring of the spiral. That is because the triangles
            // of the cover will end in the center of the ring.
            iAux = iSpiralLastVertexIdx - iVertexsPerRing + 1;
            fCenterX = 0; fCenterY = 0; fCenterZ = 0;
            for (iVertexIndex = 0; iVertexIndex < iVertexsPerRing; iVertexIndex++) {

                fCenterX = fCenterX + spiral_mesh.Vertices[iAux + iVertexIndex].X;
                fCenterY = fCenterY + spiral_mesh.Vertices[iAux + iVertexIndex].Y;
                fCenterZ = fCenterZ + spiral_mesh.Vertices[iAux + iVertexIndex].Z;

            }//for
            fCenterX = fCenterX / iVertexsPerRing;
            fCenterY = fCenterY / iVertexsPerRing;
            fCenterZ = fCenterZ / iVertexsPerRing;

            // store in the list of vertexs the vertexs of the last ring of the spiral
            // that will be used to generate the spiral tube end cover.
            iAux = iSpiralLastVertexIdx - iVertexsPerRing + 1;
            for (iVertexIndex = 0; iVertexIndex < iVertexsPerRing; iVertexIndex++) {

                v3Daux = spiral_mesh.Vertices[iAux + iVertexIndex];
                spiral_mesh.Vertices.Add(v3Daux);

            }//for

            // store the spiral end cover center vertex used to create the spiral
            // end cover. The cover is created by joining the vertex of spiral's
            // last ring with that calculated central vertex using triangles.
            v3Daux = new Vector3D(fCenterX, fCenterY, fCenterZ);
            spiral_mesh.Vertices.Add(v3Daux);

            // keep the index of the shell end cover last stored vertex
            iSpiralEndCoverLastVertexIdx = spiral_mesh.Vertices.Count() - 1;

            // JBR 2026-08-21 Para depurar espiral
            // iAux = iSpiralEndCoverFirstVertexIdx;
            // Debug.WriteLine("GenerateSpiralEndCoverVertexs:");
            // for (iVertexIndex = 0; iVertexIndex < (iVertexsPerRing*2); iVertexIndex++) {
            // Debug.WriteLine(iVertexIndex.ToString() + ": " + spiral_mesh.Vertices[iAux + iVertexIndex].X.ToString() + "," + spiral_mesh.Vertices[iAux + iVertexIndex].Y.ToString() + "," + spiral_mesh.Vertices[iAux + iVertexIndex].Z.ToString());
            // }//for
            // FIN JBR 2026-08-21 Para depurar espiral

        }//GenerateSpiralEndCoverVertexs


        /*******************************************************************************
        * @brief generates the normals for all the vertices of the spiral tube
        * ( the faces that point outside ).
        * 
        * @param[in|out] spiral_mesh with all the geometry information. Here is where
        * the generated vertex, normals, and faces information is stored
        *******************************************************************************/
        private void GenerateSpiralNormals(ref Mesh spiral_mesh) {
            Assimp.Vector3D vertexAux1;
            Assimp.Vector3D vertexAux2;
            Assimp.Vector3D vectorEdge1;
            Assimp.Vector3D vectorEdge2;
            Assimp.Vector3D vertexNormal;
            int iAux = 0;
            int iRing = 0;
            int iVertexIndex = 0;
            int iNumberOfRings = 0;

            iNumberOfRings = refAppViewModel.Revolutions * refAppViewModel.RingsRev;

            // keep the index of the spiral mesh first stored normal
            iSpiralFirstNormalIdx = spiral_mesh.Normals.Count();

            for (iRing = 0; iRing < (iNumberOfRings - 1); iRing++) {

                iAux = iRing * iVertexsPerRing;// index of the first vertex of the current processed ring

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

                    vertexAux1 = spiral_mesh.Vertices[iAux + iVertexIndex];

                    // get the vector that joins the current vertex with the equivalente vertex in the next face
                    vertexAux2 = spiral_mesh.Vertices[iAux + ((iVertexIndex + 1) % iVertexsPerRing)];
                    vectorEdge1 = vertexAux1 - vertexAux2;

                    // get the vector that joins the current vertex with the equivalente vertex in the next face
                    vertexAux2 = spiral_mesh.Vertices[iAux + iVertexIndex + iVertexsPerRing];
                    vectorEdge2 = vertexAux1 - vertexAux2;

                    // calculate the normal of that vertex and add it to the vertex list
                    vertexNormal = new Assimp.Vector3D();
                    vertexNormal = Assimp.Vector3D.Cross(vectorEdge1, vectorEdge2);
                    vertexNormal.Normalize();

                    spiral_mesh.Normals.Add(vertexNormal);

                }// for (iVertexIndex = 0

            }//for (iRing = 0;

            iAux = iRing * iVertexsPerRing;// index of the first vertex of the current processed ring

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

                vertexAux1 = spiral_mesh.Vertices[iAux + iVertexIndex];

                // get the vector that joins the current vertex with the equivalente vertex in the next face
                vertexAux2 = spiral_mesh.Vertices[iAux + ((iVertexIndex + 1) % iVertexsPerRing)];

                vectorEdge1 = vertexAux1 - vertexAux2;

                // get the vector that joins the current vertex with the equivalente vertex in the next face
                vertexAux2 = spiral_mesh.Vertices[iAux + iVertexIndex - iVertexsPerRing];

                vectorEdge2 = vertexAux2 - vertexAux1;

                // calculate the normal of that vertex and add it to the vertex list
                vertexNormal = new Assimp.Vector3D();
                vertexNormal = Assimp.Vector3D.Cross(vectorEdge1, vectorEdge2);
                vertexNormal.Normalize();

                spiral_mesh.Normals.Add(vertexNormal);

            }//for

            // keep the index of the spiral mesh last stored normal
            iSpiralLastNormalIdx = spiral_mesh.Normals.Count() - 1;

        }//GenerateSpiralNormals


        /*******************************************************************************
        * @brief generates the normals for all the vertices of the spiral end cover. The 
        * end cover is built creating triangles from the spiral final ring vertices to 
        * the centtral vertex of that ring.
        * 
        * @param[in|out] spiral_mesh with all the geometry information. Here is where
        * the generated vertex, normals, and faces information is stored
        *******************************************************************************/
        private void GenerateSpiralEndCoverNormals(ref Mesh spiral_mesh) {
            Assimp.Vector3D vertexAux1;
            Assimp.Vector3D vertexAux2;
            Assimp.Vector3D vectorEdge1;
            Assimp.Vector3D vectorEdge2;
            Assimp.Vector3D vertexNormal ;
            int iAux = 0;
            int iVertexIndex = 0;
            int iNumberOfRings = 0;

            iNumberOfRings = refAppViewModel.Revolutions * refAppViewModel.RingsRev;

            // keep the index of the spiral end cover first stored normal
            iSpiralEndCoverFirstNormalIdx = spiral_mesh.Normals.Count();

            // calculate all the normals of vertexs of the spiral last ring used to generate the spiral end cover
            iAux = iSpiralEndCoverFirstVertexIdx;
            for (iVertexIndex = 0; iVertexIndex < iVertexsPerRing; iVertexIndex++) {

                // Indexs of a face of the spiral end cover
                // iAux = iRing * iVertexsPerRing    
                //                   iAux + iVertexIndex>  + O ----------------- 1 + <iAux + iVertexIndex + 1
                //                                             +               +
                //                                               +   face    +
                //                                                 +       + 
                //                                                   +   +
                //                                                     3  < iSpiralEndCoverLastVertexIdx: is the center of the last ring
                //
                vertexAux1 = spiral_mesh.Vertices[iAux + iVertexIndex];

                // get the vector that joins the current vertex with the equivalente vertex in the next face
                vertexAux2 = spiral_mesh.Vertices[iAux + ((iVertexIndex + 1) % iVertexsPerRing)];

                vectorEdge1 = vertexAux2 - vertexAux1;

                // get the vector that joins the current vertex with the equivalente vertex in the next face
                vertexAux2 = spiral_mesh.Vertices[iSpiralEndCoverLastVertexIdx];

                vectorEdge2 = vertexAux2 - vertexAux1;

                // calculate the normal of that vertex and add it to the vertex list
                vertexNormal = new Assimp.Vector3D();
                vertexNormal = Assimp.Vector3D.Cross(vectorEdge1, vectorEdge2);
                vertexNormal.Normalize();

                spiral_mesh.Normals.Add(-vertexNormal);

            }// for (iVertexIndex = 0

            // calculate the normal of spiral end cover central vertex, as the cover
            // is flat we use the same normal than the last calcualted vertex
            vertexNormal = new Assimp.Vector3D();
            vertexNormal = spiral_mesh.Normals[iAux + iVertexIndex - 1];
            vertexNormal.Normalize();

            spiral_mesh.Normals.Add(-vertexNormal);

            // keep the index of the shell end cover last stored normal
            iSpiralEndCoverLastNormalIdx = spiral_mesh.Normals.Count() - 1;

        }//GenerateSpiralEndCoverNormals

        /*******************************************************************************
        * @brief fills the faces information by setting the indexs of the vertex that 
        * define all the faces of the spiral geometry ( the faces that point outside ).
        * 
        * IMPORTANT: when using flat face coluring, OpenGL uses the color of the last 
        * vertex that defines the face to assign the color to the triangle. That vertex 
        * is called the  "provoking index" because is the one that "provokes" the color 
        * in the flat face. To grant that faces are properly coloured when using flat 
        * ilumination the "provoking index" must be properly set!. In order that the 
        * two triangles that defien a quad faces are shown with the same color both 
        * triangle/faces should use the same vertex as provoking index.
        * 
        * @param[in|out] spiral_mesh with all the geometry information. Here is where
        * the generated vertex, normals, and faces information is stored
        *******************************************************************************/
        private void GenerateSpiralFaces(ref Mesh spiral_mesh) {
            Assimp.Face faceAux;
            int iRing = 0;
            int iVertexIndex = 0;
            int iAux = 0;
            int iNumberOfRings = 0;

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
            iSpiralFirstFaceIdx = spiral_mesh.Faces.Count();

            // generate the faces index
            for (iRing = 0; iRing < (iNumberOfRings - 1); iRing++) {

                iAux = iRing * iVertexsPerRing;

                for (iVertexIndex = 0; iVertexIndex < (iVertexsPerRing - 1); iVertexIndex++) {

                    // face 1 
                    faceAux = new Face();
                    faceAux.Indices.Add(iAux + iVertexIndex); // v 0 
                    faceAux.Indices.Add(iAux + iVertexIndex + 1); // v 1
                    faceAux.Indices.Add(iAux + iVertexIndex + 1 + iVertexsPerRing); // v 3  <= "provoking index"
                    spiral_mesh.Faces.Add(faceAux);

                    // face 2 
                    faceAux = new Face();
                    faceAux.Indices.Add(iAux + iVertexIndex + iVertexsPerRing); // v 2
                    faceAux.Indices.Add(iAux + iVertexIndex); // v 0                    
                    faceAux.Indices.Add(iAux + iVertexIndex + 1 + iVertexsPerRing); // v 3  <= "provoking index"
                    spiral_mesh.Faces.Add(faceAux);

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
                spiral_mesh.Faces.Add(faceAux);

                // last face 2 of the ring
                faceAux = new Face();
                faceAux.Indices.Add(iAux + iVertexIndex + iVertexsPerRing);// v 2 
                faceAux.Indices.Add(iAux + iVertexIndex);// v 0
                faceAux.Indices.Add(iAux + iVertexsPerRing);// v 3  <= "provoking index"
                spiral_mesh.Faces.Add(faceAux);

            }//for (iRing = 0; iRing < iNumberOfRings; iRing++) {

            // keep the index of the spiral mesh last stored face
            iSpiralLastFaceIdx = spiral_mesh.Faces.Count() - 1;

        }//GenerateSpiralFaces

        /*******************************************************************************
        * @brief defines the faces indices of the spiral end cover.
        * 
        * IMPORTANT: when using flat face coluring, OpenGL uses the color of the last 
        * vertex that defines the face to assign the color to the triangle. That vertex 
        * is called the  "provoking index" because is the one that "provokes" the color 
        * in the flat face. To grant that faces are properly coloured when using flat 
        * ilumination the "provoking index" must be properly set!. In order that the 
        * two triangles that defien a quad faces are shown with the same color both 
        * triangle/faces should use the same vertex as provoking index.
        * 
        * @param[in|out] spiral_mesh with all the geometry information. Here is where
        * the generated vertex, normals, and faces information is stored
        *******************************************************************************/
        private void GenerateSpiralEndCoverFaces(ref Mesh spiral_mesh) {
            Assimp.Face faceAux;
            int iVertexIndex = 0;
            int iAux = 0;
            int iNumberOfRings = 0;

            iNumberOfRings = refAppViewModel.Revolutions * refAppViewModel.RingsRev;

            // Indexs of a regular face of the ring:
            // iAux = iRing * iVertexsPerRing    
            //                   iAux + iVertexIndex>  + O ----------------- 1 + <iAux + iVertexIndex + 1
            //                                             +               +
            //                                               +   face    +
            //                                                 +       + 
            //                                                   +   +
            //                                                     3  < iSpiralEndCoverLastVertexIdx: is the center of the last ring
            //
            //  Face must be defined in this order:

            // IMPORTANT: "provoking index" is the last index of the face, and is the one that "provokes" the color of the face
            // when using flat colouring / ilumination.To grant that faces are properly coloured when using flat ilumination
            // the "provoking index" of the two triangles of the face must me the same, in that case Vertex 3. Or at less they
            // should have the same normal value.

            // keep the index of the shell end cover first stored face
            iSpiralEndCoverFirstFaceIdx = spiral_mesh.Faces.Count();

            iAux = iSpiralEndCoverFirstVertexIdx;

            // generate the faces index
            for (iVertexIndex = 0; iVertexIndex < iVertexsPerRing; iVertexIndex++) {

                // face 1 
                faceAux = new Face();
                faceAux.Indices.Add(iAux + iVertexIndex); // v 0
                faceAux.Indices.Add(iAux + ((iVertexIndex + 1) % iVertexsPerRing)); // v 1                
                faceAux.Indices.Add(iSpiralEndCoverLastVertexIdx); // v 3  
                spiral_mesh.Faces.Add(faceAux);

            }//for (iRings=0;

            // keep the index of the shell end cover last stored face
            iSpiralEndCoverLastFaceIdx = spiral_mesh.Faces.Count() - 1;

        }//GenerateSpiralEndCoverFaces


        /*******************************************************************************
         * @brief method that programatically generates the spiral model according to the 
         * parameters set by the user
         *******************************************************************************/
        private void GenerateSpiralModel(ref Mesh spiral_mesh) {

            spiral_mesh = new Mesh("Spiral", Assimp.PrimitiveType.Triangle);

            // reset all the counters
            iVerticesCter = 0;
            iFacesCter = 0;
            iIndicesFacesCter = 0;

            // generate and add to the geometry the vertexs 
            GenerateSpiralVertexs(ref spiral_mesh);
            GenerateSpiralEndCoverVertexs(ref spiral_mesh);

            // generate and add to the geometry the normals
            GenerateSpiralNormals(ref spiral_mesh);
            GenerateSpiralEndCoverNormals(ref spiral_mesh);

            // generate and add to the geometry the faces
            GenerateSpiralFaces(ref spiral_mesh);
            GenerateSpiralEndCoverFaces(ref spiral_mesh);

        }//GenerateSpiralModel

        /*******************************************************************************
        * @brief initializes the object geometry ( vertex, normals, faces ) wit the 
        * information in the file.
        *******************************************************************************/
        public void InitializeGeometry() {
            Mesh spiral_mesh = null;

            // call the method that procedurally generates the shell model geometry and stores it into the Scene object
            GenerateSpiralModel(ref spiral_mesh);

            liMeshesOriginal = new List<Mesh>();
            liMeshesOriginal.Add(spiral_mesh);

        }//InitializeGeometry

        /*******************************************************************************
        * @brief updates the VAO ( VBO and EBO ) with that data.
        *******************************************************************************/
        public void TransformUpdateVAO() {
            int iStride = 0;
            List<float> liCoordNColors = null;
            List<uint> liFaceIdxs = null;
            Mesh mesh;
            int iAux2 = 0;

            // ### update the content of the lists that will be used to intialize the VAO ( VBO and EBO )

            // 3 vertex floats   + 3 color floats  + 3 normal floats
            // add to the list of vertexs the coordinates of the 3D Cube
            liCoordNColors = new List<float>();
            iVerticesCter = 0;

            mesh = liMeshesOriginal[0];

            for (iAux2 = 0; iAux2 < mesh.Vertices.Count(); iAux2++) {
                // each face has 3 vertices + 3 colors + 1 transparency + 3 normals
                // vertex coords
                liCoordNColors.Add((float)mesh.Vertices[iAux2].X);
                liCoordNColors.Add((float)mesh.Vertices[iAux2].Y);
                liCoordNColors.Add((float)mesh.Vertices[iAux2].Z);
                // color
                liCoordNColors.Add(((float)refAppViewModel.SpiralColor.R) / 255f);
                liCoordNColors.Add(((float)refAppViewModel.SpiralColor.G) / 255f);
                liCoordNColors.Add(((float)refAppViewModel.SpiralColor.B) / 255f);
                liCoordNColors.Add((float)1);// (refAppViewModel.MeshTransparency / 100.0f));
                // normal
                liCoordNColors.Add((float)mesh.Normals[iAux2].X);
                liCoordNColors.Add((float)mesh.Normals[iAux2].Y);
                liCoordNColors.Add((float)mesh.Normals[iAux2].Z);
            }//for

            iVerticesCter = mesh.Vertices.Count();

            // faces indexs
            // add to the list the indexs that define the faces of the 3D Cube
            liFaceIdxs = new List<uint>();
            iFacesCter = 0;
            iIndicesFacesCter = 0;

            mesh = liMeshesOriginal[0];

            for (iAux2 = 0; iAux2 < mesh.Faces.Count(); iAux2++) {

                // each face has 3 vertex indexs because we us PostProcessSteps.Triangulate
                liFaceIdxs.Add((uint)(mesh.Faces[iAux2].Indices[0]));
                liFaceIdxs.Add((uint)(mesh.Faces[iAux2].Indices[1]));
                liFaceIdxs.Add((uint)(mesh.Faces[iAux2].Indices[2]));

                iIndicesFacesCter = iIndicesFacesCter + 3;

            }//for

            iFacesCter = mesh.Faces.Count();

            // #### initalize the Spiral VAO (VBO and EBO)  with the attributes, vertices and faces in the lists

            objectVAO = GL.GenVertexArray();
            objectVBO = GL.GenBuffer();
            objectEBO = GL.GenBuffer();

            // set the VAO in context
            GL.BindVertexArray(objectVAO);

            // set in context and initalize the VBO
            GL.BindBuffer(BufferTarget.ArrayBuffer, objectVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, liCoordNColors.Count * sizeof(float), liCoordNColors.ToArray(), BufferUsageHint.StaticDraw);

            // set in context and initalize the EBO
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, objectEBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, liFaceIdxs.Count * sizeof(uint), liFaceIdxs.ToArray(), BufferUsageHint.StaticDraw);

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

    }//cSpiral

}//}//namespace RaupCoilerII

