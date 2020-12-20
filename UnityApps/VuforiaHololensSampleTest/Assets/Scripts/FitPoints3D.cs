using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.Statistics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet;
using System.Windows;
using System;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using MathNet.Numerics;

public class Point
{
    public double x;
    public double y;
    public double z;

    public Point(double x, double y, double z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
    
    public Point(double[] p)
    {
        this.x = p[0];
        this.y = p[1];
        this.z = p[2];
    }

    /// <summary>
    /// Calculate the resultant point/vector when you add a vector to a point
    /// </summary>
    /// <param name="point">point</param>
    /// <param name="vector">vector</param>
    /// <returns>the resultant point/vector when you add a vector to a point</returns>
    public static Point operator +(Point point, Point vector)
    {
        double x = point.x + vector.x;
        double y = point.y + vector.y;
        double z = point.z + vector.z;
        return new Point(x, y, z);
    }

    /// <summary>
    /// Calculate the vector between the two points
    /// </summary>
    /// <param name="point1">first point</param>
    /// <param name="point2">second point</param>
    /// <returns>A point that represent the vector between the two input</returns>
    public static Point operator -(Point point1, Point point2)
    {
        double x = point1.x - point2.x;
        double y = point1.y - point2.y;
        double z = point1.z - point2.z;
        return new Point(x, y, z);
    }

    /// <summary>
    /// Calculate the scalar multiple of a vector
    /// </summary>
    /// <param name="k">scalar</param>
    /// <param name="vector">vector represented by a point</param>
    /// <returns>the scalar multiple of a vector represented by a point</returns>
    public static Point operator *(double k, Point vector)
    {
        return new Point(k * vector.x, k * vector.y, k * vector.z);
    }

    /// <summary>
    /// Calculate the dot product of two vectors, commutative
    /// </summary>
    /// <param name="vector1">First vector</param>
    /// <param name="vector2">Second vector</param>
    /// <returns>dot product of two vectors</returns>
    public static double operator *(Point vector1, Point vector2)
    {
        return vector1.x * vector2.x + vector1.y * vector2.y + vector1.z * vector2.z;
    }

    public static Point operator *(Matrix<double> transformation, Point point)
    {
        double[] v = { point.x, point.y, point.z, 1 };
        Vector<double> V = Vector<double>.Build.Dense(v);

        Vector<double> V_t = transformation * V;

        return new Point(V_t[0], V_t[1], V_t[2]);
    }

    public static Point operator *(Matrix<float> transformation, Point point)
    {
        return transformation.Map(x => (double)x) * point;
    }

    /// <summary>
    /// Calculate the cross product of two vectors, not commutative
    /// </summary>
    /// <param name="vector1">First vector</param>
    /// <param name="vector2">Second vector</param>
    /// <returns>cross product of two vectors</returns>
    public static Point cross(Point vector1, Point vector2)
    {
        return new Point(vector1.y * vector2.z - vector1.z * vector2.y,
            vector1.z * vector2.x - vector1.x * vector2.z,
            vector1.x * vector2.y - vector1.y * vector2.x);
    }

    /// <summary>
    /// Return the normalized vector of some input vector, i.e. a vector with the same direction with the input vector,
    /// but a magnitude of 1
    /// </summary>
    /// <param name="vector">Original vector</param>
    /// <returns>Normalized vector</returns>
    public static Point Normalize(Point vector)
    {
        return 1 / Math.Sqrt(vector * vector) * vector;
    }


    /// <summary>
    /// Calculate the distance between the two points
    /// </summary>
    /// <param name="point1">first point</param>
    /// <param name="point2">second point</param>
    /// <returns>the distance between the two points</returns>
    public static double Distance(Point point1, Point point2)
    {
        return Math.Sqrt(Math.Pow(point1.x - point2.x, 2) + Math.Pow(point1.y - point2.y, 2) + Math.Pow(point1.z - point2.z, 2));
    }

    public Matrix<double> ToMatrixDouble()
    {
        return Matrix<double>.Build.DenseOfArray(new double[,] { { this.x, this.y, this.z } });
    }



}
public class FitPoints3D
{
 
    // PCA
    public static Matrix<double> Fit(List<Point> source, List<Point> target)
    {
        // Calculate the covariance of the source and target points
        Matrix<double> covSource = Covariance(source);
        Matrix<double> covTarget = Covariance(target);

        // Using Singular Value Decomposition (SVD), get the left unitary matrix U
        MathNet.Numerics.LinearAlgebra.Factorization.Svd<double> svdSource = covSource.Svd();
        MathNet.Numerics.LinearAlgebra.Factorization.Svd<double> svdTarget = covTarget.Svd();
        Matrix<double> uSource = svdSource.U;
        Matrix<double> uTarget = svdTarget.U;

        // Get the Householder matrix also known as a reflection matrix about a plane
        Matrix<double> householder = getHouseHolderMatrix(source);

        // Get all 8 possible rotation matrices based on the determinant
        List<Matrix<double>> possibleRotations = new List<Matrix<double>>();

        if (uTarget.Multiply(uSource.Inverse()).Determinant() > 0)
        {
            possibleRotations.Add(uTarget.Multiply(NegateEigenvectors(uSource, false, false, false).Inverse()));
            possibleRotations.Add(uTarget.Multiply(NegateEigenvectors(uSource, false, false, true).Inverse()).Multiply(householder));
            possibleRotations.Add(uTarget.Multiply(NegateEigenvectors(uSource, false, true, false).Inverse()).Multiply(householder));
            possibleRotations.Add(uTarget.Multiply(NegateEigenvectors(uSource, false, true, true).Inverse()));
            possibleRotations.Add(uTarget.Multiply(NegateEigenvectors(uSource, true, false, false).Inverse()).Multiply(householder));
            possibleRotations.Add(uTarget.Multiply(NegateEigenvectors(uSource, true, false, true).Inverse()));
            possibleRotations.Add(uTarget.Multiply(NegateEigenvectors(uSource, true, true, false).Inverse()));
            possibleRotations.Add(uTarget.Multiply(NegateEigenvectors(uSource, true, true, true).Inverse()).Multiply(householder));
        }
        else
        {
            possibleRotations.Add(uTarget.Multiply(NegateEigenvectors(uSource, false, false, false).Inverse()).Multiply(householder));
            possibleRotations.Add(uTarget.Multiply(NegateEigenvectors(uSource, false, false, true).Inverse()));
            possibleRotations.Add(uTarget.Multiply(NegateEigenvectors(uSource, false, true, false).Inverse()));
            possibleRotations.Add(uTarget.Multiply(NegateEigenvectors(uSource, false, true, true).Inverse()).Multiply(householder));
            possibleRotations.Add(uTarget.Multiply(NegateEigenvectors(uSource, true, false, false).Inverse()));
            possibleRotations.Add(uTarget.Multiply(NegateEigenvectors(uSource, true, false, true).Inverse()).Multiply(householder));
            possibleRotations.Add(uTarget.Multiply(NegateEigenvectors(uSource, true, true, false).Inverse()).Multiply(householder));
            possibleRotations.Add(uTarget.Multiply(NegateEigenvectors(uSource, true, true, true).Inverse()));
        }

        // Setting up the analysis to find the best rotation matrix
        double smallestScore = double.PositiveInfinity;
        Matrix<double> bestTransformation = Matrix<double>.Build.DenseIdentity(4, 4);

        // Calculate the centroid of both triangles
        Point targetCentroid = Centroid(target);
        Point sourceCentroid = Centroid(source);

        // Iterate through the possible rotation matrices to find the best one
        foreach (Matrix<double> R in possibleRotations)
        {
            Matrix<double> R4x4 = Matrix<double>.Build.DenseIdentity(4, 4);
            R4x4.SetSubMatrix(0, 0, R);

            // Calculate the translation vector between the two triangles using the current rotation matrix
            Point T = targetCentroid - R4x4 * sourceCentroid;
            // Generate the total transformation (rotate and translate) using the current rotation matrix and the translation obtained above
            Matrix<double> transformation = Matrix<double>.Build.DenseIdentity(4, 4);

            transformation.SetSubMatrix(0, 0, R);

            transformation[0, 3] = T.x;
            transformation[1, 3] = T.y;
            transformation[2, 3] = T.z;

            double score = 0;

            foreach (var p in source)
            {
                Point transformedPoint = transformation * p;
                score += target.Min(x => Distance(x, transformedPoint)); // Gets the smallest distance between p and any point in target
            }

            if (score < smallestScore)
            {
                smallestScore = score;
                bestTransformation = transformation;
            }
        }

        return bestTransformation;
    }

    public static double[,] FitPoints(double[,] source, double[,] target)
    {

        List<Point> sourcePoints = new List<Point>();
        for (int i = 0; i < source.GetLength(0); i++)
        {
            sourcePoints.Add(new Point(source[i, 0], source[i, 1], source[i, 2]));
        }

        List<Point> targetPoints = new List<Point>();
        for (int i = 0; i < target.GetLength(0); i++)
        {
            targetPoints.Add(new Point(target[i, 0], target[i,1], target[i,2]));
        }

        return Fit(sourcePoints, targetPoints).ToArray();
    }

    /// <summary>
    /// Negate a subset of eigenvectors
    /// </summary>
    /// <param name="originalMatrix">original eigenvectors</param>
    /// <param name="c1">Whether to negate the first eigen vector</param>
    /// <param name="c2">Whether to negate the second eigen vector</param>
    /// <param name="c3">Whether to negate the third eigen vector</param>
    /// <returns>New set of eigenvectors after a subset of it have been negated</returns>
    static Matrix<double> NegateEigenvectors(Matrix<double> originalMatrix, bool c1, bool c2, bool c3)
    {
        Matrix<double> newMatrix = Matrix<double>.Build.Dense(3, 3);
        if (c1)
            newMatrix.SetColumn(0, originalMatrix.Column(0).Multiply(-1));
        else
            newMatrix.SetColumn(0, originalMatrix.Column(0));
        if (c2)
            newMatrix.SetColumn(1, originalMatrix.Column(1).Multiply(-1));
        else
            newMatrix.SetColumn(1, originalMatrix.Column(1));
        if (c3)
            newMatrix.SetColumn(2, originalMatrix.Column(2).Multiply(-1));
        else
            newMatrix.SetColumn(2, originalMatrix.Column(2));
        return newMatrix;
    }

    /// <summary>
    /// Calculates the centroid of a list of points.
    /// </summary>
    /// <param name="points">List of points</param>
    /// <returns>Centroid (average) of the list of points</returns>
    public static Point Centroid(List<Point> points)
    {
        double x = 0, y = 0, z = 0;

        foreach (var p in points)
        {
            x += p.x;
            y += p.y;
            z += p.z;
        }

        x /= points.Count;
        y /= points.Count;
        z /= points.Count;

        return new Point(x, y, z);
    }

    /// <summary>
    /// Calculate the house holder matrix, that can reflect any transformation matrix on a plane that contains a triangle constructed by three vertices
    /// </summary>
    /// <param name="vertex1">First vertex of the triangle</param>
    /// <param name="vertex2">Second vertex of the triangle</param>
    /// <param name="vertex3">Third vertex of the triangle</param>
    /// <returns>House holder matrix that can reflect any transformation on a plane that contains a triangle constructed by three vertices</returns>
    public static Matrix<double> getHouseHolderMatrix(List<Point> points)
    {
        // Initializing a house holder matrix object
        Matrix<double> houseHolderMatrix = Matrix<double>.Build.Dense(3, 3);

        // Calculate two vectors of the triangle
        //Point w = vertex3 - vertex1;
        //Point v = vertex2 - vertex1;

        // Calculate and normalize the normal of the plane that contains a triangle constructed by three vertices
        //Point n = cross(w, v);
        Point nHat = PlaneOfBestFit(points);
        //Point nHat1 = 1 / Math.Sqrt(n * n) * n;

        // Calculate the house holder matrix
        houseHolderMatrix.At(0, 0, 1 - 2 * nHat.x * nHat.x);
        houseHolderMatrix.At(0, 1, -2 * nHat.x * nHat.y);
        houseHolderMatrix.At(0, 2, -2 * nHat.x * nHat.z);
        houseHolderMatrix.At(1, 0, -2 * nHat.x * nHat.y);
        houseHolderMatrix.At(1, 1, 1 - 2 * nHat.y * nHat.y);
        houseHolderMatrix.At(1, 2, -2 * nHat.y * nHat.z);
        houseHolderMatrix.At(2, 0, -2 * nHat.x * nHat.z);
        houseHolderMatrix.At(2, 1, -2 * nHat.y * nHat.z);
        houseHolderMatrix.At(2, 2, 1 - 2 * nHat.z * nHat.z);

        return houseHolderMatrix;
    }

    /// <summary>
    /// Returns the normal of the plane of best fit of the given points
    /// </summary>
    /// <param name="points"></param>
    /// <returns></returns>
    public static Point PlaneOfBestFit(List<Point> points)
    {
        Point centroid = Centroid(points);

        Matrix<double> A = Matrix<double>.Build.Dense(3, points.Count);

        for (int i = 0; i < points.Count; ++i)
        {
            Point temp = points[i] - centroid;
            A.SetColumn(i, new double[] { temp.x, temp.y, temp.z });
        }

        MathNet.Numerics.LinearAlgebra.Factorization.Svd<double> svdA = A.Svd();

        double[] norm = svdA.U.Column(2).AsArray();
        return new Point(norm);
    }


    // Cross product

    public static Matrix<double> FindTransformationBetween(List<Point> source, List<Point> target)
    {
        // source = SortVerticesUsingEdgeLength(source);
        // target = SortVerticesUsingEdgeLength(target);

        var R1 = FindOrthoNormalMatrix(source[0], source[1], source[2]);
        var R2 = FindOrthoNormalMatrix(target[0], target[1], target[2]);

        var R = R2.Multiply(R1.Inverse());

        var T = Matrix<double>.Build.DenseIdentity(4);
        T.SetSubMatrix(0, 0, R);

        var offset = target[1] - T * source[1];

        T[0, 3] = offset.x;
        T[1, 3] = offset.y;
        T[2, 3] = offset.z;

        return T;
    }

    public static Matrix<double> FindOrthoNormalMatrix(Point point1, Point point2, Point point3)
    {
        Point v1 = Point.Normalize(point2 - point1);
        Point v2 = Point.Normalize(point2 - point3);

        Point normal = Point.Normalize(Point.cross(v1, v2));
        Point v2_ortho = Point.Normalize(Point.cross(normal, v1));

        Matrix<double> T = Matrix<double>.Build.Dense(3, 3);
        T[0, 0] = v1.x;
        T[1, 0] = v1.y;
        T[2, 0] = v1.z;
        T[0, 1] = v2_ortho.x;
        T[1, 1] = v2_ortho.y;
        T[2, 1] = v2_ortho.z;
        T[0, 2] = normal.x;
        T[1, 2] = normal.y;
        T[2, 2] = normal.z;

        return T;
    }

    public static List<Point> SortVerticesUsingEdgeLength(List<Point> points)
    {
        if (points.Count < 3)
            return points;

        var edgeTuples = new List<Tuple<int, int, double>>();
        for (int i = 0; i < points.Count; i++)
            for (int j = i + 1; j < points.Count; j++)
                edgeTuples.Add(new Tuple<int, int, double>(i, j, Point.Distance(points[i], points[j])));
        edgeTuples.Sort((t1, t2) => t1.Item3 > t2.Item3 ? 1 : -1);

        var firstFourEdgesTuples = new List<Tuple<int, int, double>>();
        var point00 = points[edgeTuples[0].Item1];
        var point01 = points[edgeTuples[0].Item2];
        var point10 = points[edgeTuples[1].Item1];
        var point11 = points[edgeTuples[1].Item2];
        firstFourEdgesTuples.Add(new Tuple<int, int, double>(0, 0, Point.Distance(point00, point10)));
        firstFourEdgesTuples.Add(new Tuple<int, int, double>(0, 1, Point.Distance(point00, point11)));
        firstFourEdgesTuples.Add(new Tuple<int, int, double>(1, 0, Point.Distance(point01, point10)));
        firstFourEdgesTuples.Add(new Tuple<int, int, double>(1, 1, Point.Distance(point01, point11)));

        firstFourEdgesTuples.Sort((t1, t2) => t1.Item3 > t2.Item3 ? 1 : -1);

        var sortedIndex = new List<int>
            {
                firstFourEdgesTuples[0].Item1 == 0 ? edgeTuples[0].Item1 : edgeTuples[0].Item2
            };

        while (sortedIndex.Count < points.Count)
        {
            var neighbourTuples = new List<Tuple<int, double>>();
            for (int i = 0; i < points.Count; i++)
                neighbourTuples.Add(new Tuple<int, double>(i, Point.Distance(points[sortedIndex[sortedIndex.Count - 1]], points[i])));

            neighbourTuples.Sort((n1, n2) => n1.Item2 > n2.Item2 ? 1 : -1);

            for (int i = 1; i < neighbourTuples.Count; i++)
                if (!sortedIndex.Contains(neighbourTuples[i].Item1))
                {
                    sortedIndex.Add(neighbourTuples[i].Item1);
                    break;
                }
        }

        var sortedPoints = sortedIndex.Select(i => points[i]).ToList();

        return sortedPoints;
    }

    /// <summary>
    /// Calculates the Covariance matrix of a list of points
    /// </summary>
    /// <param name="points"></param>
    /// <returns></returns>
    public static Matrix<double> Covariance(List<Point> points)
    {
        Point centroid = Centroid(points);
        Matrix<double> cov = Matrix<double>.Build.Dense(3, 3);
        cov.SetRow(0, new double[3] { 0, 0, 0 });
        cov.SetRow(1, new double[3] { 0, 0, 0 });
        cov.SetRow(2, new double[3] { 0, 0, 0 });

        foreach (var p in points)
        {
            Matrix<double> v = (p - centroid).ToMatrixDouble();

            cov += v * v.Transpose();
        }

        cov /= points.Count;

        return cov;
    }

    /// <summary>
    /// Calculate the distance between the two points
    /// </summary>
    /// <param name="point1">first point</param>
    /// <param name="point2">second point</param>
    /// <returns>the distance between the two points</returns>
    public static double Distance(Point point1, Point point2)
    {
        return Math.Sqrt(Math.Pow(point1.x - point2.x, 2) + Math.Pow(point1.y - point2.y, 2) + Math.Pow(point1.z - point2.z, 2));
    }
}
