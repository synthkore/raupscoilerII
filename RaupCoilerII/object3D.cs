using Assimp;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Assimp;
using OpenTK.Core;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Wpf;

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
        public Int32 iIndicesFacesCter = 0; // the total number of vertex INDEXs used to the define the object faces, some of the index may be repeated, because different faces may share some vertices. It is required by the DrawElements to know how many face indexs is goint to process.

        // model parameters
        public float objModelAngleX = 0f;
        public float objModelAngleY = 0f;
        public float objModelAngleZ = 0f;
        public float objModelScale = 1f;
        public float objModelTransX = 0f;
        public float objModelTransY = 0f;
        public float objModelTransZ = 0f;

        public virtual void Initialize() { }
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
        public Vector3 cameraRight;
        public Vector3 cameraUp;
        public Vector3 cameraForward;

        /*******************************************************************************
        * @brief Constructor with pararameters.
        * @param[in] _refViewModel reference to the VieModel object that will be used 
        * to retrive some information.
        *******************************************************************************/
        public cShellModel(ViewModel _refViewModel) {

            refAppViewModel = _refViewModel;
            cameraRight = new Vector3();
            cameraUp = new Vector3();
            cameraForward = new Vector3();

        }//object3D


        /*******************************************************************************
        * @brief generates all the vertexs of the shell geometry.
        * 
        * @param[in|out] model_mesh with all the generated vertexs are stored into that object.
        * @param[in] iVertexsPerRing the number of vertex that compose each shell ring.
        * @param[in] iTotalNumOfRings the total number of rings in the model
        *******************************************************************************/
        public void GenerateShellVertexs(ref Mesh model_mesh, int iVertexsPerRing, int iTotalNumOfRings) {
            Assimp.Vector3D vectorAux1;
            int iAux = 0;
            float fAngle = 0;
            float fAngleRads = 0;
            float fAngleDelta = 0;
            float fXAux, fYAux, fZAux;
            float fRadius = 0;
            int iRing = 0;
            int iVertexIndex = 0;

            fAngleDelta = 360f / (float)iVertexsPerRing;

            fZAux = 0;
            fRadius = 0.5f;
            for (iRing = 0; iRing < iTotalNumOfRings; iRing++) {

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

        }//GenerateShellVertexs

        /*******************************************************************************
        * @brief generates the normals for all the vertices of the shell geometry.
        * 
        * @param[in|out] model_mesh with all the geometry information. Here is where
        * the generated vertex, normals, and faces information is stored
        * @param[in] iVertexsPerRing the number of vertex that compose each shell ring
        * @param[in] iTotalNumOfRings the total number of rings in the shell model
        *******************************************************************************/
        public void GenerateShellNormals(ref Mesh model_mesh, int iVertexsPerRing, int iTotalNumOfRings) {
            Assimp.Vector3D vectorAux1;
            Assimp.Vector3D vectorAux2;
            Assimp.Vector3D vertexNormal;
            int iAux = 0;
            float fXAux, fYAux, fZAux;
            float fXAux2, fYAux2, fZAux2;
            int iRing = 0;
            int iVertexIndex = 0;

            fZAux = 0;
            for (iRing = 0; iRing < (iTotalNumOfRings - 1); iRing++) {

                iAux = iRing * iVertexsPerRing;

                // calculate all the normals of vertexs of the ring
                for (iVertexIndex = 0; iVertexIndex < iVertexsPerRing - 1; iVertexIndex++) {

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

                    fXAux = model_mesh.Vertices[iAux + iVertexIndex].X;
                    fYAux = model_mesh.Vertices[iAux + iVertexIndex].Y;
                    fZAux = model_mesh.Vertices[iAux + iVertexIndex].Z;

                    // get the vector that joins the current vertex with the equivalente vertex in the next face
                    fXAux2 = model_mesh.Vertices[iAux + iVertexIndex + 1].X;
                    fYAux2 = model_mesh.Vertices[iAux + iVertexIndex + 1].Y;
                    fZAux2 = model_mesh.Vertices[iAux + iVertexIndex + 1].Z;

                    vectorAux1 = new Assimp.Vector3D(fXAux - fXAux2, fYAux - fYAux2, fZAux - fZAux2);

                    // get the vector that joins the current vertex with the equivalente vertex in the next face
                    fXAux2 = model_mesh.Vertices[iAux + iVertexIndex + iVertexsPerRing].X;
                    fYAux2 = model_mesh.Vertices[iAux + iVertexIndex + iVertexsPerRing].Y;
                    fZAux2 = model_mesh.Vertices[iAux + iVertexIndex + iVertexsPerRing].Z;

                    vectorAux2 = new Assimp.Vector3D(fXAux - fXAux2, fYAux - fYAux2, fZAux - fZAux2);

                    // calculate the normal of that vertex and add it to the vertex list
                    vertexNormal = new Assimp.Vector3D();
                    vertexNormal = Assimp.Vector3D.Cross(vectorAux1, vectorAux2);
                    vertexNormal.Normalize();

                    model_mesh.Normals.Add(vertexNormal);

                }// for (iVertexIndex = 0

                // calculate the normal of the last vertex of the ring 
                fXAux = model_mesh.Vertices[iAux + iVertexIndex].X;
                fYAux = model_mesh.Vertices[iAux + iVertexIndex].Y;
                fZAux = model_mesh.Vertices[iAux + iVertexIndex].Z;

                // get the vector that joins the current vertex with the equivalente vertex in the next face
                fXAux2 = model_mesh.Vertices[iAux].X;
                fYAux2 = model_mesh.Vertices[iAux].Y;
                fZAux2 = model_mesh.Vertices[iAux].Z;

                vectorAux1 = new Assimp.Vector3D(fXAux - fXAux2, fYAux - fYAux2, fZAux - fZAux2);

                // get the vector that joins the current vertex with the equivalente vertex in the next face
                fXAux2 = model_mesh.Vertices[iAux + iVertexIndex + iVertexsPerRing].X;
                fYAux2 = model_mesh.Vertices[iAux + iVertexIndex + iVertexsPerRing].Y;
                fZAux2 = model_mesh.Vertices[iAux + iVertexIndex + iVertexsPerRing].Z;

                vectorAux2 = new Assimp.Vector3D(fXAux - fXAux2, fYAux - fYAux2, fZAux - fZAux2);

                // calculate the normal of that vertex and add it to the vertex list
                vertexNormal = new Assimp.Vector3D();
                vertexNormal = Assimp.Vector3D.Cross(vectorAux1, vectorAux2);
                vertexNormal.Normalize();

                model_mesh.Normals.Add(vertexNormal);

            }//for (iRing = 0;

            iAux = iRing * iVertexsPerRing;

            // calculate all the normals of vertexs of the last ring
            for (iVertexIndex = 0; iVertexIndex < iVertexsPerRing - 1; iVertexIndex++) {

                fXAux = model_mesh.Vertices[iAux + iVertexIndex].X;
                fYAux = model_mesh.Vertices[iAux + iVertexIndex].Y;
                fZAux = model_mesh.Vertices[iAux + iVertexIndex].Z;

                // get the vector that joins the current vertex with the equivalente vertex in the next face
                fXAux2 = model_mesh.Vertices[iAux + iVertexIndex + 1].X;
                fYAux2 = model_mesh.Vertices[iAux + iVertexIndex + 1].Y;
                fZAux2 = model_mesh.Vertices[iAux + iVertexIndex + 1].Z;

                vectorAux1 = new Assimp.Vector3D(fXAux - fXAux2, fYAux - fYAux2, fZAux - fZAux2);

                // get the vector that joins the current vertex with the equivalente vertex in the next face
                fXAux2 = model_mesh.Vertices[iAux + iVertexIndex - iVertexsPerRing].X;
                fYAux2 = model_mesh.Vertices[iAux + iVertexIndex - iVertexsPerRing].Y;
                fZAux2 = model_mesh.Vertices[iAux + iVertexIndex - iVertexsPerRing].Z;

                vectorAux2 = new Assimp.Vector3D(fXAux2 - fXAux, fYAux2 - fYAux, fZAux2 - fZAux);

                // calculate the normal of that vertex and add it to the vertex list
                vertexNormal = new Assimp.Vector3D();
                vertexNormal = Assimp.Vector3D.Cross(vectorAux1, vectorAux2);
                vertexNormal.Normalize();

                model_mesh.Normals.Add(vertexNormal);

            }//for

            // calculate the normal of the last vertex of the last ring

            fXAux = model_mesh.Vertices[iAux + iVertexIndex].X;
            fYAux = model_mesh.Vertices[iAux + iVertexIndex].Y;
            fZAux = model_mesh.Vertices[iAux + iVertexIndex].Z;

            // get the vector that joins the current vertex with the equivalente vertex in the next face
            fXAux2 = model_mesh.Vertices[iAux].X;
            fYAux2 = model_mesh.Vertices[iAux].Y;
            fZAux2 = model_mesh.Vertices[iAux].Z;

            vectorAux1 = new Assimp.Vector3D(fXAux - fXAux2, fYAux - fYAux2, fZAux - fZAux2);

            // get the vector that joins the current vertex with the equivalente vertex in the next face
            fXAux2 = model_mesh.Vertices[iAux + iVertexIndex - iVertexsPerRing].X;
            fYAux2 = model_mesh.Vertices[iAux + iVertexIndex - iVertexsPerRing].Y;
            fZAux2 = model_mesh.Vertices[iAux + iVertexIndex - iVertexsPerRing].Z;

            vectorAux2 = new Assimp.Vector3D(fXAux2 - fXAux, fYAux2 - fYAux, fZAux2 - fZAux);

            // calculate the normal of that vertex and add it to the vertex list
            vertexNormal = new Assimp.Vector3D();
            vertexNormal = Assimp.Vector3D.Cross(vectorAux1, vectorAux2);
            vertexNormal.Normalize();

            model_mesh.Normals.Add(vertexNormal);

        }//GenerateShellNormals

        /*******************************************************************************
        * @brief fills the faces information by setting the indexs of the vertex that 
        * define all the faces of the shell geometry.
        * 
        * @param[in|out] model_mesh with all the geometry information. Here is where
        * the generated vertex, normals, and faces information is stored
        * @param[in] iVertexsPerRing the number of vertex that compose each shell ring.
        * @param[in] iTotalNumOfRings the total number of rings in the shell model
        *******************************************************************************/
        public void GenerateShellFaces(ref Mesh model_mesh, int iVertexsPerRing, int iTotalNumOfRings) {
            Assimp.Face faceAux;
            int iRing = 0;
            int iVertexIndex = 0;
            int iAux = 0;

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
            //   face 2: 0 3 2

            // generate the faces index
            for (iRing = 0; iRing < (iTotalNumOfRings - 1); iRing++) {

                iAux = iRing * iVertexsPerRing;

                for (iVertexIndex = 0; iVertexIndex < (iVertexsPerRing - 1); iVertexIndex++) {

                    // face 1 
                    faceAux = new Face();
                    faceAux.Indices.Add(iAux + iVertexIndex);
                    faceAux.Indices.Add(iAux + iVertexIndex + 1);
                    faceAux.Indices.Add(iAux + iVertexIndex + 1 + iVertexsPerRing);
                    model_mesh.Faces.Add(faceAux);

                    // face 2 
                    faceAux = new Face();
                    faceAux.Indices.Add(iAux + iVertexIndex );
                    faceAux.Indices.Add(iAux + iVertexIndex + 1 + iVertexsPerRing);
                    faceAux.Indices.Add(iAux + iVertexIndex + iVertexsPerRing);                                                                            
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
                //   face 2: 0 3 2

                // last face 1 of the ring
                faceAux = new Face();
                faceAux.Indices.Add(iAux + iVertexIndex);
                faceAux.Indices.Add(iAux);
                faceAux.Indices.Add(iAux + iVertexsPerRing);
                model_mesh.Faces.Add(faceAux);

                // last face 2 of the ring
                faceAux = new Face();
                faceAux.Indices.Add(iAux + iVertexIndex);
                faceAux.Indices.Add(iAux + iVertexsPerRing);
                faceAux.Indices.Add(iAux + iVertexIndex + iVertexsPerRing);                                               
                model_mesh.Faces.Add(faceAux);

            }//for (iRing = 0; iRing < iNumberOfRings; iRing++) {

        }//GenerateShellFaces

        /*******************************************************************************
        * @brief method that generates/loads the shell model according to the parameters
        * set by the user
        *******************************************************************************/
        public void GenerateShellModel(ref Scene scene) {
            Mesh model_mesh;
            Assimp.Vector3D vectorAux1;
            Assimp.Vector3D vectorAux2;
            Assimp.Vector3D vertexNormal;
            Assimp.Face faceAux;
            int iAux = 0;
            float fAngle = 0;
            float fAngleRads = 0;
            float  fAngleDelta = 0;
            float fXAux,fYAux, fZAux;
            float fXAux2, fYAux2, fZAux2;
            float fRadius = 0;
            int iVertexsPerRing = 0;
            int iNumberOfRings = 8;
            int iRing = 0;
            int iVertexIndex = 0;
            
            model_mesh = new Mesh("Shell", Assimp.PrimitiveType.Triangle);

            scene.Meshes.Add(model_mesh);

            iVertexsPerRing = refAppViewModel.Resolution;
            iNumberOfRings = refAppViewModel.Revolutions * refAppViewModel.RingsRev;

            GenerateShellVertexs(ref model_mesh, iVertexsPerRing, iNumberOfRings);

            GenerateShellNormals(ref model_mesh, iVertexsPerRing, iNumberOfRings);

            GenerateShellFaces(ref model_mesh, iVertexsPerRing, iNumberOfRings);


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

        }//object3D

        /*******************************************************************************
        * @brief method that generates/loads the object geometry and stores it into a 
        * mesh object
        *******************************************************************************/
        public override void Initialize() {
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

            // calculate the model rotation matrix that allows to rotate the model arround the current view vector ( the camera vectors ) 
            // with the angles set by the user when clicking an draggin over OGL render control. This rotation is achived using
            // Quaternions from the OpenTK.Mathematics lib wich also allow rotating the object arround each axis and then combining
            // then in a single rotation matrix.
            OpenTK.Mathematics.Quaternion rotCameraUpAxis = OpenTK.Mathematics.Quaternion.FromAxisAngle(cameraUp, MathHelper.DegreesToRadians((float)-refAppViewModel.YOrientation));
            OpenTK.Mathematics.Quaternion rotCamerarRightAxis = OpenTK.Mathematics.Quaternion.FromAxisAngle(cameraRight, MathHelper.DegreesToRadians((float)-refAppViewModel.XOrientation));
            // commbine both axis rotations into a single Quaternion
            OpenTK.Mathematics.Quaternion rotCombined = rotCameraUpAxis * rotCamerarRightAxis;
            // nomralize the combined Quaternion
            rotCombined = rotCombined.Normalized();
            // get the rotation matrix that corresponds to the obtained Quaternion
            OpenTK.Mathematics.Matrix4 rotMatrix = Matrix4.CreateFromQuaternion(rotCombined);

            // to multiply the OpenTK Rotation Matrix4 with the Assimp.net vertices we must convert the
            // obtained OpenTK.Mathematics.Matrix4 matrix to an Assimp.Matrix4x4 matrix.            
            rotationMatrix = new Assimp.Matrix4x4(
                rotMatrix.M11, rotMatrix.M12, rotMatrix.M13, rotMatrix.M14,
                rotMatrix.M21, rotMatrix.M22, rotMatrix.M23, rotMatrix.M24,
                rotMatrix.M31, rotMatrix.M32, rotMatrix.M33, rotMatrix.M34,
                rotMatrix.M41, rotMatrix.M42, rotMatrix.M43, rotMatrix.M44
            );

            // reset the rotation angles previously set by the user when clickng and draggin on the screen.
            refAppViewModel.XOrientation = 0;
            refAppViewModel.YOrientation = 0;
            refAppViewModel.ZOrientation = 0;

            // 3 vertex floats   + 3 color floats  + 3 normal floats
            modelCoordNColors = new List<float>();

            // apply the obtained rotation matrix over the vertices and the normals of the model           
            iVerticesCter = liTransformedMesh[0].Vertices.Count();
            for (iAux = 0; iAux < iVerticesCter; iAux++) {
                // first apply the transformations
                liTransformedMesh[0].Vertices[iAux] = rotationMatrix * liTransformedMesh[0].Vertices[iAux];
                liTransformedMesh[0].Normals[iAux] = rotationMatrix * liTransformedMesh[0].Normals[iAux];
            }//for

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
                modelCoordNColors.Add(((float)refAppViewModel.MeshColor.R) / 255);
                modelCoordNColors.Add(((float)refAppViewModel.MeshColor.G) / 255);
                modelCoordNColors.Add(((float)refAppViewModel.MeshColor.B) / 255);
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
        public override void Initialize() {
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
