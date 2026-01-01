using OpenTK.Graphics.OpenGL4;
using CruxEngine.Graphics;
using CruxEngine.Graphics.Shaders;
using CruxEngine.Utilities.IO;
using CruxEngine.Components;
using CruxEngine.Scripting;

namespace CruxEngine.Core;

public abstract class Scene
{
    public SkyboxShader Skybox { get; private set;}
    public SceneLighting Lighting { get; private set;}

    private readonly MeshBuffer skyboxBuffer;

    public event Action? SceneUpdateEvent;
    public List<GameObject> Instantiated = new List<GameObject>();

    //Not implemented
    public Sandbox ScriptingSandbox = new Sandbox();

    protected virtual string DefaultSkyboxPath { get; } = JsonPresetLoader.TemplateSkyboxJsonPresetPath;
    protected virtual string DefaultSceneLightingPath { get; } = JsonPresetLoader.TemplateSceneLightingJsonPresetPath;

    public Scene()
    {
        Skybox = JsonPresetLoader.LoadSkybox(DefaultSkyboxPath);
        Lighting = JsonPresetLoader.LoadSceneLighting(DefaultSceneLightingPath);

        skyboxBuffer = GraphicsCache.GetInstancedQuadBuffer("Skybox");
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

    public void SetSkybox(SkyboxShader Selected)
    {
        Skybox = Selected;
    }

    public void SetLighting(SceneLighting Selected)
    {
        Lighting = Selected;
        Lighting.Apply();
    }
}
