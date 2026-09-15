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
    * @brief class that implements the methods related to the shell coiling axis
    * 3D model. It is used to render the 3D model of the coiling axis.
    *******************************************************************************/
    public class cAxis : object3D {

        // coiling axis top cover mesh indexes
        // first
        public int iAxisTopCoverFirstVertexIdx;
        public int iAxisTopCoverFirstNormalIdx;
        public int iAxisTopCoverFirstFaceIdx;
        // last
        public int iAxisTopCoverLastVertexIdx;
        public int iAxisTopCoverLastNormalIdx;
        public int iAxisTopCoverLastFaceIdx;
        // coiling axis top section mesh indexes
        // first
        public int iAxisTubeFirstVertexIdx;
        public int iAxisTubeFirstNormalIdx;
        public int iAxisTubeFirstFaceIdx;
        // last
        public int iAxisTubeLastVertexIdx;
        public int iAxisTubeLastNormalIdx;
        public int iAxisTubeLastFaceIdx;
        // coiling axis bottom cover mesh indexes
        // first
        public int iAxisBotCoverFirstVertexIdx;
        public int iAxisBotCoverFirstNormalIdx;
        public int iAxisEndCoverFirstFaceIdx;
        // last
        public int iAxisBotCoverLastVertexIdx;
        public int iAxisBotCoverLastNormalIdx;
        public int iAxisBotCoverLastFaceIdx;

        // the top and bottom Y coordinates of the axis
        public float fTopY;
        public float fBotY;

        /*******************************************************************************
        * @brief Constructor with pararameters.
        * @param[in] _refViewModel reference to the VieModel object that will be used 
        * to retrive some information.
        *******************************************************************************/
        public cAxis(ViewModel _refViewModel) {

            refAppViewModel = _refViewModel;

            fTopY = 1.0f;
            fBotY = -1.0f;

        }//cAxis

        /*******************************************************************************
        * @brief generates all the vertices of the 3D model of the shell coiling axis 
        * @param[in|out] axis_mesh with all the generated vertices are stored into that object.
        *******************************************************************************/
        private void GenerateAxisvertices(ref Mesh axis_mesh) {
            // variables used to calculate the generating circle used to generate the coiling axis
            List<Assimp.Vector2D> liAxisSectionVertices; // vertices of the simple axis section
            float flAngleVerticesRad = 0; // angle between successive vertices of the ring, in radians
            Assimp.Vector2D vector2DAux1;
            float fCurrRingVertexAngleRad = 0;
            int iverticesPerRing = 0;
            float fRadius = 0;

            // variables used to generate the spiral model using
            int iVertexIndex = 0;
            float fAxisTopCoverY = 0;
            float fAxisBotCoverY = 0;
            // auxiliar variables
            Assimp.Vector3D vectorAux1;
            float fXAux = 0.0f, fYAux = 0.0f, fZAux = 0.0f;
            int iIterCter = 0;

            // use the fMaxY and fMinY to calculate the two ends of the coiling axis
            fAxisTopCoverY = fTopY;
            fAxisBotCoverY = fBotY;

            // ## 2D Generating circle 

            // create and calculate the ring used to generate the axis tube
            liAxisSectionVertices = new List<Vector2D>();

            fRadius = (float) refAppViewModel.AxisWidth;// the axis width
            iverticesPerRing = refAppViewModel.AxisResolution;// axis circle ressolution

            flAngleVerticesRad = (2.0f * (float)Math.PI) / (float)iverticesPerRing;
            fCurrRingVertexAngleRad = 0;
            for (iVertexIndex = 0; iVertexIndex < iverticesPerRing; iVertexIndex++) {

                fXAux = ((float)Math.Cos((double)fCurrRingVertexAngleRad) * fRadius);
                fYAux = ((float)Math.Sin((double)fCurrRingVertexAngleRad) * fRadius);

                // store the current vertex of the generated shape into the list
                vector2DAux1 = new Assimp.Vector2D(fXAux, fYAux);
                liAxisSectionVertices.Add(vector2DAux1);

                fCurrRingVertexAngleRad = fCurrRingVertexAngleRad + flAngleVerticesRad;

            }//for

            // ## 3D tube vertices

            // the vertices of the liAxisSectionVertices are processed 3 times, in a 
            // diferent ways on each iteration of to generate the axis:
            // - 1st iteration generates the axis tube top cover
            // - 2nd iteration generates the top section of the axis tube
            // - 3th iteration generates the bottom section of the sxis tube
            // - 4th iteration generates the axis tube bottom cover
            for (iIterCter = 0; iIterCter < 4; iIterCter++) {

                // update the indexs that correspond to the current iteration
                switch (iIterCter) {
                    case 0://axis tube top cover
                        iAxisTopCoverFirstVertexIdx = axis_mesh.Vertices.Count();
                        fYAux = fAxisTopCoverY;
                        break;
                    case 1: // axis tube top section
                        iAxisTubeFirstVertexIdx = axis_mesh.Vertices.Count();
                        fYAux = fAxisTopCoverY;
                        break;
                    case 2:// axis tube bottom section
                        fYAux = fAxisBotCoverY;
                        break;
                    case 3://axis tube bottom cover
                        iAxisBotCoverFirstVertexIdx = axis_mesh.Vertices.Count();
                        fYAux = fAxisBotCoverY;
                        break;
                }//switch

                // scale the current "dThetaRad" ring: calculate all the vertices of the current ring of the shell
                for (iVertexIndex = 0; iVertexIndex < iverticesPerRing; iVertexIndex++) {

                    // convert the coordinates of the generating curve to the axis coordinates
                    fXAux = liAxisSectionVertices[iVertexIndex].X + objModelTransX;                    
                    fZAux = liAxisSectionVertices[iVertexIndex].Y + objModelTransZ;

                    // store the calculated vertex coordinates into the mesh structure
                    vectorAux1 = new Assimp.Vector3D(fXAux, fYAux+ objModelTransY, fZAux);
                    axis_mesh.Vertices.Add(vectorAux1);

                }//for

                // update the indexs that correspond to the current iteration
                switch (iIterCter) {
                    case 0://axis tube top cover
                        // also add the vertex that corresponds to the center of the axis top cover
                        vectorAux1 = new Assimp.Vector3D(0 + objModelTransX, fYAux + objModelTransY, 0 + objModelTransZ);
                        axis_mesh.Vertices.Add(vectorAux1);
                        iAxisTopCoverLastVertexIdx = axis_mesh.Vertices.Count() - 1;
                        break;
                    case 1: // axis tube top section
                        break;
                    case 2:// axis tube bottom section
                        iAxisTubeLastVertexIdx = axis_mesh.Vertices.Count() - 1;
                        break;
                    case 3://axis tube bottom cover
                        // also add the vertex that corresponds to the center of the axis bottom cover
                        vectorAux1 = new Assimp.Vector3D(0 + objModelTransX, fYAux + objModelTransY, 0 + objModelTransZ);
                        axis_mesh.Vertices.Add(vectorAux1);
                        iAxisBotCoverLastVertexIdx = axis_mesh.Vertices.Count() - 1;
                        break;
                }//switch

            }//for

        }//GenerateAxisvertices

        /*******************************************************************************
        * @brief generates the normals for all the vertices of the axis 3D model
        * ( the faces that point outside ).
        * 
        * @param[in|out] axis_mesh with all the geometry information. Here is where
        * the generated vertex, normals, and faces information is stored
        *******************************************************************************/
        private void GenerateAxisNormals(ref Mesh axis_mesh) {
            Assimp.Vector3D vertexAux1;
            Assimp.Vector3D vertexAux2;
            Assimp.Vector3D vectorEdge1;
            Assimp.Vector3D vectorEdge2;
            Assimp.Vector3D vertexNormal;
            int iVertexIndex = 0;
            int iverticesPerRing = 0;
            int iAux = 0;
            float fXAux = 0.0f, fYAux = 0.0f, fZAux = 0.0f;

            iverticesPerRing = refAppViewModel.AxisResolution;// axis circle ressolution

            // #### calcualate the normals for the axis top cover vertices

            // keep the index of the spiral end cover first stored normal
            iAxisTopCoverFirstNormalIdx = axis_mesh.Normals.Count();

            // calculate all the normals of vertices of the axis top cover
            iAux = iAxisTopCoverFirstVertexIdx;
            for (iVertexIndex = 0; iVertexIndex < iverticesPerRing; iVertexIndex++) {

                // Indexs of a face of the spiral end cover
                // iAux = iRing * iverticesPerRing    
                //                   iAux + iVertexIndex>  + O ----------------- 1 + <iAux + iVertexIndex + 1
                //                                             +               +
                //                                               +   face    +
                //                                                 +       + 
                //                                                   +   +
                //                                                     3  < iSpiralEndCoverLastVertexIdx: is the center of the last ring
                //
                vertexAux1 = axis_mesh.Vertices[iAux + iVertexIndex];

                // get the vector that joins the current vertex with the equivalente vertex in the next face
                vertexAux2 = axis_mesh.Vertices[iAux + ((iVertexIndex + 1) % iverticesPerRing)];

                vectorEdge1 = vertexAux2 - vertexAux1;

                // get the vector that joins the current vertex with the equivalente vertex in the next face
                vertexAux2 = axis_mesh.Vertices[iAxisTopCoverLastVertexIdx];

                vectorEdge2 = vertexAux2 - vertexAux1;

                // calculate the normal of that vertex and add it to the vertex list
                vertexNormal = new Assimp.Vector3D();
                vertexNormal = Assimp.Vector3D.Cross(vectorEdge2, vectorEdge1);
                vertexNormal.Normalize();

                axis_mesh.Normals.Add(vertexNormal);

            }// for (iVertexIndex = 0

            // calculate the normal of axis top cover central vertex, as the cover
            // is flat we use the same normal than the last calcualted vertex
            vertexNormal = new Assimp.Vector3D();
            vertexNormal = axis_mesh.Normals[iAux + iVertexIndex - 1];
            vertexNormal.Normalize();

            axis_mesh.Normals.Add(vertexNormal);

            // keep the index of the axis top cover last stored normal
            iAxisTopCoverLastNormalIdx = axis_mesh.Normals.Count() - 1;

            // ######## calcualate the normals for the axis tube vertices ########

            // keep the index of the axis tubefirst stored normal
            iAxisTubeFirstNormalIdx = axis_mesh.Normals.Count();

            // calculate the normals of the vertices of the axis tube top ring
            iAux = iAxisTubeFirstVertexIdx;
            for (iVertexIndex = 0; iVertexIndex < iverticesPerRing; iVertexIndex++) {

                // As the axis is a rendered as cylinder algined with the Y axis, the normal
                // of each vertex of the axis tube can be calculated directly as a vector
                // from the 0,0 to the X,Z coordinate ( a line between the Y axix and the
                // cylindier vertex coordinates.

                fXAux = -objModelTransX + axis_mesh.Vertices[iAux + iVertexIndex].X;
                fYAux = -objModelTransY + axis_mesh.Vertices[iAux + iVertexIndex].Y;
                fZAux = -objModelTransZ + axis_mesh.Vertices[iAux + iVertexIndex].Z;
                vertexNormal = new Assimp.Vector3D(fXAux, 0, fZAux);
                vertexNormal.Normalize();

                axis_mesh.Normals.Add(vertexNormal);

            }// for (iVertexIndex = 0

            // calculate the normals of the vertices of the axis tube bottom ring
            for (iVertexIndex = 0; iVertexIndex<iverticesPerRing; iVertexIndex++) {

                // As the axis is a rendered as cylinder algined with the Y axis, the normal
                // of each vertex of the axis tube can be calculated directly as a vector
                // from the 0,0 to the X,Z coordinate ( a line between the Y axix and the
                // cylindier vertex coordinates.

                fXAux = - objModelTransX + axis_mesh.Vertices[iAux + iVertexIndex].X;
                fYAux = - objModelTransY + axis_mesh.Vertices[iAux + iVertexIndex].Y;
                fZAux = - objModelTransZ + axis_mesh.Vertices[iAux + iVertexIndex].Z;
                vertexNormal = new Assimp.Vector3D(fXAux, 0, fZAux);
                vertexNormal.Normalize();

                axis_mesh.Normals.Add(vertexNormal);


            }//for

            // keep the index of the axis tubefirst stored normal
            iAxisTubeLastNormalIdx = axis_mesh.Normals.Count()-1;

            // ######## calcualate the normals of the axis bottom cover vertices ########

            // keep the index of the axis bottom cover first stored normal
            iAxisBotCoverFirstNormalIdx = axis_mesh.Normals.Count();

            // calculate all the normals of vertices of the axis top cover
            iAux = iAxisBotCoverFirstVertexIdx;
            for (iVertexIndex = 0; iVertexIndex < iverticesPerRing; iVertexIndex++) {

                // Indexs of a face of the spiral end cover
                // iAux = iRing * iverticesPerRing    
                //                   iAux + iVertexIndex>  + O ----------------- 1 + <iAux + iVertexIndex + 1
                //                                             +               +
                //                                               +   face    +
                //                                                 +       + 
                //                                                   +   +
                //                                                     3  < iSpiralEndCoverLastVertexIdx: is the center of the last ring
                //
                vertexAux1 = axis_mesh.Vertices[iAux + iVertexIndex];

                // get the vector that joins the current vertex with the equivalente vertex in the next face
                vertexAux2 = axis_mesh.Vertices[iAux + ((iVertexIndex + 1) % iverticesPerRing)];

                vectorEdge1 = vertexAux1 - vertexAux2;

                // get the vector that joins the current vertex with the equivalente vertex in the next face
                vertexAux2 = axis_mesh.Vertices[iAxisBotCoverLastVertexIdx];

                vectorEdge2 = vertexAux1 - vertexAux2;

                // calculate the normal of that vertex and add it to the vertex list
                vertexNormal = new Assimp.Vector3D();
                vertexNormal = Assimp.Vector3D.Cross(vectorEdge1, vectorEdge2);
                vertexNormal.Normalize();

                axis_mesh.Normals.Add(vertexNormal);

            }// for (iVertexIndex = 0

            // calculate the normal of axis top cover central vertex, as the cover
            // is flat we use the same normal than the last calcualted vertex
            vertexNormal = new Assimp.Vector3D();
            vertexNormal = axis_mesh.Normals[iAux + iVertexIndex - 1];
            vertexNormal.Normalize();

            axis_mesh.Normals.Add(vertexNormal);

            // keep the index of the axis bottom cover last stored normal
            iAxisBotCoverLastNormalIdx = axis_mesh.Normals.Count() - 1;

        }//GenerateAxisNormals

        /*******************************************************************************
        * @brief fills the faces information by setting the indexs of the vertex that 
        * define all the faces of the axis geometry ( the faces that point outside ).
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
        private void GenerateAxisFaces(ref Mesh axis_mesh) {
            Assimp.Face faceAux;
            int iVertexIndex = 0;
            int iAux = 0;
            int iVertexsPerRing = 0;

            iVertexsPerRing = refAppViewModel.AxisResolution;// axis circle ressolution

            // ######## calcualate the faces indices of the faces of the axis tube top cover ########

            // Indexs of a regular face of the ring:
            // iAux = iRing * iVertexsPerRing    
            //                   iAux + iVertexIndex>  + O ----------------- 1 + <iAux + iVertexIndex + 1
            //                                             +               +
            //                                               +   face    +
            //                                                 +       + 
            //                                                   +   +
            //                                                     3  < iAxisTopCoverLastVertexIdx: is the center of the last ring
            //
            //  Face must be defined in this order:

            // IMPORTANT: "provoking index" is the last index of the face, and is the one that "provokes" the color of the face
            // when using flat colouring / ilumination.To grant that faces are properly coloured when using flat ilumination
            // the "provoking index" of the two triangles of the face must me the same, in that case Vertex 3. Or at less they
            // should have the same normal value.

            // keep the index of the coiling axis bottom cover first stored face
            iAxisTopCoverFirstFaceIdx = axis_mesh.Faces.Count();

            iAux = iAxisTopCoverFirstVertexIdx;

            // generate the faces index
            for (iVertexIndex = 0; iVertexIndex < iVertexsPerRing; iVertexIndex++) {

                // face 1 
                faceAux = new Face();
                faceAux.Indices.Add(iAux + iVertexIndex); // v 0                
                faceAux.Indices.Add(iAxisTopCoverLastVertexIdx); // v 3  
                faceAux.Indices.Add(iAux + ((iVertexIndex + 1) % iVertexsPerRing)); // v 1
                axis_mesh.Faces.Add(faceAux);

            }//for (iRings=0;

            // keep the index of the shell end cover last stored face
            iAxisTopCoverLastFaceIdx = axis_mesh.Faces.Count() - 1;

            // ######## calcualate the faces indices of the faces of the axis tube ########

            // Indexs of a regular face of the tube ring:
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

            // keep the index of the axis tube mesh first stored face
            iAxisTubeFirstFaceIdx = axis_mesh.Faces.Count();

            iAux = iAxisTubeFirstVertexIdx;

            for (iVertexIndex = 0; iVertexIndex < (iVertexsPerRing - 1); iVertexIndex++) {

                // face 1 
                faceAux = new Face();                
                faceAux.Indices.Add(iAux + iVertexIndex); // v 0 
                faceAux.Indices.Add(iAux + iVertexIndex + 1); // v 1
                faceAux.Indices.Add(iAux + iVertexIndex + 1 + iVertexsPerRing); // v 3  <= "provoking index"
                axis_mesh.Faces.Add(faceAux);

                // face 2 
                faceAux = new Face();
                faceAux.Indices.Add(iAux + iVertexIndex + iVertexsPerRing); // v 2
                faceAux.Indices.Add(iAux + iVertexIndex); // v 0                    
                faceAux.Indices.Add(iAux + iVertexIndex + 1 + iVertexsPerRing); // v 3  <= "provoking index"
                axis_mesh.Faces.Add(faceAux);

            }//for (iVertexIndex = 0

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
            axis_mesh.Faces.Add(faceAux);

            // last face 2 of the ring
            faceAux = new Face();
            faceAux.Indices.Add(iAux + iVertexIndex + iVertexsPerRing);// v 2 
            faceAux.Indices.Add(iAux + iVertexIndex);// v 0
            faceAux.Indices.Add(iAux + iVertexsPerRing);// v 3  <= "provoking index"
            axis_mesh.Faces.Add(faceAux);

            // keep the index of the axis tube mesh last stored face
            iAxisTubeLastFaceIdx = axis_mesh.Faces.Count();

            // ######## calcualate the faces indices of the faces of the axis tube bottom cover ########

            // Indexs of a regular face of the ring:
            // iAux = iRing * iVertexsPerRing    
            //                   iAux + iVertexIndex>  + O ----------------- 1 + <iAux + iVertexIndex + 1
            //                                             +               +
            //                                               +   face    +
            //                                                 +       + 
            //                                                   +   +
            //                                                     3  < iAxisBotCoverLastVertexIdx: is the center of the last ring
            //
            //  Face must be defined in this order:

            // IMPORTANT: "provoking index" is the last index of the face, and is the one that "provokes" the color of the face
            // when using flat colouring / ilumination.To grant that faces are properly coloured when using flat ilumination
            // the "provoking index" of the two triangles of the face must me the same, in that case Vertex 3. Or at less they
            // should have the same normal value.

            // keep the index of the coiling axis bottom cover first stored face
            iAxisEndCoverFirstFaceIdx = axis_mesh.Faces.Count();

            iAux = iAxisBotCoverFirstVertexIdx;

            // generate the faces index
            for (iVertexIndex = 0; iVertexIndex < iVertexsPerRing; iVertexIndex++) {

                // face 1 
                faceAux = new Face();
                faceAux.Indices.Add(iAux + iVertexIndex); // v 0
                faceAux.Indices.Add(iAux + ((iVertexIndex + 1) % iVertexsPerRing)); // v 1                
                faceAux.Indices.Add(iAxisBotCoverLastVertexIdx); // v 3  
                axis_mesh.Faces.Add(faceAux);

            }//for (iRings=0;

            // keep the index of the shell end cover last stored face
            iAxisBotCoverLastFaceIdx = axis_mesh.Faces.Count() - 1;

        }//GenerateAxisFaces

        /*******************************************************************************
         * @brief method that programatically generates the spiral model according to the 
         * parameters set by the user
         *******************************************************************************/
        private void GenerateAxisModel(ref Mesh axis_mesh) {

            axis_mesh = new Mesh("Axis", Assimp.PrimitiveType.Triangle);

            // reset all the counters
            iVerticesCter = 0;
            iFacesCter = 0;
            iIndicesFacesCter = 0;

            // generate and add to the geometry the vertices 
            GenerateAxisvertices(ref axis_mesh);

            // generate and add to the geometry the normals
            GenerateAxisNormals(ref axis_mesh);

            // generate and add to the geometry the faces
            GenerateAxisFaces(ref axis_mesh);

        }//GenerateAxisModel

        /*******************************************************************************
        * @brief initializes the object geometry ( vertex, normals, faces ) wit the 
        * information in the file.
        *******************************************************************************/
        public void InitializeGeometry() {
            Mesh axis_mesh = null;

            // call the method that procedurally generates the coiling axis of the shell
            // model geometry and stores it into the Scene object
            GenerateAxisModel(ref axis_mesh);

            liMeshesOriginal = new List<Mesh>();
            liMeshesOriginal.Add(axis_mesh);

        }//InitializeGeometry

        /*******************************************************************************
        * @brief updates the VAO ( VBO and EBO ) with that data.
        *******************************************************************************/
        public void UpdateVAO() {
            int iStride = 0;
            List<float> liCoordNColors = null;
            List<uint> liFaceIdxs = null;
            Mesh mesh;
            int iAux2 = 0;

            // ### update the content of the lists that will be used to intialize the VAO ( VBO and EBO )

            // 3 vertex floats   + 3 color floats  + 3 normal floats
            // add to the list of vertices the coordinates of the 3D Cube
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
                liCoordNColors.Add(((float)refAppViewModel.AxisColor.R) / 255f);
                liCoordNColors.Add(((float)refAppViewModel.AxisColor.G) / 255f);
                liCoordNColors.Add(((float)refAppViewModel.AxisColor.B) / 255f);
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

    }//cAxis

}//}//namespace RaupCoilerII

