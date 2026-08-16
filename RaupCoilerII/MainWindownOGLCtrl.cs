using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Assimp;
using OpenTK.Core;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Wpf;
using static RaupCoilerII.ViewModel;

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

        cGizmo ZYXaxis;
        cShellModel ShellModel;
        unitCube UnitContainerCube;
        cSpiral Spiral;

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

        // OpenGL buffer size ( size of the area used to draw the model )
        private int OGLBufHeigh = 0;
        private int OGLBufWidth = 0;

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
                        layout (location = 1) in vec4 aColor;
                        layout (location = 2) in vec3 aNormal;

                        uniform mat4 uModel;
                        uniform mat4 uView;
                        uniform mat4 uProjection;

                        out vec3 FragPos;
                        out vec4 Color;
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
                        in vec4 Color;
                        flat in vec3 Normal;
                        // in vec3 Normal;

                        out vec4 FragColor;

                        uniform vec3 lightPos;
                        uniform vec3 viewPos;                        
                        uniform vec3 lightColor;
                        
                        // Light intensities
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
                                vec3 result = ( ambient + diffuse + specular ) * Color.rgb;
                                FragColor = vec4(result, Color.a);

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
            
            // set the flag to force updating the model in the next OnRender event
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
            float flGizmoViewDist = 4.0f;
            float flGizmoLightDist = 100.0f;
            float flGizmoScale = 1.0f;

            if (appViewModel.bRequireToRegenerate) {

                appViewModel.bRequireToRegenerate = false;
                appViewModel.bRequireToTransform = true;
                appViewModel.bRequireToReRender = true;

                // regenerate the shell model geometry
                ShellModel.InitializeGeometry();

            }//if

            if (appViewModel.bRequireToTransform) {

                appViewModel.bRequireToTransform = false;
                appViewModel.bRequireToReRender = true;

                // apply the current transformations to the original shell geometry
                ShellModel.TransformUpdateVAO();

            }//if

            // only repaint the model in case the flag has been set
            if (appViewModel.bRequireToReRender) {
                
                appViewModel.bRequireToReRender = false;

                // pomemos en contexto los shaders y las estructuras de la geometria
                GL.UseProgram(shaderProgram);

                // clear the buffer
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                // adjust to the same size than the WpfOGL control, the size of the OGL Viewport
                // area at which the shell model will be drawn 
                int[] viewport = new int[4];
                GL.GetInteger(GetPName.Viewport, viewport);
                int OGLBufWidth = viewport[2];
                int OGLBufHeight = viewport[3];
                GL.Viewport(0, 0, OGLBufWidth, OGLBufHeight);

                // enable the "DepthTest" when redendering the model in order the faces
                // are shown or not according to its depth and the other faces position
                GL.Enable(EnableCap.DepthTest);

                // convert view vector spheric coordinates (Theta, Phi, Ro) to cartesian coordinates (X,Y,Z)
                // convert angles from degrees to radians                   
                viewXAngle = MathHelper.DegreesToRadians((float)appViewModel.ViewThetaAzimuthAngle);
                viewYAngle = MathHelper.DegreesToRadians((float)appViewModel.ViewPhiZenithAngle);
                viewZAngle = MathHelper.DegreesToRadians((float)appViewModel.ViewThetaAzimuthAngle);
                viewDist = (float)appViewModel.ViewRoDistance;
                // calculate the X,Y,Z endpoint of the View Vector, the other endpoint is the 0,0,0
                float projectedViewVector = (float)Math.Cos((float)viewYAngle) * (float)viewDist;
                // endpoint X coordinate
                viewX = (float)Math.Cos((float)viewXAngle) * projectedViewVector;
                // endpoint Y coordinate
                viewY = (float)viewDist * (float)Math.Sin((float)viewYAngle);
                // endpoint Z coordinate
                viewZ = (float)Math.Sin(viewZAngle) * projectedViewVector;               
                // now get the vector
                view_vector = new Vector3(viewX, viewY, viewZ);

                // the view matrix has a direction vector but also needs an 'up vector' that specifies where
                // is "up" and where is "down" of the view. 
                // The Up vector must be inverted to avoid that the camera flips when passing 90º over the model or 270º under the model.
                if ((Math.Abs(viewYAngle) > (Math.PI / 2)) && (Math.Abs(viewYAngle) < (3 * (Math.PI / 2)))) {                    
                    up_vector = -1 * Vector3.UnitY;
                } else {
                    up_vector = Vector3.UnitY;
                }

                // convert light vector spheric coordinates (Theta, Phi, Ro) to cartesian coordinates (X,Y,Z)
                // convert angles from degrees to radians      
                lightXAngle = MathHelper.DegreesToRadians((float)appViewModel.LightThetaAzimuthAngle);
                lightYAngle = MathHelper.DegreesToRadians((float)appViewModel.LightPhiZenithAngle);
                lightZAngle = MathHelper.DegreesToRadians((float)appViewModel.LightThetaAzimuthAngle);
                lightDist = (float)appViewModel.LightRoDistance;
                // calculate the X,Y,Z endpoint of the Light Vector, the other endpoint is the 0,0,0
                float projectLight = (float)Math.Cos((float)lightYAngle) * (float)lightDist;
                // endpoint X coordinate
                lightX = (float)Math.Cos((float)lightXAngle) * projectLight;
                // Endpoint Y coordinate
                lightY = (float)lightDist * (float)Math.Sin((float)lightYAngle);
                // endpoint Z coordinate
                lightZ = (float)Math.Sin(lightZAngle) * projectLight;
                // now get the vector
                light_vector = new Vector3(lightX, lightY, lightZ);
                 

                Matrix4 transforMatrix = Matrix4.Identity; // Matrix4.CreateScale((float)ShellModel.objModelScale);// * Matrix4.CreateTranslation(ShellModel.objModelTransX* ShellModel.objModelScale, ShellModel.objModelTransY*ShellModel.objModelScale, ShellModel.objModelTransZ* ShellModel.objModelScale);
                Matrix4 viewMatrix = Matrix4.LookAt(view_vector,Vector3.Zero,up_vector);
                Matrix4 projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45f), (float)OpenTkControl.ActualWidth / (float)OpenTkControl.ActualHeight, 0.1f, 100f);
                
                // initialize the content of the different graphic card Uniform buffers used by
                // the vertex shader and the fragment shader:                
                // data for the vertex shader:
                GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram, "uModel"), false, ref transforMatrix);
                GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram, "uView"), false, ref viewMatrix);
                GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram, "uProjection"), false, ref projectionMatrix);                
                // vectors data for the fragment shader:
                GL.Uniform3(GL.GetUniformLocation(shaderProgram, "lightPos"), light_vector);
                GL.Uniform3(GL.GetUniformLocation(shaderProgram, "viewPos"), view_vector);
                GL.Uniform3(GL.GetUniformLocation(shaderProgram, "lightColor"), Vector3.One);
                // light parameters for the fragment shader:
                GL.Uniform1(GL.GetUniformLocation(shaderProgram, "ambientStrength"), 0.2);// miViewModel.ambient);
                GL.Uniform1(GL.GetUniformLocation(shaderProgram, "specularStrength"), 0.5);// miViewModel.difuse);
                GL.Uniform1(GL.GetUniformLocation(shaderProgram, "shininess"), 32);// miViewModel.shine);

                // ###### render the Model ######
                GL.BindVertexArray(ShellModel.objectVAO);

                // generate the geoemtry with the selecthed method
                switch (appViewModel.geometryStyle) {

                    case (tGeometryStyleEnum.Vertexs):
                        GL.PointSize(5.0f);
                        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Point);
                        GL.DrawElements(BeginMode.Triangles, ShellModel.iIndicesFacesCter, DrawElementsType.UnsignedInt, 0);
                        break;
                    
                    case (tGeometryStyleEnum.Triangles):
                        GL.LineWidth(3.0f);
                        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                        GL.DrawElements(BeginMode.Triangles, ShellModel.iIndicesFacesCter, DrawElementsType.UnsignedInt, 0);
                        break;

                    case (tGeometryStyleEnum.Faces):
                        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                        GL.DrawElements(BeginMode.Triangles, ShellModel.iIndicesFacesCter, DrawElementsType.UnsignedInt, 0);
                        break;

                    case (tGeometryStyleEnum.NoMesh):
                        break;

                }//switch                                                             

                // render the shell model spiral
                if (appViewModel.Spiral) {

                    // clear the context
                    GL.BindVertexArray(0);

                    // render the shell model spiral
                    GL.BindVertexArray(Spiral.objectVAO);
                    transforMatrix = Matrix4.CreateScale((float)UnitContainerCube.objModelScale) * Matrix4.CreateTranslation(UnitContainerCube.objModelTransX, UnitContainerCube.objModelTransY, UnitContainerCube.objModelTransZ);
                    // transforMatrix = transforMatrix * Matrix4.CreateRotationX(MathHelper.DegreesToRadians(objAngleX)) * Matrix4.CreateRotationY(MathHelper.DegreesToRadians(objAngleY)) * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(objAngleZ));
                    // transforMatrix = transforMatrix * Matrix4.CreateRotationX(MathHelper.DegreesToRadians(objXYZAxisAngleX)) * Matrix4.CreateRotationY(MathHelper.DegreesToRadians(objXYZAxisAngleY)) * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(objXYZAxisAngleZ));
                    GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram, "uModel"), false, ref transforMatrix);
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                    GL.DrawElements(BeginMode.Triangles, UnitContainerCube.iIndicesFacesCter, DrawElementsType.UnsignedInt, 0);
                    // clear the context
                    GL.BindVertexArray(0);

                }

                // render the unit cube that contains the shell model when autoscale mode is enabled
                if (appViewModel.UnitCube) {

                    // clear the context
                    GL.BindVertexArray(0);

                    // render the -1,1,-1,1 containing cube
                    GL.BindVertexArray(UnitContainerCube.objectVAO);
                    transforMatrix = Matrix4.CreateScale((float)UnitContainerCube.objModelScale) * Matrix4.CreateTranslation(UnitContainerCube.objModelTransX, UnitContainerCube.objModelTransY, UnitContainerCube.objModelTransZ);
                    // transforMatrix = transforMatrix * Matrix4.CreateRotationX(MathHelper.DegreesToRadians(objAngleX)) * Matrix4.CreateRotationY(MathHelper.DegreesToRadians(objAngleY)) * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(objAngleZ));
                    // transforMatrix = transforMatrix * Matrix4.CreateRotationX(MathHelper.DegreesToRadians(objXYZAxisAngleX)) * Matrix4.CreateRotationY(MathHelper.DegreesToRadians(objXYZAxisAngleY)) * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(objXYZAxisAngleZ));
                    GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram, "uModel"), false, ref transforMatrix);
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                    GL.DrawElements(BeginMode.Triangles, UnitContainerCube.iIndicesFacesCter, DrawElementsType.UnsignedInt, 0);
                    // clear the context
                    GL.BindVertexArray(0);

                }// if (appViewModel.UnitCube) {

                // render the XYZ Axis ( GIZMO ) if enabled. It is drawn in an independent ViewPort
                if (appViewModel.Axis3D) {

                    // set the Viewport area at the bottom left in the OpenGL buffer at which the gizmo will be rendered
                    GL.Viewport(0, 0, (int)(OGLBufWidth* 0.25), (int)(OGLBufHeight * 0.25));

                    // disable the "DepthTest" when redendering the gizmo in order it is always
                    // shown in front of the 3D model and no depth is taken into consideration
                    GL.Disable(EnableCap.DepthTest);

                    // the view of the Gizmo has the same orientation than the view of the model, but the
                    // distance from the viewer to the Gizmo is constant to grant that it is not affaceted 
                    // with the model zoon in zoom out and it is always shown with the same scale.

                    // convert view vector spheric coordinates (Theta, Phi, Ro) to cartesian coordinates (X,Y,Z)
                    // convert angles from degrees to radians                   
                    viewXAngle = MathHelper.DegreesToRadians((float)appViewModel.ViewThetaAzimuthAngle);
                    viewYAngle = MathHelper.DegreesToRadians((float)appViewModel.ViewPhiZenithAngle);
                    viewZAngle = MathHelper.DegreesToRadians((float)appViewModel.ViewThetaAzimuthAngle);
                    viewDist = (float)flGizmoViewDist;
                    // calculate the X,Y,Z endpoint of the View Vector, the other endpoint is the 0,0,0
                    projectedViewVector = (float)Math.Cos((float)viewYAngle) * (float)viewDist;
                    // Endpoint X coordinate
                    viewX = (float)Math.Cos((float)viewXAngle) * projectedViewVector;
                    // Endpoint Y coordinate
                    viewY = (float)viewDist * (float)Math.Sin((float)viewYAngle);
                    // Endpoint Z coordinate
                    viewZ = (float)Math.Sin(viewZAngle) * projectedViewVector;
                    view_vector = new Vector3(viewX, viewY, viewZ);

                    // the view matrix has a direction vector but also an up vector that sets where is Up and where is Down.
                    // invert the Up vector to avoid that the camera flips when passing 90º over the model or 270º under the model.
                    if ((Math.Abs(viewYAngle) > (Math.PI / 2)) && (Math.Abs(viewYAngle) < (3 * (Math.PI / 2)))) {
                        up_vector = -1 * Vector3.UnitY;
                    } else {
                        up_vector = Vector3.UnitY;
                    }

                    // the light of the Gizmo has the same orientation than the light of the model, but the
                    // distance from the `light to the Gizmo is kept constant to grant that it is not affaceted 
                    // with the model zoon in zoom out and it is always shown with the same light.

                    // convert light vector spheric coordinates (Theta, Phi, Ro) to cartesian coordinates (X,Y,Z)
                    // convert angles from degrees to radians      
                    // lightXAngle = MathHelper.DegreesToRadians((float)appViewModel.LightThetaAzimuthAngle);
                    // lightYAngle = MathHelper.DegreesToRadians((float)appViewModel.LightPhiZenithAngle);
                    // lightZAngle = MathHelper.DegreesToRadians((float)appViewModel.LightThetaAzimuthAngle);
                    lightDist = (float)flGizmoLightDist;
                    // calculate the X,Y,Z endpoint of the Light Vector, the other endpoint is the 0,0,0
                    projectLight = (float)Math.Cos((float)lightYAngle) * (float)lightDist;
                    // Endpoint X coordinate
                    lightX = (float)Math.Cos((float)lightXAngle) * projectLight;
                    // Endpoint Y coordinate
                    lightY = (float)lightDist * (float)Math.Sin((float)lightYAngle);
                    // Endpoint Z coordinate
                    lightZ = (float)Math.Sin(lightZAngle) * projectLight;
                    light_vector = new Vector3(lightX, lightY, lightZ);

                    transforMatrix = Matrix4.CreateScale((float)flGizmoScale);// * Matrix4.CreateTranslation(ShellModel.objModelTransX* ShellModel.objModelScale, ShellModel.objModelTransY*ShellModel.objModelScale, ShellModel.objModelTransZ* ShellModel.objModelScale);
                    viewMatrix = Matrix4.LookAt(view_vector, Vector3.Zero, up_vector);
                    // projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45f), (float)OpenTkControl.ActualWidth / (float)OpenTkControl.ActualHeight, 0.1f, 100f);

                    // initialize the content of the different graphic card Uniform buffers used by
                    // the vertex shader and the fragment shader:
                    // data for the vertex shader:
                    GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram, "uModel"), false, ref transforMatrix);
                    GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram, "uView"), false, ref viewMatrix);
                    // GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram, "uProjection"), false, ref projectionMatrix);
                    // vectors data for the fragment shader:
                    GL.Uniform3(GL.GetUniformLocation(shaderProgram, "lightPos"), light_vector);
                    // GL.Uniform3(GL.GetUniformLocation(shaderProgram, "viewPos"), view_vector);
                    // GL.Uniform3(GL.GetUniformLocation(shaderProgram, "lightColor"), Vector3.One);
                    // light parameters for the fragment shader:
                    // GL.Uniform1(GL.GetUniformLocation(shaderProgram, "ambientStrength"), 0.2);// miViewModel.ambient);
                    // GL.Uniform1(GL.GetUniformLocation(shaderProgram, "specularStrength"), 0.5);// miViewModel.difuse);
                    // GL.Uniform1(GL.GetUniformLocation(shaderProgram, "shininess"), 32);// miViewModel.shine);

                    // clear the context
                    GL.BindVertexArray(0);

                    // ###### render the XYZ Axis ######
                    GL.BindVertexArray(ZYXaxis.objectVAO);
                    // transforMatrix = Matrix4.CreateScale((float)ZYXaxis.objModelScale) * Matrix4.CreateTranslation(ZYXaxis.objModelTransX, ZYXaxis.objModelTransY, ZYXaxis.objModelTransZ);
                    transforMatrix = Matrix4.CreateScale((float)flGizmoScale) * Matrix4.CreateTranslation(0, 0, 0);
                    // transforMatrix = transforMatrix * Matrix4.CreateRotationX(MathHelper.DegreesToRadians(objAngleX)) * Matrix4.CreateRotationY(MathHelper.DegreesToRadians(objAngleY)) * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(objAngleZ));
                    // transforMatrix = transforMatrix * Matrix4.CreateRotationX(MathHelper.DegreesToRadians(objXYZAxisAngleX)) * Matrix4.CreateRotationY(MathHelper.DegreesToRadians(objXYZAxisAngleY)) * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(objXYZAxisAngleZ));
                    GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram, "uModel"), false, ref transforMatrix);

                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                    GL.DrawElements(BeginMode.Triangles, ZYXaxis.iIndicesFacesCter, DrawElementsType.UnsignedInt, 0);
                    // clear the context
                    GL.BindVertexArray(0);
                    
                }//if (appViewModel.Axis3D)

            }// if (appViewModel.bRequireToReRender)

        }//OpenTkControl_OnRender

    }//public partial class MainWindow : Window

}//namespace RaupCoilerII
