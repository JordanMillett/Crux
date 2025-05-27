namespace Crux.Utilities.Helpers;

public static class MatrixHelper
{        
    public static float[] Matrix4ToArray(Matrix4 matrix)
    {
        return new float[]
        {
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44
        };
    }

    public static void Matrix4ToArray(Matrix4 matrix, out float[] values)
    {
        values = Matrix4ToArray(matrix);
    }

    public static void Decompose(Matrix4 matrix, out Vector3 scale, out Quaternion rotation, out Vector3 translation)
    {
        // Start by extracting the translation
        translation = matrix.Row3.Xyz;

        // Extract the scale
        Vector3 row0 = matrix.Row0.Xyz;
        Vector3 row1 = matrix.Row1.Xyz;
        Vector3 row2 = matrix.Row2.Xyz;

        scale = new Vector3(
            row0.Length,
            row1.Length,
            row2.Length
        );

        // Normalize the rows to remove scaling
        row0 = row0.Normalized();
        row1 = row1.Normalized();
        row2 = row2.Normalized();

        // Construct the rotation matrix without scaling
        Matrix3 rotationMatrix = new Matrix3(row0, row1, row2);

        // Convert the rotation matrix to a quaternion
        rotation = Quaternion.FromMatrix(rotationMatrix);
    }

    public static Matrix4 CreateRotationMatrixFromAxes(Vector3[] axes)
    {
        return new Matrix4(
            axes[0].X, axes[1].X, axes[2].X, 0.0f, 
            axes[0].Y, axes[1].Y, axes[2].Y, 0.0f,
            axes[0].Z, axes[1].Z, axes[2].Z, 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f                
        );
    }
    
    public static Matrix3 ExtractRotationScale(Matrix4 matrix)
    {
        return new Matrix3(
            matrix.M11, matrix.M12, matrix.M13,
            matrix.M21, matrix.M22, matrix.M23,
            matrix.M31, matrix.M32, matrix.M33
        );
    }

    public static Matrix3 ExtractRotation(Matrix4 matrix)
    {
        float scaleX = new Vector3(matrix.M11, matrix.M12, matrix.M13).Length;
        float scaleY = new Vector3(matrix.M21, matrix.M22, matrix.M23).Length;
        float scaleZ = new Vector3(matrix.M31, matrix.M32, matrix.M33).Length;

        Matrix3 rotationMatrix = new Matrix3(
            matrix.M11 / scaleX, matrix.M12 / scaleY, matrix.M13 / scaleZ,
            matrix.M21 / scaleX, matrix.M22 / scaleY, matrix.M23 / scaleZ,
            matrix.M31 / scaleX, matrix.M32 / scaleY, matrix.M33 / scaleZ
        );

        return rotationMatrix;
    }

    public static Matrix4 ExtractRotationAsMatrix4(Matrix4 matrix)
    {
        // Extract the scale factors from the matrix by calculating the lengths of the columns
        float scaleX = new Vector3(matrix.M11, matrix.M12, matrix.M13).Length;
        float scaleY = new Vector3(matrix.M21, matrix.M22, matrix.M23).Length;
        float scaleZ = new Vector3(matrix.M31, matrix.M32, matrix.M33).Length;

        // Create a normalized rotation matrix by dividing each row by its respective scale factor
        Matrix3 rotationMatrix = new Matrix3(
            matrix.M11 / scaleX, matrix.M12 / scaleY, matrix.M13 / scaleZ,
            matrix.M21 / scaleX, matrix.M22 / scaleY, matrix.M23 / scaleZ,
            matrix.M31 / scaleX, matrix.M32 / scaleY, matrix.M33 / scaleZ
        );

        // Convert the Matrix3 (rotation) to Matrix4 for use in Vector3.TransformPosition
        return new Matrix4(
            rotationMatrix.M11, rotationMatrix.M12, rotationMatrix.M13, 0,
            rotationMatrix.M21, rotationMatrix.M22, rotationMatrix.M23, 0,
            rotationMatrix.M31, rotationMatrix.M32, rotationMatrix.M33, 0,
            0, 0, 0, 1
        );
    }

    public static Matrix3 Transpose(Matrix3 matrix)
    {
        return new Matrix3(
            matrix.M11, matrix.M21, matrix.M31,
            matrix.M12, matrix.M22, matrix.M32,
            matrix.M13, matrix.M23, matrix.M33
        );
    }
}

