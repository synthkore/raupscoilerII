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
    * @brief base class that defines the common properties and methods of any of the
    * 3D objects rendered in the application.
    *******************************************************************************/
    public class object3D {

        protected ViewModel refAppViewModel; // keep a reference to the ViewModel to execute some operations

        // OpenGL/Graphic card object data:
        // handlers of the OpenGL buffers that contain vertices and faces of that object
        public int objectVAO;
        protected int objectVBO;
        protected int objectEBO;

        // list of meshes that keeps the copy of the model geometry. The information will be copied 
        // from that buffers to the OpenGL buffers every time there are chenges on it
        protected List<Mesh> liMeshesOriginal = null; // list with the original meshes of the object

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

        /*******************************************************************************
        * @brief initializes the geometry of the object (liMeshesOriginal). It may initialize
        * the vertex and faces of that object, programatically, or by loading them from a file. 
        *******************************************************************************/
        public virtual void InitializeGeometry() { }

        /*******************************************************************************
        * @brief stores the mesh into the liMehsesTransformed and uses it to update that 
        * information in the VAO ( VBO and EBO )
        *******************************************************************************/
        public virtual void UpdateVAO(){ }

        /*******************************************************************************
        * @brief Auxiliar method that clones the received mesh into a new mesh
        * @return the cloned mesh.
        *******************************************************************************/
        public static Mesh CloneMesh(Mesh source) {
            var dest = new Mesh(source.Name, source.PrimitiveType);

            // Vertices
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





}
