using Crux.Utilities.Helpers;
using OpenTK.Graphics.OpenGL4;

namespace Crux.Graphics;

public class MeshBuffer
{
    //int vao; configuration for vertex data, offsets, and indices
    //int vbo; unique vertex data (pos, normal, uv)
    //int ebo; indices to connect the vertices into triangles

    public int VAO = -1;
    public int StaticVBO = -1;

    public int DynamicVBO = -1;
    public int DynamicVBOBufferLength = 0;
    public int DynamicVBOTypesByteSize = 0;

    public bool DrawnThisFrame = false;

    public int EBO = -1;

    private int attributeIndex = 0;

    public MeshBuffer()
    {
        VAO = GL.GenVertexArray();
    }

    public void SetDynamicVBOData(float[] flatpack, int instances)
    {
        if (instances > DynamicVBOBufferLength)
        {
            DynamicVBOBufferLength = Math.Max(64, DynamicVBOBufferLength * 2);

            GL.BindBuffer(BufferTarget.ArrayBuffer, DynamicVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, DynamicVBOBufferLength * DynamicVBOTypesByteSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        GL.BindBuffer(BufferTarget.ArrayBuffer, DynamicVBO);
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, instances * DynamicVBOTypesByteSize, flatpack);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
    }

    public void Draw(int vertices)
    {
        GL.BindVertexArray(VAO);
        GL.DrawElements(PrimitiveType.Triangles, vertices, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);

        GraphicsCache.DrawCallsThisFrame++;
        GraphicsCache.TrianglesThisFrame += (vertices / 3);
    }

    public void DrawInstanced(int vertices, int instances)
    {
        GL.BindVertexArray(VAO);
        GL.DrawElementsInstanced(PrimitiveType.Triangles, vertices, DrawElementsType.UnsignedInt, IntPtr.Zero, instances);
        GL.BindVertexArray(0);

        GraphicsCache.DrawCallsThisFrame++;
        GraphicsCache.TrianglesThisFrame += (vertices / 3) * instances;
    }

    public void DrawInstancedWithoutIndices(int vertices, int instances)
    {
        GL.BindVertexArray(VAO);
        GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, vertices, instances);
        GL.BindVertexArray(0);

        GraphicsCache.DrawCallsThisFrame++;
        GraphicsCache.TrianglesThisFrame += (vertices / 3) * instances;
    }

    public void GenEBO(uint[] indices)
    {
        GL.BindVertexArray(VAO);

        EBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

        // No need to unbind the ElementArrayBuffer here. Keep it bound for later rendering.
        //GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        GL.BindVertexArray(0);
    }

    public void GenStaticVBO(VertexAttribute[] attributes)
    {
        GL.BindVertexArray(VAO);

        StaticVBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, StaticVBO);

        int vertexCount = attributes[0].Data.Length / attributes[0].TypeSize;
        int stride = attributes.Sum(attribute => attribute.TypeSize);
        List<float> interleavedList = new List<float>(vertexCount * stride);

        for (int i = 0; i < vertexCount; i++)
        {
            foreach (VertexAttribute attribute in attributes)
            {
                for (int j = 0; j < attribute.TypeSize; j++)
                {
                    interleavedList.Add(attribute.Data[i * attribute.TypeSize + j]);
                }
            }
        }

        float[] interleavedData = interleavedList.ToArray();
        GL.BufferData(BufferTarget.ArrayBuffer, interleavedData.Length * sizeof(float), interleavedData, BufferUsageHint.StaticDraw);

        int byteOffset = 0;
        for (int i = 0; i < attributes.Length; i++)
        {
            GL.VertexAttribPointer(i, attributes[i].TypeSize, VertexAttribPointerType.Float, false, stride * sizeof(float), byteOffset);
            GL.EnableVertexAttribArray(i);
            byteOffset += attributes[i].TypeSize * sizeof(float);
            attributeIndex++;
        }

        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
    }

    public void GenDynamicVBO(Type[] attributes)
    {
        foreach (Type T in attributes)
            DynamicVBOTypesByteSize += VertexAttributeHelper.GetTypeByteSize(T);

        //Bind VAO    
        GL.BindVertexArray(VAO);

        //Generate Dynamic VBO (Matrices, Offsets)
        DynamicVBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, DynamicVBO); //bind
        GL.BufferData(BufferTarget.ArrayBuffer, 0, IntPtr.Zero, BufferUsageHint.DynamicDraw); //buffer empty

        int totalByteSize = 0;
        foreach (Type attributeType in attributes)
            totalByteSize += VertexAttributeHelper.GetTypeByteSize(attributeType);

        int byteOffset = 0;
        int byteSize = 0;
        foreach (Type attributeType in attributes)
        {
            if (attributeType == typeof(float))
            {
                GL.VertexAttribPointer(attributeIndex, VertexAttributeHelper.GetTypeWidth(typeof(float)), VertexAttribPointerType.Float, false, totalByteSize, byteOffset);
                GL.EnableVertexAttribArray(attributeIndex);
                GL.VertexAttribDivisor(attributeIndex, 1);
                byteOffset += VertexAttributeHelper.GetTypeByteSize(typeof(float));
                attributeIndex++;
            }
            else if (attributeType == typeof(Vector2))
            {
                GL.VertexAttribPointer(attributeIndex, VertexAttributeHelper.GetTypeWidth(typeof(Vector2)), VertexAttribPointerType.Float, false, totalByteSize, byteOffset);
                GL.EnableVertexAttribArray(attributeIndex);
                GL.VertexAttribDivisor(attributeIndex, 1);
                byteOffset += VertexAttributeHelper.GetTypeByteSize(typeof(Vector2));
                attributeIndex++;
            }
            else if (attributeType == typeof(Vector3))
            {
                GL.VertexAttribPointer(attributeIndex, VertexAttributeHelper.GetTypeWidth(typeof(Vector3)), VertexAttribPointerType.Float, false, totalByteSize, byteOffset);
                GL.EnableVertexAttribArray(attributeIndex);
                GL.VertexAttribDivisor(attributeIndex, 1);
                byteOffset += VertexAttributeHelper.GetTypeByteSize(typeof(Vector3));
                attributeIndex++;
            }
            else if (attributeType == typeof(Vector4))
            {
                GL.VertexAttribPointer(attributeIndex, VertexAttributeHelper.GetTypeWidth(typeof(Vector4)), VertexAttribPointerType.Float, false, totalByteSize, byteOffset);
                GL.EnableVertexAttribArray(attributeIndex);
                GL.VertexAttribDivisor(attributeIndex, 1);
                byteOffset += VertexAttributeHelper.GetTypeByteSize(typeof(Vector4));
                attributeIndex++;
            }
            else if (attributeType == typeof(Matrix4))
            {
                for (int i = 0; i < 4; i++) // Each row of Matrix4 is a vec4
                {
                    GL.VertexAttribPointer(
                        attributeIndex,                                             //Index
                        VertexAttributeHelper.GetTypeWidth(typeof(Vector4)),        //Width
                        VertexAttribPointerType.Float,                              //Type                
                        false,                                                      //Normalized
                        totalByteSize,                                              //Stride
                        i * VertexAttributeHelper.GetTypeByteSize(typeof(Vector4))  //Offset
                        );

                    GL.EnableVertexAttribArray(attributeIndex);
                    GL.VertexAttribDivisor(attributeIndex, 1);
                    byteOffset += VertexAttributeHelper.GetTypeByteSize(typeof(Vector4));
                    attributeIndex++;
                }
            }
        }

        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
    }
}
