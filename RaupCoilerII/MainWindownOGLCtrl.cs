using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Assimp;
using OpenTK.Core;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Wpf;

namespace RaupCoilerII {

    /*****************************************************************************
    * @note There are two main geometry objetcs that derive from Object3D class. Each
    * object has its own methods to be initialized and to be transformed and then 
    * updated in the OpenGL buffers:
    *   - Mode. geometry:
    *       - Initialize(...)
    *       - TransformAndUpdateVAO(...)
    *   - ZYX axisl geometry
    *       - Initialize(...)
    *       - TransformAndUpdateVAO(...)
    ***************************************************************************/


    /*****************************************************************************
    * @brief Partial class with the methods and properties related to the Open GL control.
    ***************************************************************************/
    public partial class MainWindow : System.Windows.Window {

        // Open GL related parameters
        private int shaderProgram;

        cZYXaxis ZYXaxis;
        cShellModel ShellModel;

        // view coordinates
        private float viewXAngle = 0f;
        private float viewYAngle = 0f;
        private float viewZAngle = 0f;
        private float viewDist = 0f;
        // view vector endpoint coordinates
        private float viewX = 0f;
        private float viewY = 0f;
        private float viewZ = 0f;

        // light coordinates
        private float lightXAngle = 0f;
        private float lightYAngle = 0f;
        private float lightZAngle = 0f;
        private float lightDist = 0f;
        // light vector endpoint coordinates
        private float lightX = 0f;
        private float lightY = 0f;
        private float lightZ = 0f;
        // light parameters
        private float ambient = 0f;
        private float difuse = 0f;
        private int shine = 0;


        /*****************************************************************************
         * @brief Initialzies some OpenGL Functionalities
         ***************************************************************************/
        private void InitializeOpenGL() {

            // initialize some OpenGL functionalites 
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.FrontFace(FrontFaceDirection.Ccw); // o Cw según tu modelo          

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        }//InitializeOpenGL

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
                        flat out vec3 Normal;
                        // out vec3 Normal;

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
                        flat in vec3 Normal;
                        // in vec3 Normal;

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
        * @brief delegate that manages the event that occurs when the OpenGL control has
        * been loaded.
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void OpenTkControl_Loaded(object sender, RoutedEventArgs e) {

            CreateShaders();

            //if (appViewModel.bRequireToRegenerate) {
            //    TransformShellSpiralAxisGeometry();
            //    appViewModel.bRequireToRegenerate = false;
            //}

            appViewModel.bRequireToTransform = true;
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
            float flAux = 0;
            Vector3 up_vector = new Vector3();
            Vector3 light_vector = new Vector3();
            Vector3 view_vector = new Vector3();

            if (appViewModel.bRequireToRegenerate) {

                ShellModel.InitializeGeometry();

                appViewModel.bRequireToRegenerate = false;
                appViewModel.bRequireToTransform = true;
                appViewModel.bRequireToReRender = true;

            }//if

            if (appViewModel.bRequireToTransform) {

                ShellModel.TransformUpdateVAO();
                
                appViewModel.bRequireToTransform = false;
                appViewModel.bRequireToReRender = true;               

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
                // view spheric coordinates
                viewXAngle = MathHelper.DegreesToRadians((float)appViewModel.ViewThetaAzimuthAngle);
                viewYAngle = MathHelper.DegreesToRadians((float)appViewModel.ViewPhiZenithAngle);
                viewZAngle = MathHelper.DegreesToRadians((float)appViewModel.ViewThetaAzimuthAngle);
                viewDist = (float)appViewModel.ViewRoDistance;
                // light spheric coordinates
                lightXAngle = MathHelper.DegreesToRadians((float)appViewModel.LightThetaAzimuthAngle);
                lightYAngle = MathHelper.DegreesToRadians((float)appViewModel.LightPhiZenithAngle);
                lightZAngle = MathHelper.DegreesToRadians((float)appViewModel.LightThetaAzimuthAngle);
                lightDist = (float)appViewModel.LightRoDistance;

                // calculate the coordinates of the View Vector

                // invert the Up vector to avoid that the camera flips when passing 90º over the model or 270º under the model.
                if ((Math.Abs(viewYAngle) > (Math.PI / 2)) && (Math.Abs(viewYAngle) < (3 * (Math.PI / 2)))) {
                    up_vector = -1 * Vector3.UnitY;
                } else {
                    up_vector = Vector3.UnitY;
                }

                // calculate the X,Y,Z endpoint of the View Vector, the other endpoint is the 0,0,0
                float projectView = (float)Math.Cos((float)viewYAngle) * (float)viewDist;
                // Endpoint X coordinate
                viewX = (float)Math.Cos((float)viewXAngle) * projectView;
                // Endpoint Y coordinate
                viewY = (float)viewDist * (float)Math.Sin((float)viewYAngle);
                // Endpoint Z coordinate
                viewZ = (float)Math.Sin(viewZAngle) * projectView;               
                view_vector = new Vector3(viewX, viewY, viewZ);

                // calculate the X,Y,Z endpoint of the Light Vector, the other endpoint is the 0,0,0
                float projectLight = (float)Math.Cos((float)lightYAngle) * (float)lightDist;
                // Endpoint X coordinate
                lightX = (float)Math.Cos((float)lightXAngle) * projectLight;
                // Endpoint Y coordinate
                lightY = (float)lightDist * (float)Math.Sin((float)lightYAngle);
                // Endpoint Z coordinate
                lightZ = (float)Math.Sin(lightZAngle) * projectLight;
                light_vector = new Vector3(lightX, lightY, lightZ);

                Matrix4 transforMatrix;
                Matrix4 viewMatrix = Matrix4.LookAt(view_vector,Vector3.Zero,up_vector);
                Matrix4 projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45f), (float)OpenTkControl.ActualWidth / (float)OpenTkControl.ActualHeight, 0.1f, 100f);

                // get the camera/view Righ Up and Forward axis in orde to use it on model rotations
                Matrix4 invView = Matrix4.Invert(viewMatrix);
                ShellModel.cameraRightVector = new Vector3(invView.M11, invView.M12, invView.M13);
                ShellModel.cameraUpVector = new Vector3(invView.M21, invView.M22, invView.M23);
                ShellModel.cameraForward = new Vector3(invView.M31, invView.M32, invView.M33);
                // normalize the vectors
                ShellModel.cameraForward.Normalize();
                ShellModel.cameraRightVector.Normalize();
                ShellModel.cameraUpVector.Normalize();
                // cameraForwared is negative becuase camera looks to -Z
                ShellModel.cameraForward = -ShellModel.cameraForward;

                // obtenemos los handlers de los diferentes Uniforms a partir de su nombre
                int locModel = GL.GetUniformLocation(shaderProgram, "uModel");
                int locView = GL.GetUniformLocation(shaderProgram, "uView");
                int locProj = GL.GetUniformLocation(shaderProgram, "uProjection");

                // transforMatrix = Matrix4.CreateScale((float)objModelScale) * Matrix4.CreateTranslation(objModelTransX, objModelTransY, objModelTransZ);
                // transforMatrix = transforMatrix * Matrix4.CreateRotationX(MathHelper.DegreesToRadians(objModelAngleX)) * Matrix4.CreateRotationY(MathHelper.DegreesToRadians(objModelAngleY)) * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(objModelAngleZ));

                // INICIO

                // OpenTK.Mathematics.Quaternion rotationQuatern = OpenTK.Mathematics.Quaternion.Identity;
                // rotationQuatern *= OpenTK.Mathematics.Quaternion.FromAxisAngle(Vector3.UnitX, MathHelper.DegreesToRadians(objModelAngleX));
                // rotationQuatern *= OpenTK.Mathematics.Quaternion.FromAxisAngle(Vector3.UnitY, MathHelper.DegreesToRadians(objModelAngleY));
                // rotationQuatern *= OpenTK.Mathematics.Quaternion.FromAxisAngle(Vector3.UnitZ, MathHelper.DegreesToRadians(objModelAngleZ));
                // convert the quaterinon to a matrix
                // Matrix4 rotation = Matrix4.CreateFromQuaternion(rotationQuatern);

                // transforMatrix = Matrix4.CreateScale((float)objModelScale);// * Matrix4.CreateTranslation(objModelTransX, objModelTransY, objModelTransZ);
                transforMatrix = Matrix4.CreateScale((float)ShellModel.objModelScale);

                // FIN

                // inicializamos el contenido de las diferentes buffers Uniform de las matrices 
                // en los shaders con las matrices calculadas
                GL.UniformMatrix4(locModel, false, ref transforMatrix);
                GL.UniformMatrix4(locView, false, ref viewMatrix);
                GL.UniformMatrix4(locProj, false, ref projectionMatrix);

                GL.Uniform3(GL.GetUniformLocation(shaderProgram, "lightPos"), light_vector);
                GL.Uniform3(GL.GetUniformLocation(shaderProgram, "lightColor"), Vector3.One);
                GL.Uniform3(GL.GetUniformLocation(shaderProgram, "viewPos"), view_vector);

                GL.Uniform1(GL.GetUniformLocation(shaderProgram, "ambientStrength"), 0.2);// miViewModel.ambient);
                GL.Uniform1(GL.GetUniformLocation(shaderProgram, "specularStrength"), 0.5);// miViewModel.difuse);
                GL.Uniform1(GL.GetUniformLocation(shaderProgram, "shininess"), 32);// miViewModel.shine);

                // ###### render the Model ######
                GL.BindVertexArray(ShellModel.objectVAO);
                GL.LineWidth(3.0f);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);// PolygonMode.Line);// PolygonMode.Fill);
                GL.DrawElements(BeginMode.Triangles, ShellModel.iIndicesFacesCter, DrawElementsType.UnsignedInt, 0);
                
                // clear the context
                GL.BindVertexArray(0);

                // render the XYZ Axis if enabled
                if (appViewModel.Axis3D) {

                    // ###### render the XYZ Axis ######
                    GL.BindVertexArray(ZYXaxis.objectVAO);
                    transforMatrix = Matrix4.CreateScale((float)ZYXaxis.objModelScale) * Matrix4.CreateTranslation(ZYXaxis.objModelTransX, ZYXaxis.objModelTransY, ZYXaxis.objModelTransZ);
                    // transforMatrix = transforMatrix * Matrix4.CreateRotationX(MathHelper.DegreesToRadians(objAngleX)) * Matrix4.CreateRotationY(MathHelper.DegreesToRadians(objAngleY)) * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(objAngleZ));
                    // transforMatrix = transforMatrix * Matrix4.CreateRotationX(MathHelper.DegreesToRadians(objXYZAxisAngleX)) * Matrix4.CreateRotationY(MathHelper.DegreesToRadians(objXYZAxisAngleY)) * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(objXYZAxisAngleZ));
                    GL.UniformMatrix4(locModel, false, ref transforMatrix);
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                    GL.DrawElements(BeginMode.Triangles, ZYXaxis.iIndicesFacesCter, DrawElementsType.UnsignedInt, 0);
                    // clear the context
                    GL.BindVertexArray(0);

                }//if (appViewModel.Axis3D)

            }// if (appViewModel.bRequireToReRender)

        }//OpenTkControl_OnRender

    }//public partial class MainWindow : Window

}//namespace RaupCoilerII
