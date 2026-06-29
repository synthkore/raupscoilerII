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
                    // each face has 3 vertices + 3 colors + 1 transparency + 3 normals
                    // vertex coords
                    axisCoordNColors.Add((float)mesh.Vertices[iAux2].X);
                    axisCoordNColors.Add((float)mesh.Vertices[iAux2].Y);
                    axisCoordNColors.Add((float)mesh.Vertices[iAux2].Z);
                    // color
                    axisCoordNColors.Add((float)fR);
                    axisCoordNColors.Add((float)fG);
                    axisCoordNColors.Add((float)fB);
                    axisCoordNColors.Add(1.0f);// alpha:transparency
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

    }//cZYXaxis

}//namespace RaupCoilerII
