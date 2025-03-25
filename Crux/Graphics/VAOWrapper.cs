using Crux.Utilities.Helpers;
using OpenTK.Graphics.OpenGL4;

namespace Crux.Graphics;

public class VAOWrapper
{
    //int vao; configuration for vertex data, offsets, and indices
    //int vbo; unique vertex data (pos, normal, uv)
    //int ebo; indices to connect the vertices into triangles

    public int VAO = -1;
    public int StaticVBO = -1;
    public int DynamicVBO = -1;

    public int EBO = -1;

    private int vertexAttributeIndex = 0;

    public VAOWrapper()
    {
        VAO = GL.GenVertexArray();
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

        for (int i = 0; i < attributes.Length; i++)
        {
            GL.VertexAttribPointer(i, attributes[i].TypeSize, VertexAttribPointerType.Float, false, stride * sizeof(float), vertexAttributeIndex * sizeof(float));
            GL.EnableVertexAttribArray(i);

            vertexAttributeIndex += attributes[i].TypeSize;
        }

        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
    }

    public void GenDynamicVBO(Type[] attributes)
    {
        GL.BindVertexArray(VAO);

        DynamicVBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, DynamicVBO); //bind
        GL.BufferData(BufferTarget.ArrayBuffer, 0, IntPtr.Zero, BufferUsageHint.DynamicDraw); //buffer empty

        int stride = 0;
        foreach (Type attributeType in attributes)
            stride += VertexAttributeHelper.StrideLookup(attributeType);

        int currentOffset = 0;
        foreach (Type attributeType in attributes)
        {
            if (attributeType == typeof(float))
            {
                GL.VertexAttribPointer(vertexAttributeIndex, 1, VertexAttribPointerType.Float, false, stride, (IntPtr)(currentOffset));
                GL.EnableVertexAttribArray(vertexAttributeIndex);
                vertexAttributeIndex++;
                currentOffset += VertexAttributeHelper.StrideLookup(attributeType);
            }
            else if (attributeType == typeof(Vector2))
            {
                GL.VertexAttribPointer(vertexAttributeIndex, 2, VertexAttribPointerType.Float, false, stride, (IntPtr)(currentOffset));
                GL.EnableVertexAttribArray(vertexAttributeIndex);
                vertexAttributeIndex++;
                currentOffset += VertexAttributeHelper.StrideLookup(attributeType);
            }
            else if (attributeType == typeof(Vector3))
            {
                GL.VertexAttribPointer(vertexAttributeIndex, 3, VertexAttribPointerType.Float, false, stride, (IntPtr)(currentOffset));
                GL.EnableVertexAttribArray(vertexAttributeIndex);
                vertexAttributeIndex++;
                currentOffset += VertexAttributeHelper.StrideLookup(attributeType);
            }
            else if (attributeType == typeof(Vector4))
            {
                GL.VertexAttribPointer(vertexAttributeIndex, 4, VertexAttribPointerType.Float, false, stride, (IntPtr)(currentOffset));
                GL.EnableVertexAttribArray(vertexAttributeIndex);
                vertexAttributeIndex++;
                currentOffset += VertexAttributeHelper.StrideLookup(attributeType);
            }
            else if (attributeType == typeof(Matrix4))
            {
                for (int i = 0; i < 4; i++)
                {
                    GL.VertexAttribPointer(vertexAttributeIndex + i, 4, VertexAttribPointerType.Float, false, stride, (IntPtr)(currentOffset + i * 4 * sizeof(float)));
                    GL.EnableVertexAttribArray(vertexAttributeIndex + i);
                    GL.VertexAttribDivisor(vertexAttributeIndex + i, 1);
                }
                vertexAttributeIndex += 4;
                currentOffset += VertexAttributeHelper.StrideLookup(attributeType);
            }
        }

        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
    }

}
