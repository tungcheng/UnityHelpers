﻿using UnityEngine;
using MIConvexHull;
using System.Collections.Generic;
using System.Linq;
using g3;

namespace UnityHelpers
{
    public static class MeshHelpers
    {
        public const int MAX_VERTICES = 65534;
        public enum TriangleSearchType { all, any, none, }

        /// <summary>
        /// Appends the vertices, triangle, normals, uv, uv2, uv3, uv4 of the other mesh to the current mesh.
        /// </summary>
        /// <param name="current">The mesh to append to</param>
        /// <param name="other">The mesh that will be appended</param>
        /// <param name="position">Position of TRS matrix</param>
        /// <param name="rotation">Rotation of TRS matrix</param>
        /// <param name="scale">Scale of TRS matrix</param>
        /// <returns>False if the mesh being appended to cannot fit what is being appended</returns>
        public static bool Append(this MeshData current, MeshData other, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (current.vertices.Length + other.vertices.Length < MAX_VERTICES)
            {
                Vector3[] manipulatedVertices = ManipulateVertices(other.vertices, position, rotation, scale);

                int[] correctedTriangles = new int[other.triangles.Length];
                for (int i = 0; i < correctedTriangles.Length; i++)
                    correctedTriangles[i] = other.triangles[i] + current.vertices.Length;

                current.vertices = current.vertices.Concat(manipulatedVertices).ToArray();
                current.triangles = current.triangles.Concat(correctedTriangles).ToArray();
                current.normals = current.normals.Concat(other.normals).ToArray();
                current.uv = current.uv.Concat(other.uv).ToArray();
                current.uv2 = current.uv2.Concat(other.uv2).ToArray();
                current.uv3 = current.uv3.Concat(other.uv3).ToArray();
                current.uv4 = current.uv4.Concat(other.uv4).ToArray();
                return true;
            }
            else
                Debug.LogWarning("MeshHelpers: Could not append mesh to other mesh");

            return false;
        }
        /// <summary>
        /// Appends the vertices, triangle, normals, uv, uv2, uv3, uv4 of the other mesh to the current mesh.
        /// </summary>
        /// <param name="current">The mesh to append to</param>
        /// <param name="other">The mesh that will be appended</param>
        /// <param name="position">Position of TRS matrix</param>
        /// <param name="rotation">Rotation of TRS matrix</param>
        /// <param name="scale">Scale of TRS matrix</param>
        /// <returns>False if the mesh being appended to cannot fit what is being appended</returns>
        public static bool Append(this Mesh current, Mesh other, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (current.vertices.Length + other.vertices.Length < MAX_VERTICES)
            {
                Vector3[] manipulatedVertices = ManipulateVertices(other.vertices, position, rotation, scale);

                int[] correctedTriangles = new int[other.triangles.Length];
                for (int i = 0; i < correctedTriangles.Length; i++)
                    correctedTriangles[i] = other.triangles[i] + current.vertices.Length;

                current.vertices = current.vertices.Concat(manipulatedVertices).ToArray();
                current.triangles = current.triangles.Concat(correctedTriangles).ToArray();
                current.normals = current.normals.Concat(other.normals).ToArray();
                current.uv = current.uv.Concat(other.uv).ToArray();
                current.uv2 = current.uv2.Concat(other.uv2).ToArray();
                current.uv3 = current.uv3.Concat(other.uv3).ToArray();
                current.uv4 = current.uv4.Concat(other.uv4).ToArray();
                return true;
            }
            else
                Debug.LogWarning("MeshHelpers: Could not append mesh to other mesh");

            return false;
        }
        /// <summary>
        /// Manipulates all vertices of an array using a TRS matrix
        /// </summary>
        /// <param name="original">The vertices to be manipulated</param>
        /// <param name="position">The position of the TRS</param>
        /// <param name="rotation">The rotation of the TRS</param>
        /// <param name="scale">The scale of the TRS</param>
        /// <returns>The manipulated vertices</returns>
        public static Vector3[] ManipulateVertices(this Vector3[] original, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (!rotation.IsValid())
                rotation = rotation.FixNaN();

            Vector3[] manipulated = new Vector3[original.Length];
            Matrix4x4 manipulator = Matrix4x4.identity;
            manipulator.SetTRS(position, rotation, scale);
            int i = 0;
            while (i < original.Length)
            {
                manipulated[i] = manipulator.MultiplyPoint3x4(original[i]);
                i++;
            }
            return manipulated;
        }

        /// <summary>
        /// Shifts all the indices within the enumerable by the given amount
        /// </summary>
        /// <param name="triangles">The triangles sequence</param>
        /// <param name="shiftAmount">The amount to shift by</param>
        /// <returns>A shifted triangle sequence</returns>
        public static IEnumerable<int> ShiftTriangleIndices(this IEnumerable<int> triangles, int shiftAmount)
        {
            return triangles.Select((index) => index + shiftAmount);
        }
        /// <summary>
        /// Finds all triangles with values within the given range
        /// </summary>
        /// <param name="triangles">The triangles sequence</param>
        /// <param name="startIndex">The beginning of the range [inclusive]</param>
        /// <param name="endIndex">The end of the range [inclusive]</param>
        /// <param name="searchType">If set to all then all three of the triangle values must be within the range. If set to any then only one has to comply for the triangle to make the cut. If set to none then none of the triangle values have to be within the range to be added.</param>
        /// <returns>A subset of the original triangles sequence</returns>
        public static IEnumerable<int> FindTriangles(this IEnumerable<int> triangles, int startIndex, int endIndex, TriangleSearchType searchType = TriangleSearchType.all)
        {
            int[] currentTriangle = new int[3];
            List<int> containedTriangles = new List<int>();
            int currentIndex = 0;
            bool hadGoodIndex = false;
            bool hadBadIndex = false;
            bool toBeAdded = false;
            var enumerator = triangles.GetEnumerator();
            while (enumerator.MoveNext())
            {
                int subIndex = currentIndex % 3;
                if (currentIndex > 0 && subIndex == 0 && toBeAdded)
                {
                    containedTriangles.AddRange(currentTriangle);
                    hadGoodIndex = false;
                    hadBadIndex = false;
                }

                if (enumerator.Current >= startIndex && enumerator.Current <= endIndex)
                    hadGoodIndex = true;
                else
                    hadBadIndex = true;
                
                currentTriangle[subIndex] = enumerator.Current;
                currentIndex++;
                toBeAdded = ((searchType == TriangleSearchType.all && !hadBadIndex) || (searchType == TriangleSearchType.any && hadGoodIndex) || (searchType == TriangleSearchType.none && !hadGoodIndex));
            }
            //The while loop misses the last triangle, so we add it as long as there actually were triangles to begin with
            if (triangles.Count() > 0 && toBeAdded)
                containedTriangles.AddRange(currentTriangle);

            return containedTriangles;
        }

        /// <summary>
        /// Checks if a point is on the surface of the object's mesh. This method goes through all the triangles of the mesh.
        /// This variant is faster, but it is less accurate. Works better with meshes that have triangles with consistently small areas around the size of the given range.
        /// </summary>
        /// <param name="currentObject">The object to be checked. Must have a MeshFilter component attached.</param>
        /// <param name="point">The point to be checked.</param>
        /// <param name="range">The distance from the point to check. (Clamped between 0.1 and MaxValue)</param>
        /// <returns>True if the point is on the surface of the mesh up to a certain range.</returns>
        public static bool IsPointOnSurface(this Transform currentObject, Vector3 point, float range)
        {
            bool onSurface = false;
            MeshFilter meshFilter = currentObject.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                Mesh mesh = meshFilter.sharedMesh;
                onSurface = mesh.IsPointOnSurface(currentObject.InverseTransformPoint(point), range);
            }
            return onSurface;
        }
        /// <summary>
        /// Checks if a point is on the surface of the mesh. This method goes through all the triangles of the mesh.
        /// This variant is faster, but it is less accurate. Works better with meshes that have triangles with consistently small areas around the size of the given range.
        /// </summary>
        /// <param name="mesh">The mesh to be checked.</param>
        /// <param name="point">The point to be checked.</param>
        /// <param name="range">The distance from the point to check. (Clamped between 0.1 and MaxValue)</param>
        /// <returns>True if the point is on the surface of the mesh up to a certain range.</returns>
        public static bool IsPointOnSurface(this Mesh mesh, Vector3 point, float range)
        {
            bool onSurface = false;

            Bounds pointBounds = new Bounds(point, Vector3.one * Mathf.Clamp(range, 0.1f, float.MaxValue));

            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                Vector3 vertexA = mesh.vertices[mesh.triangles[i + 0]];
                Vector3 vertexB = mesh.vertices[mesh.triangles[i + 1]];
                Vector3 vertexC = mesh.vertices[mesh.triangles[i + 2]];

                if (pointBounds.Contains(vertexA) || pointBounds.Contains(vertexB) || pointBounds.Contains(vertexC) || pointBounds.Contains(CalculateTriangleCenter(vertexA, vertexB, vertexC)))
                    onSurface = true;
            }
            return onSurface;
        }
        /// <summary>
        /// Checks if a point is on the surface of the object's mesh. This method goes through all the triangles of the mesh.
        /// </summary>
        /// <param name="currentObject">The object to be checked. Must have a MeshFilter component attached.</param>
        /// <param name="point">The point to be checked.</param>
        /// <returns>True if the point is on the surface of the mesh.</returns>
        public static bool IsPointOnSurface(this Transform currentObject, Vector3 point)
        {
            bool onSurface = false;
            MeshFilter meshFilter = currentObject.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                Mesh mesh = meshFilter.sharedMesh;
                onSurface = mesh.IsPointOnSurface(currentObject.InverseTransformPoint(point));
            }
            return onSurface;
        }
        /// <summary>
        /// Checks if a point is on the surface of the mesh. This method goes through all the triangles of the mesh.
        /// </summary>
        /// <param name="mesh">The mesh to be checked.</param>
        /// <param name="point">The point to be checked.</param>
        /// <returns>True if the point is on the surface of the mesh.</returns>
        public static bool IsPointOnSurface(this Mesh mesh, Vector3 point)
        {
            bool onSurface = false;
            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                Vector3 triangleA = mesh.vertices[mesh.triangles[i + 0]];
                Vector3 triangleB = mesh.vertices[mesh.triangles[i + 1]];
                Vector3 triangleC = mesh.vertices[mesh.triangles[i + 2]];

                if (PointInTriangle(triangleA, triangleB, triangleC, point))
                {
                    onSurface = true;
                }
            }
            return onSurface;
        }
        /// <summary>
        /// Checks if a point is inside of the object's mesh. This function is slow due to the need to build a spatial data structure using geometry3Sharp.
        /// </summary>
        /// <param name="currentObject">The object to be checked. Must have a MeshFilter component attached.</param>
        /// <param name="point">The point in question.</param>
        /// <returns>Whether the point is inside the mesh.</returns>
        public static bool IsPointInside(this Transform currentObject, Vector3 point)
        {
            bool isInside = false;
            MeshFilter meshFilter = currentObject.GetComponent<MeshFilter>();

            if (meshFilter != null)
            {
                Mesh mesh = meshFilter.sharedMesh;
                isInside = mesh.IsPointInside(currentObject.InverseTransformPoint(point));
            }

            return isInside;
        }
        /// <summary>
        /// Checks if a point is inside of a mesh. This function is slow due to the need to build a spatial data structure using geometry3Sharp.
        /// </summary>
        /// <param name="mesh">The mesh to be checked.</param>
        /// <param name="point">The point in question.</param>
        /// <returns>Whether the point is inside the mesh.</returns>
        public static bool IsPointInside(this Mesh mesh, Vector3 point)
        {
            var mesh3 = GenerateDynamicMesh(mesh.vertices, mesh.triangles, mesh.normals);
            var spatial = new DMeshAABBTree3(mesh3);
            spatial.Build();
            return spatial.IsInside(new Vector3d(point.x, point.y, point.z));
        }
        /// <summary>
        /// Gets the center of a triangle given it's three points.
        /// </summary>
        /// <param name="vertexA">First point of the triangle</param>
        /// <param name="vertexB">Second point of the triangle</param>
        /// <param name="vertexC">Third point of the triangle</param>
        /// <returns>The center of the triangle</returns>
        public static Vector3 CalculateTriangleCenter(Vector3 vertexA, Vector3 vertexB, Vector3 vertexC)
        {
            return (vertexA + vertexB + vertexC) / 3f;
        }
        /// <summary>
        /// Calculates the normal of the triangle given it's three points.
        /// </summary>
        /// <param name="vertexA">First point of the triangle</param>
        /// <param name="vertexB">Second point of the triangle</param>
        /// <param name="vertexC">Third point of the triangle</param>
        /// <returns>The direction perpendicular to the triangle's face</returns>
        public static Vector3 CalculateTriangleNormal(Vector3 vertexA, Vector3 vertexB, Vector3 vertexC)
        {
            return Vector3.Cross(vertexB - vertexA, vertexC - vertexA).normalized;
        }
        /// <summary>
        /// Calculates the area of the triangle given it's three points.
        /// </summary>
        /// <param name="vertexA">First point of the triangle</param>
        /// <param name="vertexB">Second point of the triangle</param>
        /// <param name="vertexC">Third point of the triangle</param>
        /// <returns>The surface area of the face of the triangle</returns>
        public static float CalculateTriangleArea(Vector3 vertexA, Vector3 vertexB, Vector3 vertexC)
        {
            float distanceAB = Vector3.Distance(vertexA, vertexB);
            float distanceAC = Vector3.Distance(vertexA, vertexC);
            return (distanceAB * distanceAC * Mathf.Sin(Vector3.Angle(vertexB - vertexA, vertexC - vertexA) * Mathf.Deg2Rad)) / 2f;
        }
        /// <summary>
        /// Checks if a point is in a triangle.
        /// </summary>
        /// <param name="triangleA">First triangle vertex.</param>
        /// <param name="triangleB">Second triangle vertex.</param>
        /// <param name="triangleC">Third triangle vertex.</param>
        /// <param name="point">The point to be checked.</param>
        /// <returns>True if the point is in the triangle, false otherwise.</returns>
        public static bool PointInTriangle(Vector3 triangleA, Vector3 triangleB, Vector3 triangleC, Vector3 point)
        {
            if (SameSide(point, triangleA, triangleB, triangleC) && SameSide(point, triangleB, triangleA, triangleC) && SameSide(point, triangleC, triangleA, triangleB))
            {
                Vector3 vc1 = Vector3.Cross(triangleA - triangleB, triangleA - triangleC);
                if (Mathf.Abs(Vector3.Dot((triangleA - point).normalized, vc1.normalized)) <= .01f)
                    return true;
            }

            return false;
        }
        private static bool SameSide(Vector3 p1, Vector3 p2, Vector3 A, Vector3 B)
        {
            Vector3 cp1 = Vector3.Cross(B - A, p1 - A);
            Vector3 cp2 = Vector3.Cross(B - A, p2 - A);
            if (Vector3.Dot(cp1, cp2) >= 0) return true;
            return false;
        }

        /// <summary>
        /// Reduces the triangle count of the given mesh.
        /// </summary>
        /// <param name="vertices">The vertices of the mesh.</param>
        /// <param name="triangles">The triangles of the mesh.</param>
        /// <param name="normals">The normals of the mesh.</param>
        /// <param name="trianglePercent">The percent of the original triangle count the final triangle count should be.</param>
        /// <returns>The decimated mesh.</returns>
        public static MeshData DecimateByTriangleCount(IEnumerable<Vector3> vertices, IEnumerable<int> triangles, IEnumerable<Vector3> normals, float trianglePercent = 0.5f)
        {
            var mesh = GenerateDynamicMesh(vertices, triangles, normals);
            var reducer = new Reducer(mesh);
            reducer.ReduceToTriangleCount(Mathf.RoundToInt(Mathf.Clamp01(trianglePercent) * (triangles.Count() / 3)));
            return new MeshData(reducer.Mesh);
        }
        /// <summary>
        /// Reduces the vertex count of the given mesh.
        /// </summary>
        /// <param name="vertices">The vertices of the mesh.</param>
        /// <param name="triangles">The triangles of the mesh.</param>
        /// <param name="normals">The normals of the mesh.</param>
        /// <param name="vertexPercent">The percent of the original vertex count the final vertex count should be.</param>
        /// <returns>The decimated mesh.</returns>
        public static MeshData DecimateByVertexCount(IEnumerable<Vector3> vertices, IEnumerable<int> triangles, IEnumerable<Vector3> normals, float vertexPercent = 0.5f)
        {
            var mesh = GenerateDynamicMesh(vertices, triangles, normals);
            var reducer = new Reducer(mesh);
            reducer.ReduceToVertexCount(Mathf.RoundToInt(Mathf.Clamp01(vertexPercent) * vertices.Count()));
            return new MeshData(reducer.Mesh);
        }
        private static DMesh3 GenerateDynamicMesh(IEnumerable<Vector3> vertices, IEnumerable<int> triangles, IEnumerable<Vector3> normals)
        {
            return DMesh3Builder.Build(vertices.Select(vertex => new Vector3f(vertex.x, vertex.y, vertex.z)), triangles, normals.Select(vector => new Vector3f(vector.x, vector.y, vector.z)));
        }

        /// <summary>
        /// Creates a convex hull given the original vertices of a mesh.
        /// </summary>
        /// <param name="original">The original vertices of a mesh</param>
        /// <param name="convexMesh">The output mesh data of the convex hull</param>
        /// <param name="PlaneDistanceTolerance">The plane distance tolerance</param>
        /// <returns>The outcome of the process (successfull or unsuccessful)</returns>
        public static ConvexHullCreationResultOutcome GenerateConvexHull(Vector3[] original, out MeshData convexMesh, double PlaneDistanceTolerance = Constants.DefaultPlaneDistanceTolerance)
        {
            convexMesh = new MeshData();

            List<Vertex> vertices = original.Select(point => new Vertex(point)).ToList();
            var creation = ConvexHull.Create(vertices, PlaneDistanceTolerance);
            var result = creation.Result;

            List<int> triangles = new List<int>();
            List<Vertex> resultVertices = result.Points.ToList();
            foreach (var face in result.Faces)
            {
                triangles.Add(resultVertices.IndexOf(face.Vertices[0]));
                triangles.Add(resultVertices.IndexOf(face.Vertices[1]));
                triangles.Add(resultVertices.IndexOf(face.Vertices[2]));
            }

            convexMesh.vertices = result.Points.Select(point => point.ToVec()).ToArray();
            convexMesh.triangles = triangles.ToArray();
            //convexMesh.RecalculateNormals();
            //convexMesh.RecalculateBounds();

            return creation.Outcome;
        }

        /// <summary>
        /// The vertex proxy between MIConvexHull and Unity.
        /// </summary>
        private class Vertex : IVertex
        {
            public double[] Position { get; set; }
            public Vertex(double x, double y, double z)
            {
                Position = new double[3] { x, y, z };
            }
            public Vertex(Vector3 ver)
            {
                Position = new double[3] { ver.x, ver.y, ver.z };
            }
            public Vector3 ToVec()
            {
                return new Vector3((float)Position[0], (float)Position[1], (float)Position[2]);
            }
        }

        /// <summary>
        /// The mesh data class is used to help with the creation of
        /// meshes. The main reason for having a separate class is to
        /// allow the manipulation of meshes on separate threads.
        /// </summary>
        public class MeshData
        {
            public Vector3[] vertices;
            public int[] triangles;
            public Vector3[] normals;
            public Color[] colors;
            public Vector2[] uv;
            public Vector2[] uv2;
            public Vector2[] uv3;
            public Vector2[] uv4;

            public MeshData()
            {
                vertices = new Vector3[0];
                triangles = new int[0];
                normals = new Vector3[0];
                colors = new Color[0];
                uv = new Vector2[0];
                uv2 = new Vector2[0];
                uv3 = new Vector2[0];
                uv4 = new Vector2[0];
            }
            public MeshData(Mesh _mesh) : this(_mesh.vertices, _mesh.triangles, _mesh.normals, _mesh.colors, _mesh.uv)
            {
                uv2 = _mesh.uv2;
                uv3 = _mesh.uv3;
                uv4 = _mesh.uv4;
            }
            public MeshData(IEnumerable<Vector3> _vertices, IEnumerable<int> _triangles) : this()
            {
                vertices = _vertices.ToArray();
                triangles = _triangles.ToArray();
            }
            public MeshData(IEnumerable<Vector3> _vertices, IEnumerable<int> _triangles, IEnumerable<Vector3> _normals) : this(_vertices, _triangles)
            {
                normals = _normals.ToArray();
            }
            public MeshData(IEnumerable<Vector3> _vertices, IEnumerable<int> _triangles, IEnumerable<Vector3> _normals, IEnumerable<Color> _colors) : this(_vertices, _triangles, _normals)
            {
                colors = _colors.ToArray();
            }
            public MeshData(IEnumerable<Vector3> _vertices, IEnumerable<int> _triangles, IEnumerable<Vector3> _normals, IEnumerable<Vector2> _uv) : this(_vertices, _triangles, _normals)
            {
                uv = _uv.ToArray();
            }
            public MeshData(IEnumerable<Vector3> _vertices, IEnumerable<int> _triangles, IEnumerable<Vector3> _normals, IEnumerable<Color> _colors, IEnumerable<Vector2> _uv) : this(_vertices, _triangles, _normals, _colors)
            {
                uv = _uv.ToArray();
            }
            public MeshData(DMesh3 _mesh) : this()
            {
                _mesh = new DMesh3(_mesh, true);
                vertices = _mesh.VertexIndices().Select(vID => { var vertex = _mesh.GetVertexf(vID); return new Vector3(vertex.x, vertex.y, vertex.z); }).ToArray();
                triangles = _mesh.TriangleIndices().SelectMany(tID => _mesh.GetTriangle(tID).array).ToArray();
            }
            public void Dispose()
            {
                vertices = null;
                triangles = null;
                normals = null;
                uv = null;
                uv2 = null;
                uv3 = null;
                uv4 = null;
            }

            public Mesh GenerateMesh()
            {
                Mesh mesh = new Mesh();
                mesh.name = "Custom Mesh";

                mesh.Clear();
                mesh.vertices = vertices;
                mesh.triangles = triangles;
                mesh.colors = colors;
                if (uv.Length > 0)
                    mesh.uv = uv;
                if (uv2.Length > 0)
                    mesh.uv2 = uv2;
                if (uv3.Length > 0)
                    mesh.uv3 = uv3;
                if (uv4.Length > 0)
                    mesh.uv4 = uv4;
                if (normals.Length == vertices.Length)
                    mesh.normals = normals;
                else
                {
                    Debug.LogWarning("MeshData: Normals not same length as vertices, recalculating normals");
                    mesh.RecalculateNormals();
                }

                return mesh;
            }
            public System.Collections.IEnumerator DebugNormals()
            {
                for (int i = 0; i < vertices.Length; i++)
                {
                    Debug.DrawRay(vertices[i], normals[i], Color.blue, 10000);
                    if (i % 100 == 0)
                        yield return null;
                }
            }
        }
    }
}