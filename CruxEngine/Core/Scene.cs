using OpenTK.Graphics.OpenGL4;
using CruxEngine.Graphics;
using CruxEngine.Graphics.Shaders;
using CruxEngine.Utilities.IO;
using CruxEngine.Components;
using CruxEngine.Scripting;

namespace CruxEngine.Core;

public abstract class Scene
{
    public SkyboxShader Skybox;

    public SceneLighting Lighting;

    private readonly MeshBuffer skyboxBuffer;

    public event Action? SceneUpdateEvent;
    public List<GameObject> Instantiated = new List<GameObject>();

    //Not implemented
    public Sandbox ScriptingSandbox = new Sandbox();
    
    public Scene(string skyboxPath = "CruxEngine/Assets/Templates/Skybox.json")
    {


        //string materialPath = "CruxEngine/Assets/Materials/Skybox.json";
        
        //Skybox = (SkyboxShader) Presets.LoadPresetShader(Presets.ShaderPresets.Unlit_2D_Skybox, false);

        Skybox = JsonAssetLoader.LoadSkybox(skyboxPath);

        skyboxBuffer = GraphicsCache.GetInstancedQuadBuffer("Skybox");
        
        Lighting = new SceneLighting();
    }

    public void Start()
    {
        OnStart();
    }

    protected virtual void OnStart() {}

    public void Update()
    {
        OnUpdate(); //Update Scene Logic
        SceneUpdateEvent?.Invoke(); //Update GameObject Logic
    }

    protected virtual void OnUpdate() {}
    
    public void RenderSkybox()
    {
        GL.DepthMask(false);

        Skybox.Bind();
        
        GL.BindVertexArray(skyboxBuffer.VAO);
        
        //MOVE OUT OF HERE TO GRAPHICS CACHE I THINK
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        GraphicsCache.DrawCallsThisFrame++;

        GL.BindVertexArray(0);

        Skybox.Unbind();
        
        GL.DepthMask(true); 
    }
}
