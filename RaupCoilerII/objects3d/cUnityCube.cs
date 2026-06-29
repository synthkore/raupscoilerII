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
       * @brief class that implements the methods of the shell -1,+1,-1,+1 container 
       * cube.
       *******************************************************************************/
    public class cUnityCube : object3D {

        /*******************************************************************************
        * @brief Constructor with pararameters.
        * @param[in] _refViewModel reference to the VieModel object that will be used 
        * to retrive some information.
        *******************************************************************************/
        public cUnityCube(ViewModel _refViewModel) {

            refAppViewModel = _refViewModel;

        }//object3D

        /*******************************************************************************
        * @brief method that generates/loads the object geometry and stores it into a 
        * mesh object
        *******************************************************************************/
        public void InitializeGeometry() {

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
            scene = importer.ImportFile("Cube.obj", postProcessSteps);

            if (scene == null || scene.SceneFlags.HasFlag(SceneFlags.Incomplete) || scene.RootNode == null) {
                Console.WriteLine("Error loading the Cube model.");
                return;
            }

            // modelOriginalZYXAxis is a list of 3 mehses. Each mesh corresponds to an axis. 
            liOriginaldMesh = scene.Meshes;

            objModelScale = 1.0f;
            objModelTransX = 0f;
            objModelTransY = 0f;
            objModelTransZ = 0f;

        }//InitializeGeometry

        /*******************************************************************************
        * @brief applies the transformation over the object mesh vertices and faces and 
        * then updates that information in the VAO ( VBO and EBO )
        *******************************************************************************/
        public void TransformUpdateVAO() {
            int iStride = 0;
            float fR = 0, fG = 0, fB = 0;
            List<float> cubeCoordNColors = null;
            List<uint> cubeFaceIdxs = null;
            Mesh mesh;
            int iAux = 0;
            int iAux2 = 0;


            // ### update the content of the lists that will be used to intialize the VAO ( VBO and EBO )

            // 3 vertex floats   + 3 color floats  + 3 normal floats
            // add to the list of vertexs the coordinates of the 3D Cube
            cubeCoordNColors = new List<float>();
            iVerticesCter = 0;

            mesh = liOriginaldMesh[0];

            for (iAux2 = 0; iAux2 < mesh.Vertices.Count(); iAux2++) {
                // each face has 3 vertices + 3 colors + 1 transparency + 3 normals
                // vertex coords
                cubeCoordNColors.Add((float)mesh.Vertices[iAux2].X);
                cubeCoordNColors.Add((float)mesh.Vertices[iAux2].Y);
                cubeCoordNColors.Add((float)mesh.Vertices[iAux2].Z);
                // color
                cubeCoordNColors.Add((float)1.0f);
                cubeCoordNColors.Add((float)0.0f);
                cubeCoordNColors.Add((float)0.0f);
                cubeCoordNColors.Add(0.3f);// alpha:transparency
                // normal
                cubeCoordNColors.Add((float)mesh.Normals[iAux2].X);
                cubeCoordNColors.Add((float)mesh.Normals[iAux2].Y);
                cubeCoordNColors.Add((float)mesh.Normals[iAux2].Z);
            }//for

            iVerticesCter = mesh.Vertices.Count();

            // faces indexs
            // add to the list the indexs that define the faces of the 3D Cube
            cubeFaceIdxs = new List<uint>();
            iFacesCter = 0;
            iIndicesFacesCter = 0;

            mesh = liOriginaldMesh[0];

            for (iAux2 = 0; iAux2 < mesh.Faces.Count(); iAux2++) {

                // each face has 3 vertex indexs because we us PostProcessSteps.Triangulate
                cubeFaceIdxs.Add((uint)(mesh.Faces[iAux2].Indices[0]));
                cubeFaceIdxs.Add((uint)(mesh.Faces[iAux2].Indices[1]));
                cubeFaceIdxs.Add((uint)(mesh.Faces[iAux2].Indices[2]));

                iIndicesFacesCter = iIndicesFacesCter + 3;

            }//for

            iFacesCter = mesh.Faces.Count();

            // #### initalize the Cube VAO (VBO and EBO)  with the attributes, vertices and faces in the lists

            objectVAO = GL.GenVertexArray();
            objectVBO = GL.GenBuffer();
            objectEBO = GL.GenBuffer();

            // set the VAO in context
            GL.BindVertexArray(objectVAO);

            // set in context and initalize the VBO
            GL.BindBuffer(BufferTarget.ArrayBuffer, objectVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, cubeCoordNColors.Count * sizeof(float), cubeCoordNColors.ToArray(), BufferUsageHint.StaticDraw);

            // set in context and initalize the EBO
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, objectEBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, cubeFaceIdxs.Count * sizeof(uint), cubeFaceIdxs.ToArray(), BufferUsageHint.StaticDraw);

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

    }//cUnityCube

}//}//namespace RaupCoilerII

