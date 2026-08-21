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
    public class cGizmo : object3D {

        // the three vectors defined by the view vector
        public Vector3 cameraRight;
        public Vector3 cameraUp;
        public Vector3 cameraForward;

        /*******************************************************************************
        * @brief Constructor with pararameters.
        * @param[in] _refViewModel reference to the VieModel object that will be used 
        * to retrive some information.
        *******************************************************************************/
        public cGizmo(ViewModel _refViewModel) {

            refAppViewModel = _refViewModel;

        }//object3D

        /*******************************************************************************
        * @brief initializes the object geometry ( vertex, normals, faces ) wit the 
        * information in the file.
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

            // the gizmo is a list of 3 mehses. Each mesh corresponds to an axis. 
            liMeshesOriginal = scene.Meshes;

            objModelScale = 0.15f;
            objModelTransX = -0.5f;
            objModelTransY = -0.5f;
            objModelTransZ = -0.5f;

        }//Initialize

        /*******************************************************************************
        * @brief updates the VAO ( VBO and EBO ) with that data.
        *******************************************************************************/
        public override void UpdateVAO() {
            int iStride = 0;
            float fR = 0, fG = 0, fB = 0;
            List<float> liCoordNColors = null;
            List<uint> liFaceIdxs = null;
            Mesh meshAux;
            int iVertexCounterAux = 0; // used to calculate and initialize the face index of the faces of the different meshes in a single mesh
            int iAux = 0;
            int iAux2 = 0;


            // ### update the content of the lists that will be used to intialize the VAO ( VBO and EBO )

            // prepare list for the VBO with 3 vertex floats   + 3 color floats  + 3 normal floats for each
            // vertex. As the file with the gizmo is organized in 3 meshes ( one for each axis ) we have
            // to process each of the three axis independently when adding it to the list to assing a different
            // color to each arrow.
            liCoordNColors = new List<float>();
            iVerticesCter = 0;
            for (iAux = 0; iAux < 3; iAux++) {

                meshAux = liMeshesOriginal[iAux];

                // assign a different color to each mesh of the object ( each mesh is an arrow of the axis)
                if (iAux == 0) { fR = 0; fG = 0; fB = 1; }
                if (iAux == 1) { fR = 0; fG = 1; fB = 0; }
                if (iAux == 2) { fR = 1; fG = 0; fB = 0; }

                for (iAux2 = 0; iAux2 < meshAux.Vertices.Count(); iAux2++) {
                    // each face has 3 vertices + 3 colors + 1 transparency + 3 normals
                    // vertex coords
                    liCoordNColors.Add((float)meshAux.Vertices[iAux2].X);
                    liCoordNColors.Add((float)meshAux.Vertices[iAux2].Y);
                    liCoordNColors.Add((float)meshAux.Vertices[iAux2].Z);
                    // color
                    liCoordNColors.Add((float)fR);
                    liCoordNColors.Add((float)fG);
                    liCoordNColors.Add((float)fB);
                    liCoordNColors.Add(1.0f);// alpha:transparency
                    // normal
                    liCoordNColors.Add((float)meshAux.Normals[iAux2].X);
                    liCoordNColors.Add((float)meshAux.Normals[iAux2].Y);
                    liCoordNColors.Add((float)meshAux.Normals[iAux2].Z);
                }//for

                iVerticesCter = iVerticesCter + meshAux.Vertices.Count();

            }//for

            // faces indexs
            // prepare list for the EBO with the indexs that define the faces of the 3D XYZ Axis
            liFaceIdxs = new List<uint>();
            iFacesCter = 0;
            iIndicesFacesCter = 0;
            iVertexCounterAux = 0;
            for (iAux = 0; iAux < 3; iAux++) {

                meshAux = liMeshesOriginal[iAux];
                iFacesCter = iFacesCter + meshAux.Faces.Count();

                for (iAux2 = 0; iAux2 < meshAux.Faces.Count(); iAux2++) {

                    // each face has 3 vertex indexs because we us PostProcessSteps.Triangulate
                    liFaceIdxs.Add((uint)(iVertexCounterAux + meshAux.Faces[iAux2].Indices[0]));
                    liFaceIdxs.Add((uint)(iVertexCounterAux + meshAux.Faces[iAux2].Indices[1]));
                    liFaceIdxs.Add((uint)(iVertexCounterAux + meshAux.Faces[iAux2].Indices[2]));

                    iIndicesFacesCter = iIndicesFacesCter + 3;

                }//for

                iVertexCounterAux = iVertexCounterAux + meshAux.Vertices.Count();

            }//for

            // #### initalize the ZYX axis VAO (VBO and EBO)  with the attributes, vertices and faces in the lists

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

    }//cGizmo

}//namespace RaupCoilerII
