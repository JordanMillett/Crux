# Shader Presets

```mermaid
graph LR
	vert_3d --> Lit_3D
    frag_3d_lit --> Lit_3D

	vert_3d --> Unlit_3D
    frag_3d_unlit --> Unlit_3D

	vert_2d --> Unlit_2D
	frag_2d_unlit --> Unlit_2D

	vert_2d_font --> Unlit_2D_Font
    frag_2d_unlit_font --> Unlit_2D_Font

    vert_2d --> Unlit_2D_Skybox
    frag_2d_unlit_skybox --> Unlit_2D_Skybox
```