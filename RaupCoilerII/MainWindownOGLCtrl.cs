using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Assimp;
using OpenTK.Core;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Wpf;

namespace RaupCoilerII {

    /*****************************************************************************
    * @brief Partial class with the methods and properties related to the Open GL control.
    ***************************************************************************/
    public partial class MainWindow : Window {

        // Open GL related parameters
        private int shaderProgram;
        // Model data
        private int vaoModel;
        private int vboModel;
        private int eboModel;
        float[] flArrayReadModel = null;
        int[] iIdxsReadModel = null;
        Int32 iModelIdxCter = 0;
        
        // Axis data
        private int vaoAxis;
        private int vboAxis;
        private int eboAxis;
        float[] flArrayReadAxis = null;
        int[] iIdxsReadAxis = null;
        Int32 iAxisIdxCter = 0;

        // model parameters
        private float objAngleX = 0f;
        private float objAngleY = 0f;
        private float objAngleZ = 0f;
        private float objScale = 0f;
        private float objTransX = 0f;
        private float objTransY = 0f;
        private float objTransZ = 0f;
        
        // view coordinates
        private float viewXAngle = 0f;
        private float viewYAngle = 0f;
        private float viewDist = 0f;
        private float viewX = 0f;
        private float viewY = 0f;
        private float viewZ = 0f;
        // light coordinates
        private float lightXAngle = 0f;
        private float lightYAngle = 0f;
        private float lightDist = 0f;
        private float lightX = 0f;
        private float lightY = 0f;
        private float lightZ = 0f;
        // light parameters
        private float ambient = 0f;
        private float difuse = 0f;
        private int shine = 0;

        /*****************************************************************************
         * @brief Initialzies the OpenTK library
         ***************************************************************************/
        private void InitializeOpenTK() {

            // initialize the OpenGL WPF control
            var settings = new GLWpfControlSettings {
                MajorVersion = 4,
                MinorVersion = 6
            };

            OpenTkControl.Start(settings);

            //myGLControl = new GLControl {
            //    Dock = DockStyle.Fill
            //};
            // this.Controls.Add(myGLControl);

            // var timer = new System.Windows.Forms.Timer();
            // timer.Interval = 16; // 60 FPS
            // timer.Tick += (s, e) => {
            //     angle += 1f;
            //     myGLControl.Invalidate();
            // 
            // };
            // timer.Start();

        }//InitializeOpenTK

        /*****************************************************************************
         * @brief Creates the shaders that process the model data in the graphic card
         ***************************************************************************/
        private void CreateShaders() {
            int status; ;

            string vertexShaderSource = @"
                        #version 330 core

                        layout (location = 0) in vec3 aPos;
                        layout (location = 1) in vec3 aColor;
                        layout (location = 2) in vec3 aNormal;

                        uniform mat4 uModel;
                        uniform mat4 uView;
                        uniform mat4 uProjection;

                        out vec3 FragPos;
                        out vec3 Color;
                        out vec3 Normal;

                        void main()
                        {
                            FragPos = vec3(uModel * vec4(aPos, 1.0));
                            Color = aColor;
                            Normal = mat3(transpose(inverse(uModel))) * aNormal;

                            gl_Position = uProjection * uView * vec4(FragPos, 1.0);
                        }
                        ";

            string fragmentShaderSource = @"
                        #version 330 core

                        in vec3 FragPos;
                        in vec3 Color;
                        in vec3 Normal;

                        out vec4 FragColor;

                        uniform vec3 lightPos;
                        uniform vec3 viewPos;                        
                        uniform vec3 lightColor;
                        
                        // Intensidades
                        uniform bool isLightSource; // flat that indicates if the lighting calculations must be applied over the rendered object 
                        uniform float ambientStrength;
                        uniform float specularStrength;
                        uniform int shininess;

                        void main()
                        {                            

                            if (isLightSource){
                            
                                // as the rendered object is considered as a light emiting source, light calculations
                                // are omited and it is rendered at full color as a full emiting light source
                                FragColor = vec4(lightColor, 1.0);

                            }else{
                        
                                 // ambient light
                                vec3 ambient = ambientStrength * lightColor;      

                                // normalize to 1.0 the normal vector
                                vec3 norm = normalize(Normal);

                                // difuse (Lambert)
                                // calcula el vector de direccion de la luz ( es la diferencia
                                // entre la posición de la luz y la posición de la cara ) y luego
                                // lo normaliza
                                vec3 lightDir = normalize(lightPos - FragPos);
                                float diff = max(dot(norm, lightDir), 0.0);
                                vec3 diffuse = diff * lightColor;

                                // specular
                                vec3 viewDir = normalize(viewPos - FragPos);
                                vec3 reflectDir = reflect(-lightDir, norm);  
                                float spec = pow(max(dot(viewDir, reflectDir), 0.0), shininess);
                                vec3 specular = specularStrength * spec * lightColor;  

                                // final light calculations
                                vec3 result = ( ambient + diffuse + specular ) * Color;
                                FragColor = vec4(result, 1.0);

                            }//if

                        }
                        ";

            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);
            GL.CompileShader(vertexShader);

            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out status);
            if (status == (int)All.False) {
                string infoLog = GL.GetShaderInfoLog(vertexShader);
                Console.WriteLine("Error compiling shader!");
                Console.WriteLine(infoLog);
            } else {
                Console.WriteLine("Shader has been compiled!");
            }

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            GL.CompileShader(fragmentShader);

            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out status);
            if (status == (int)All.False) {
                string infoLog = GL.GetShaderInfoLog(fragmentShader);
                Console.WriteLine("Error compiling shader!");
                Console.WriteLine(infoLog);
            } else {
                Console.WriteLine("Shader has been compiled!");
            }

            shaderProgram = GL.CreateProgram();
            GL.AttachShader(shaderProgram, vertexShader);
            GL.AttachShader(shaderProgram, fragmentShader);
            GL.LinkProgram(shaderProgram);

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

        }//CreateShaders

        /*******************************************************************************
        * @brief method that genereates the shell the axis and the spiral geometry and
        * initializes the openGL data structures with that new generated data.
        *******************************************************************************/
        public void InitializeXYZAxis() {
            var importer = new AssimpContext();
            // Acceder al primer Mesh (malla) del archivo
            Mesh mesh = null;
            Scene scene = null;
            int iStride = 0;
            int iAux = 0;


            // initialize some OpenGL functionalites 
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.FrontFace(FrontFaceDirection.Ccw); // o Cw según tu modelo

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // ponemos en contexto el control GLControl sobre el que vamos a operar
            // if (!myGLControl.Context.IsCurrent) myGLControl.MakeCurrent();

            // Configuración de carga (Post-Procesamiento)
            // * Triangulate: Convierte todo a triángulos (vital para OpenGL)
            // * FlipUVs: Voltea las texturas (OpenGL tiene el origen de textura abajo-izq,
            // la mayoría de formatos arriba-izq)
            // * GenerateNormals: Si el modelo no tiene normales, las calcula.
            var postProcessSteps = PostProcessSteps.Triangulate |
                                    PostProcessSteps.FlipUVs |
                                    PostProcessSteps.GenerateNormals;

            // ######################### Loade the XYZ axis 3d model 

            // Cargamos la escena
            scene = importer.ImportFile("Axis.obj", postProcessSteps);

            // importer.ExportFile(scene, "salida", "stl");

            if (scene == null || scene.SceneFlags.HasFlag(SceneFlags.Incomplete) || scene.RootNode == null) {
                Console.WriteLine("Error loading the XYZ axis model.");
                return;
            }

            // 3 vertex floats   + 3 color floats  + 3 normal floats
            List<float> axisCoordNColors = new List<float>();
            // add to the list of vertexs the coordinates of the woman
            mesh = scene.Meshes[0];
            int iAxisTotalVertices = mesh.Vertices.Count();
            for (iAux = 0; iAux < iAxisTotalVertices; iAux++) {
                // each face has 3 vertices + 3 colors + 3 normals
                // vertex coords
                axisCoordNColors.Add((float)mesh.Vertices[iAux].X);
                axisCoordNColors.Add((float)mesh.Vertices[iAux].Y);
                axisCoordNColors.Add((float)mesh.Vertices[iAux].Z);
                // color
                axisCoordNColors.Add((float)1);
                axisCoordNColors.Add((float)0);
                axisCoordNColors.Add((float)0);
                // normal
                axisCoordNColors.Add((float)mesh.Normals[iAux].X);
                axisCoordNColors.Add((float)mesh.Normals[iAux].Y);
                axisCoordNColors.Add((float)mesh.Normals[iAux].Z);
            }//for

            // faces indexs
            // add to the list of the indexs of the vertexs that define the faces of the mode
            List<uint> axisFaceIdxs = new List<uint>();
            mesh = scene.Meshes[0];
            foreach (var face in mesh.Faces) {
                // each face has 3 index because we us PostProcessSteps.Triangulate
                axisFaceIdxs.Add((uint)face.Indices[0]);
                axisFaceIdxs.Add((uint)face.Indices[1]);
                axisFaceIdxs.Add((uint)face.Indices[2]);
            }
            iAxisIdxCter = axisFaceIdxs.Count;

            //  initalize the axis vao, vbo and ebo with the cube attributes, vertices and faces
            vaoAxis = GL.GenVertexArray();
            vboAxis = GL.GenBuffer();
            eboAxis = GL.GenBuffer();

            // set the VAO in context
            GL.BindVertexArray(vaoAxis);

            // set in context and initalize the VBO
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboAxis);
            GL.BufferData(BufferTarget.ArrayBuffer, axisCoordNColors.Count * sizeof(float), axisCoordNColors.ToArray(), BufferUsageHint.StaticDraw);

            // set in context and initalize the EBO
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboAxis);
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

        }//InitializeXYZAxis

        /*******************************************************************************
        * @brief method that genereates the shell the axis and the spiral geometry and
        * initializes the openGL data structures with that new generated data.
        *******************************************************************************/
        public void InitializeShellSpiralAxisGeometry() {
            var importer = new AssimpContext();
            // Acceder al primer Mesh (malla) del archivo
            Mesh mesh = null;
            int iStride = 0;
            int iAux = 0;
            // maximum and minimum coordinates values used to center the model
            float flMaxX = 0, flMaxY = 0, flMaxZ = 0;
            float flMinX = 0, flMinY = 0, flMinZ = 0;


            // initialize some OpenGL functionalites 
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.FrontFace(FrontFaceDirection.Ccw); // o Cw según tu modelo

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // ponemos en contexto el control GLControl sobre el que vamos a operar
            // if (!myGLControl.Context.IsCurrent) myGLControl.MakeCurrent();

            // Configuración de carga (Post-Procesamiento)
            // * Triangulate: Convierte todo a triángulos (vital para OpenGL)
            // * FlipUVs: Voltea las texturas (OpenGL tiene el origen de textura abajo-izq,
            // la mayoría de formatos arriba-izq)
            // * GenerateNormals: Si el modelo no tiene normales, las calcula.
            var postProcessSteps = PostProcessSteps.Triangulate |
                                    PostProcessSteps.FlipUVs |
                                    PostProcessSteps.GenerateNormals;


            // ######################### Initialize MODEL 

            // Cargamos la escena
            Scene scene = importer.ImportFile("TestOGL.obj", postProcessSteps);

            // importer.ExportFile(scene, "salida", "stl");

            if (scene == null || scene.SceneFlags.HasFlag(SceneFlags.Incomplete) || scene.RootNode == null) {
                Console.WriteLine("Error al cargar el modelo");
                return;
            }

            // 3 vertex floats   + 3 color floats  + 3 normal floats
            List<float> modelCoordNColors = new List<float>();
            // add to the list of vertexs the coordinates of the woman
            mesh = scene.Meshes[1];
            int iModelTotalVertices = mesh.Vertices.Count();
            for (iAux = 0; iAux < iModelTotalVertices; iAux++) {
                // each face has 3 vertices + 3 colors + 3 normals
                // vertex coords
                modelCoordNColors.Add((float)mesh.Vertices[iAux].X);
                modelCoordNColors.Add((float)mesh.Vertices[iAux].Y);
                modelCoordNColors.Add((float)mesh.Vertices[iAux].Z);
                // color
                modelCoordNColors.Add(((float)appViewModel.MeshColor.R) / 255);
                modelCoordNColors.Add(((float)appViewModel.MeshColor.G) / 255);
                modelCoordNColors.Add(((float)appViewModel.MeshColor.B) / 255);
                // normal
                modelCoordNColors.Add((float)mesh.Normals[iAux].X);
                modelCoordNColors.Add((float)mesh.Normals[iAux].Y);
                modelCoordNColors.Add((float)mesh.Normals[iAux].Z);
                // update the Maximum and mínimum coordinates values if necessary
                if (mesh.Vertices[iAux].X > flMaxX) {
                    flMaxX = mesh.Vertices[iAux].X;
                } else if (mesh.Vertices[iAux].X < flMinX) {
                    flMinX = mesh.Vertices[iAux].X;
                }
                if (mesh.Vertices[iAux].Y > flMaxY) {
                    flMaxY = mesh.Vertices[iAux].Y;
                } else if (mesh.Vertices[iAux].Y < flMinY) {
                    flMinY = mesh.Vertices[iAux].Y;
                }
                if (mesh.Vertices[iAux].Z > flMaxZ) {
                    flMaxZ = mesh.Vertices[iAux].Z;
                } else if (mesh.Vertices[iAux].Z < flMinZ) {
                    flMinZ = mesh.Vertices[iAux].Z;
                }
            }//for

            // faces indexs
            // add to the list of the indexs of the vertexs that define the faces of the mode
            List<uint> modelFaceIdxs = new List<uint>();
            mesh = scene.Meshes[1];
            foreach (var face in mesh.Faces) {
                // each face has 3 index because we us PostProcessSteps.Triangulate
                modelFaceIdxs.Add((uint)face.Indices[0]);
                modelFaceIdxs.Add((uint)face.Indices[1]);
                modelFaceIdxs.Add((uint)face.Indices[2]);
            }
            iModelIdxCter = modelFaceIdxs.Count;

            //  initalize the model vao, vbo and ebo with the cube attributes, vertices and faces

            vaoModel = GL.GenVertexArray();
            vboModel = GL.GenBuffer();
            eboModel = GL.GenBuffer();
            
            // set the VAO in context
            GL.BindVertexArray(vaoModel);
            
            // set in context and initalize the VBO
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboModel);
            GL.BufferData(BufferTarget.ArrayBuffer, modelCoordNColors.Count * sizeof(float), modelCoordNColors.ToArray(), BufferUsageHint.StaticDraw);

            // set in context and initalize the EBO
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboModel);
            GL.BufferData(BufferTarget.ElementArrayBuffer, modelFaceIdxs.Count * sizeof(uint), modelFaceIdxs.ToArray(), BufferUsageHint.StaticDraw);

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

            // calculate the translation values to center the object
            objTransX = -(flMaxX - flMinX)/2;
            objTransY = -(flMaxY - flMinY) / 2;
            objTransZ = -(flMaxZ - flMinZ) / 2;

        }//InitializeShellSpiralAxisGeometry

        /*******************************************************************************
        * @brief delegate that manages the event that occurs when the OpenGL control has
        * been loaded.
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void OpenTkControl_Loaded(object sender, RoutedEventArgs e) {

            CreateShaders();

            InitializeXYZAxis();
            InitializeShellSpiralAxisGeometry();

            appViewModel.bRequireToRegenerate = false;
            appViewModel.bRequireToReRender = true;

        }//OpenTkControl_Loaded

        /*******************************************************************************
        * @brief delegate that manages the event that occurs when the OpenGL changes its
        * size.
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void OpenTkControl_SizeChanged(object sender, SizeChangedEventArgs e) {
            
            // set the flag to force updating the model in the next OnRender event
            appViewModel.bRequireToReRender = true;

        }//OpenTkControl_SizeChanged

        /*******************************************************************************
        * @brief delegate that manages the event raised by the OpenGL WPF control to 
        * request to render its content.
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void OpenTkControl_OnRender(TimeSpan delta) {

            Vector3 up_vector = new Vector3();
            Vector3 light_vector = new Vector3();
            Vector3 view_vector = new Vector3();

            // only regenerate the model geometry if any geometry related parameter has changed
            if (appViewModel.bRequireToRegenerate) {

                InitializeShellSpiralAxisGeometry();
                appViewModel.bRequireToRegenerate = false;

            }//if

            // only repaint the model in case any render related parameter has changed
            if (appViewModel.bRequireToReRender) {
                
                appViewModel.bRequireToReRender = false;

                // ponemos en contexto el control GLControl sobre el que vamos a operar
                // if (!myGLControl.Context.IsCurrent) myGLControl.MakeCurrent();

                // borramos el buffer de pintado
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                // pomemos en contexto los shaders y las estructuras de la geometria
                GL.UseProgram(shaderProgram);

                // get the object transformation parameters from the controls
                // objAngle = rotateYVScrollBar.Value;
                // objScale = (float)((float)scaleVScrollBar.Value / 1000);
                // objTransX = (float)((float)tansXHScrollBar.Value / 1000);
                // objTransZ = (float)((float)tansZVScrollBar.Value / 1000);
                // 
                // // get view parameters from the controls
                // viewXAngle = (float)(viewXAngleScrollBar.Value);
                // viewYAngle = (float)(viewYAngleScrollBar.Value);
                // viewDist = (float)(viewDistScrollBar.Value);
                // 
                // // get light parameter from the controls
                // ambient = (float)ambNumUpDwn.Value;
                // difuse = (float)difuseNumUpDwn.Value;
                // shine = (int)specNumUpDwn.Value;
                // 
                // // get light parameters from the controls
                // lightXAngle = (float)(lightXAngleScrollBar.Value);
                // lightYAngle = (float)(lightYAngleScrollBar.Value);
                // lightDist = (float)(lightDistScrollBar.Value);

                // update the labels of the controls
                // rotyDescrLbl.Text = "rotY:" + objAngle.ToString("0.00");
                // scaleDescrLbl.Text = "scale:" + objScale.ToString("0.00");
                // transXDescrLbl.Text = "transX:" + objTransX.ToString("0.00");
                // transZDescrLbl.Text = "transZ:" + objTransZ.ToString("0.00");
                // viewXAngleDescrLbl.Text = "viewXº:" + viewXAngle.ToString("0.00");
                // viewYAngleDescrLbl.Text = "viewYº:" + viewYAngle.ToString("0.00");
                // viewDistDescrLbl.Text = "viewDist:" + viewDist.ToString("0.00");
                // lightXAngleDescrLbl.Text = "lightXº:" + lightXAngle.ToString("0.00");
                // lightYAngleDescrLbl.Text = "lightYº:" + lightYAngle.ToString("0.00");
                // lightDistDescrLbl.Text = "lightDist:" + lightDist.ToString("0.00");

                // convert from degrees to radians
                viewXAngle = 0; //MathHelper.DegreesToRadians(miViewModel.viewXAngle);// * (float)(2 * Math.PI / (float)360);
                viewYAngle = 0; //MathHelper.DegreesToRadians(miViewModel.viewYAngle);// * (float)(2 * Math.PI / (float)360);
                viewDist = 3;// miViewModel.viewDist;
                lightXAngle = 0;//MathHelper.DegreesToRadians(miViewModel.lightXAngle);// * (float)(2 * Math.PI / (float)360);
                lightYAngle = 0;//MathHelper.DegreesToRadians(miViewModel.lightYAngle);// * (float)(2 * Math.PI / (float)360);
                lightDist = 10;// miViewModel.lightDist;

                objAngleX = (float)appViewModel.XOrientation;
                objAngleY = (float)appViewModel.YOrientation;
                objAngleZ = (float)appViewModel.ZOrientation;
                objScale = 1;// appViewModel.


                // calculate the coordinates of the View Vector

                // invert the Up vector to avoid that the camera flips when passing 90º over the model or 270º under the model.
                if ((Math.Abs(viewYAngle) > (Math.PI / 2)) && (Math.Abs(viewYAngle) < (3 * (Math.PI / 2)))) {
                    up_vector = -1 * Vector3.UnitY;
                } else {
                    up_vector = Vector3.UnitY;
                }

                // calculate the X,Y,Z coordinates of the View Vector
                float projectView = (float)Math.Cos((float)viewYAngle) * (float)viewDist;
                viewY = (float)viewDist * (float)Math.Sin((float)viewYAngle);
                viewX = (float)Math.Cos((float)viewXAngle) * projectView;
                viewZ = (float)Math.Sin((float)viewXAngle) * projectView;
                view_vector = new Vector3(viewX, viewY, viewZ);

                // viewVectorDescrLbl.Text = "View (" + viewX.ToString("0.00") + "," + viewY.ToString("0.00") + "," + viewZ.ToString("0.00") + ")";

                // calculate the X,Y,Z coordinates of the Light Vector
                float projectLight = (float)Math.Cos((float)lightYAngle) * (float)lightDist;
                lightY = (float)lightDist * (float)Math.Sin((float)lightYAngle);
                lightX = (float)Math.Cos((float)lightXAngle) * projectLight;
                lightZ = (float)Math.Sin((float)lightXAngle) * projectLight;
                light_vector = new Vector3(lightX, lightY, lightZ);
                // lightVectorDescrLbl.Text = "Light (" + lightX.ToString("0.00") + "," + lightY.ToString("0.00") + "," + lightZ.ToString("0.00") + ")";

                Matrix4 model;
                Matrix4 view = Matrix4.LookAt(
                view_vector,
                Vector3.Zero,
                up_vector);

                Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(45f), (float)OpenTkControl.ActualWidth / (float)OpenTkControl.ActualHeight, 0.1f, 100f);

                // obtenemos los handlers de los diferentes Uniforms a partir de su nombre
                int locModel = GL.GetUniformLocation(shaderProgram, "uModel");
                int locView = GL.GetUniformLocation(shaderProgram, "uView");
                int locProj = GL.GetUniformLocation(shaderProgram, "uProjection");

                model = Matrix4.CreateScale((float)objScale) * Matrix4.CreateTranslation(objTransX, objTransY, objTransZ);
                model = model * Matrix4.CreateRotationX(MathHelper.DegreesToRadians(objAngleX)) * Matrix4.CreateRotationY(MathHelper.DegreesToRadians(objAngleY)) * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(objAngleZ));

                // inicializamos el contenido de las diferentes buffers Uniform de las matrices 
                // en los shaders con las matrices calculadas
                GL.UniformMatrix4(locModel, false, ref model);
                GL.UniformMatrix4(locView, false, ref view);
                GL.UniformMatrix4(locProj, false, ref projection);

                GL.Uniform3(GL.GetUniformLocation(shaderProgram, "lightPos"), light_vector);
                GL.Uniform3(GL.GetUniformLocation(shaderProgram, "lightColor"), Vector3.One);
                GL.Uniform3(GL.GetUniformLocation(shaderProgram, "viewPos"), view_vector);

                GL.Uniform1(GL.GetUniformLocation(shaderProgram, "ambientStrength"), 0.2);// miViewModel.ambient);
                GL.Uniform1(GL.GetUniformLocation(shaderProgram, "specularStrength"), 0.5);// miViewModel.difuse);
                GL.Uniform1(GL.GetUniformLocation(shaderProgram, "shininess"), 32);// miViewModel.shine);

                // render the axis
                Matrix4 axis;

                GL.BindVertexArray(vaoModel);

                // printamos el primer objeto en modo solido
                GL.Uniform1(GL.GetUniformLocation(shaderProgram, "isLightSource"), 0);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                GL.DrawElements(BeginMode.Triangles, iAxisIdxCter, DrawElementsType.UnsignedInt, 0);


                GL.BindVertexArray(vaoAxis);

                // printamos el segundo cubo desplazado en modo malla
                axis = Matrix4.CreateScale((float)0.25) * Matrix4.CreateTranslation(0, -1.0f, -1.0f);
                GL.UniformMatrix4(locModel, false, ref axis);

                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                GL.DrawElements(BeginMode.Triangles, iAxisIdxCter, DrawElementsType.UnsignedInt, 0);

                // MyOpenTkControl.SwapBuffers();

            }

        }//OpenTkControl_OnRender

    }

}//namespace RaupCoilerII
