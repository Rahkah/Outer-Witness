using UnityEngine;
using UnityEditor;

namespace OuterWitness.Editor
{
    public class PlanetMeshFixer : EditorWindow
    {
        [MenuItem("Tools/Outer Witness/Planet Tools")]
        public static void ShowWindow()
        {
            GetWindow<PlanetMeshFixer>("Planet Tools");
        }

        private void OnGUI()
        {
            GUILayout.Label("高精度星球生成工具", EditorStyles.boldLabel);

            if (GUILayout.Button("创建高精度星球 (64x32)"))
            {
                CreateHighResPlanet(64, 32);
            }

            if (GUILayout.Button("创建超高精度星球 (128x64)"))
            {
                CreateHighResPlanet(128, 64);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("修复选中物体的法线"))
            {
                FixSelectedObjectNormals();
            }
        }

        private void CreateHighResPlanet(int horizontalSegments, int verticalSegments)
        {
            GameObject planet = new GameObject($"Planet_{horizontalSegments}x{verticalSegments}");

            Mesh mesh = CreateHighResSphereMesh(horizontalSegments, verticalSegments);

            MeshFilter meshFilter = planet.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            MeshRenderer renderer = planet.AddComponent<MeshRenderer>();

            // 尝试使用现有材质，如果找不到则创建新材质
            Material existingMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/AttlerockMaterial.mat");
            Material planetMaterial = existingMaterial != null ? existingMaterial : new Material(Shader.Find("Universal Render Pipeline/Lit"));

            if (existingMaterial == null)
            {
                planetMaterial.color = new Color(0.6f, 0.8f, 1f);
                planetMaterial.SetFloat("_Metallic", 0f);
                planetMaterial.SetFloat("_Smoothness", 0.5f);

                // 确保材质设置为不透明
                planetMaterial.SetFloat("_Surface", 0); // 0 = Opaque
                planetMaterial.SetFloat("_Blend", 0); // 0 = Alpha
                planetMaterial.SetFloat("_AlphaClip", 0);
                planetMaterial.SetFloat("_Cull", 2); // 2 = Back, 只渲染外表面

                // 关键：设置为不透明渲染队列
                planetMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            }

            renderer.sharedMaterial = planetMaterial;

            // 设置正确的阴影模式和接收
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;

            planet.AddComponent<SphereCollider>();

            // 强制重新计算法线以确保正确方向
            mesh.RecalculateNormals();

            Selection.activeGameObject = planet;
            Debug.Log($"创建了 {horizontalSegments}x{verticalSegments} 高精度星球", planet);
        }

        private void FixSelectedObjectNormals()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择一个包含Mesh的物体！", "确定");
                return;
            }

            MeshFilter meshFilter = selected.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                EditorUtility.DisplayDialog("错误", "所选物体没有MeshFilter或Mesh！", "确定");
                return;
            }

            // 创建Mesh的副本以避免修改原始资源
            Mesh meshCopy = Instantiate(meshFilter.sharedMesh);
            meshFilter.sharedMesh = meshCopy;

            meshCopy.RecalculateNormals();
            meshCopy.RecalculateTangents();
            meshCopy.RecalculateBounds();

            Debug.Log($"已修复 {selected.name} 的法线", selected);
        }

        private Mesh CreateHighResSphereMesh(int horizontalSegments, int verticalSegments)
        {
            Mesh mesh = new Mesh();

            int numVertices = (horizontalSegments + 1) * (verticalSegments + 1);
            Vector3[] vertices = new Vector3[numVertices];
            Vector3[] normals = new Vector3[numVertices];
            Vector2[] uv = new Vector2[numVertices];
            int[] triangles = new int[horizontalSegments * verticalSegments * 6];

            for (int y = 0; y <= verticalSegments; y++)
            {
                for (int x = 0; x <= horizontalSegments; x++)
                {
                    int index = y * (horizontalSegments + 1) + x;

                    float angleX = (float)x / horizontalSegments * Mathf.PI * 2;
                    float angleY = (float)y / verticalSegments * Mathf.PI;

                    float xPos = Mathf.Sin(angleY) * Mathf.Cos(angleX);
                    float yPos = Mathf.Cos(angleY);
                    float zPos = Mathf.Sin(angleY) * Mathf.Sin(angleX);

                    vertices[index] = new Vector3(xPos, yPos, zPos);
                    normals[index] = vertices[index].normalized; // 法线指向外侧（原始方向）
                    uv[index] = new Vector2((float)x / horizontalSegments, (float)y / verticalSegments);
                }
            }

            int triangleIndex = 0;
            for (int y = 0; y < verticalSegments; y++)
            {
                for (int x = 0; x < horizontalSegments; x++)
                {
                    int baseIndex = y * (horizontalSegments + 1) + x;

                    // 反转三角形绕序，使法线指向外侧
                    triangles[triangleIndex++] = baseIndex;
                    triangles[triangleIndex++] = baseIndex + 1;
                    triangles[triangleIndex++] = baseIndex + horizontalSegments + 1;

                    triangles[triangleIndex++] = baseIndex + 1;
                    triangles[triangleIndex++] = baseIndex + horizontalSegments + 2;
                    triangles[triangleIndex++] = baseIndex + horizontalSegments + 1;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = triangles;

            return mesh;
        }
    }
}