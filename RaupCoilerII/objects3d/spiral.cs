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
       * @brief class that implements the methods related to the shell model 3D spiral
       *******************************************************************************/
    public class cSpiral : object3D {

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
        * @param[in|out] model_mesh with all the generated vertexs are stored into that object.
        *******************************************************************************/
        private void GenerateSpiral(ref Mesh spiral_mesh) {
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


            // calculate some parameters that will be used to generate the shell 3D model
            iTotalNumberOfRings = refAppViewModel.Revolutions * refAppViewModel.RingsRev; // rings in the complete model: is the number of rings per revolution multiplied by the number of revolutions
            dAngleRingsRad = ((double)refAppViewModel.Revolutions * (2.0d * (double)Math.PI)) / (double)iTotalNumberOfRings;

            // calculate the Rc value according to the confiugred D and fRadius parameter
            dRc = (double)refAppViewModel.IniRadius * (1.0d + (double)refAppViewModel.Dparam) / (1.0d - (double)refAppViewModel.Dparam);

            dThetaRad = 0;
            for (iRing = 0; iRing < iTotalNumberOfRings; iRing++) {

                // calculate the W^(𝜃/2π) for the current Theta value
                dAux = (double)Math.Pow((double)refAppViewModel.Wparam, (double)(dThetaRad / (double)(2.0d * Math.PI)));// W^(1/refAppViewModel.RingsRev)

                // scale the current "dThetaRad" ring: calculate all the vertices of the current ring of the shell
                // for (iVertexIndex = 0; iVertexIndex < iVerticesPerGenCurve; iVertexIndex++) {

                    // apply the "Raup & Michelson" model parameters on the generting curve center of coordinates: (0,0)
                    dXAux = ((double)0.0f + dRc) * dAux;
                    dYAux = ((double)0.0f * dAux) - (dRc * (double)refAppViewModel.Tparam * (dAux - 1.0d));
                    dZAux = 0;

                    // apply the corresponding "dThetaRad" rotation over the transformed generating curve. This 
                    // converts from cylindrical coordinates to cartesian coordinates ( r,y,theta -> x,y,z )
                    fXAux = (float)((dXAux * Math.Cos(-dThetaRad)) + (dZAux * Math.Sin(-dThetaRad)));
                    fYAux = (float)dYAux;
                    fZAux = (float)(-(dXAux * Math.Sin(-dThetaRad)) + (dZAux * Math.Cos(-dThetaRad)));

                    // apply the previously calculated scale factor
                    fXAux = fXAux * objModelScale;
                    fYAux = fYAux * objModelScale;
                    fZAux = fZAux * objModelScale;

                    // store the calculated vertex coordinates into the mesh structure
                    vectorAux1 = new Assimp.Vector3D(fXAux, fYAux, fZAux);
                    spiral_mesh.Vertices.Add(vectorAux1);

                // }//for

                // increase the Theta angle of the following shell model ring
                dThetaRad = dThetaRad + dAngleRingsRad;

                // apply the W param on the next ring radius
                // fCurrRingRadius = fCurrRingRadius * flWRing;

            }//for (iRings=0;

        }//GenerateSpiral

        /*******************************************************************************
        * @brief initializes the object geometry ( vertex, normals, faces ) wit the 
        * information in the file.
        *******************************************************************************/
        public void InitializeGeometry() {

            var importer = new AssimpContext();
            // Acceder al primer Mesh (malla) del archivo
            int iAux = 0;
            Mesh model_mesh = null;

            model_mesh = new Mesh();

            // call the method that procedurally generates the shell model geometry and stores it into the Scene object
            GenerateSpiral(ref model_mesh);

            liOriginaldMesh = new List<Mesh>();
            liOriginaldMesh.Add(new Mesh()); // create a new empty mesh object at index 0
            liOriginaldMesh[0] = object3D.CloneMesh(model_mesh);// clone the object in the index 0 mesh

        }//InitializeGeometry

        /*******************************************************************************
        * @brief transforms the original geometry (liOriginaldMesh) with the object 
        * current transformation values ( liTransformedMesh ) and then updates the VAO
        * ( VBO and EBO ) with that data.
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

            mesh = liOriginaldMesh[0];

            for (iAux2 = 0; iAux2 < mesh.Vertices.Count(); iAux2++) {
                // each face has 3 vertices + 3 colors + 1 transparency + 3 normals
                // vertex coords
                liCoordNColors.Add((float)mesh.Vertices[iAux2].X);
                liCoordNColors.Add((float)mesh.Vertices[iAux2].Y);
                liCoordNColors.Add((float)mesh.Vertices[iAux2].Z);
                // color
                liCoordNColors.Add((float)1.0f);
                liCoordNColors.Add((float)0.0f);
                liCoordNColors.Add((float)0.0f);
                liCoordNColors.Add(0.3f);// alpha:transparency
                // normal
                liCoordNColors.Add((float)1.0f);// mesh.Normals[iAux2].X);
                liCoordNColors.Add((float)1.0f);//mesh.Normals[iAux2].Y);
                liCoordNColors.Add((float)1.0f);//mesh.Normals[iAux2].Z);
            }//for

            iVerticesCter = mesh.Vertices.Count();

            // faces indexs
            // add to the list the indexs that define the faces of the 3D Cube
            liFaceIdxs = new List<uint>();
            iFacesCter = 0;
            iIndicesFacesCter = 0;

            mesh = liOriginaldMesh[0];

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

        }//TransformUpdateVAO

    }//cSpiral

}//}//namespace RaupCoilerII

