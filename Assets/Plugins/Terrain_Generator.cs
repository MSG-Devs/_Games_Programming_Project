using System;
using UnityEngine;
using UnityEditor;
using JetBrains.Annotations;
using System.Security.Cryptography;
using UnityEngine.UI;
using System.Collections;
using TMPro.EditorUtilities;
using System.Linq;
using UnityEditor.AnimatedValues;


//https://www.youtube.com/watch?v=bQdb90KK2l8

public class Terrain_Generator : EditorWindow
{
    [MenuItem("Window/Terrain Generator")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow<Terrain_Generator>("Terrain Generator");
    }

    public GameObject generatedTerrain;
    public GameObject[] childrenTiles;

    public Material terrainMaterial = null;

    public Texture2D heightMap1 = null;
    public Texture2D heightMap2 = null;
    public int heightMapScale = 20;
    public int heightMapCount = 1;

    Mesh mesh;

    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;

    public int tileSize = 64;

    public int xGridSize = 1;
    public int zGridSize = 1;

    AnimBool showGridVariables, showTileVariables, showHeightMaps;

    private void OnEnable()
    {
        EditorWindow.GetWindow<Terrain_Generator>("Terrain Generator").minSize = new Vector2(500,500);

        showGridVariables = new AnimBool(false);
        showGridVariables.valueChanged.AddListener(Repaint);

        showTileVariables = new AnimBool(false);
        showTileVariables.valueChanged.AddListener(Repaint);

        showHeightMaps = new AnimBool(false);
        showHeightMaps.valueChanged.AddListener(Repaint);
    }

    void OnGUI()
    {
        GUILayout.BeginVertical("Box");

        GUILayout.Label("Terrain Generator", EditorStyles.boldLabel);

        //GUILayout.Space(10);

        //GUILayout.Label("Please enter the size that you want each tile to be.", EditorStyles.boldLabel);
        //tileSize = EditorGUILayout.IntField("Tile Size", tileSize);

        GUILayout.Space(10);

        GUILayout.Label("Please insert your preferred material into the field below.", EditorStyles.boldLabel);
        terrainMaterial = (Material)EditorGUILayout.ObjectField(terrainMaterial, typeof(Material), true);

        GUILayout.Space(10);

        //showHeightMaps.target = EditorGUILayout.ToggleLeft("Edit no. of height maps.", showHeightMaps.target);

        //heightMapCount = EditorGUILayout.IntField("No. of height Maps.", heightMapCount);

        GUILayout.Label("Please insert your preferred height map into the field below.", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();

        heightMap1 = (Texture2D)EditorGUILayout.ObjectField(heightMap1, typeof(Texture2D), true, GUILayout.Width(100), GUILayout.Height(100));

        heightMap2 = (Texture2D)EditorGUILayout.ObjectField(heightMap2, typeof(Texture2D), true, GUILayout.Width(100), GUILayout.Height(100));

        GUILayout.EndHorizontal();

        heightMapScale = EditorGUILayout.IntField("Scale", heightMapScale);

        GUILayout.EndVertical();

        GUILayout.Space(10);

        GUILayout.BeginVertical("Box");

        GUILayout.Label("Please Select a grid size.", EditorStyles.boldLabel);
        //showGridVariables.target = EditorGUILayout.ToggleLeft("Edit Grid Size", showGridVariables.target);

        GUILayout.Space(10);

        //showTileVariables.target = false;

        xGridSize = EditorGUILayout.IntField("Grid Length", xGridSize);

        zGridSize = EditorGUILayout.IntField("Grid height", zGridSize);

        GUILayout.Space(10);

        GUILayout.Label("Please select a tile to change the property of a single tile.", EditorStyles.boldLabel);

        for (int i = 0, z = 0; z < zGridSize; z++)
        {
            GUILayout.BeginHorizontal();
            for (int x = 0; x < xGridSize; x++)
            {
                if(GUILayout.Button("(" + (x +1) + "," + (zGridSize-z) + ")", GUILayout.Width(35), GUILayout.Height(35)))
                {
                    Debug.Log("Selected: " + "(" + (x + 1) + "," + (zGridSize - z) + ")");
                    Selection.activeObject = GameObject.Find("(" + (x + 1) + "," + (zGridSize - z) + ")");
                    i++;
                }
            }
            GUILayout.EndHorizontal();
        }

        //GUILayout.Label("Toggle below represents a tile being selected.");
        //showTileVariables.target = EditorGUILayout.ToggleLeft("Show a tiles variables", showTileVariables.target);
        //GUILayout.Button("Variables for tiles here.");

        GUILayout.EndVertical();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Generate new envrionment."))
        {
            generatedTerrain = GameObject.Find("Generated Terrain");
            GenerateTerrain();
        }

        GUILayout.Space(10);

    }
    public void  GenerateTerrain()
    {
        if (generatedTerrain != null)
        {
            DestroyImmediate(generatedTerrain);
        }

        tileSize = 250;

        generatedTerrain = new GameObject("Generated Terrain");

        childrenTiles = new GameObject[xGridSize * zGridSize];

        for (int i = 0, z = 0; z < zGridSize; z++)
        {
            for (int x = 0; x < xGridSize; x++)
            {
                childrenTiles[i] = new GameObject("(" + (x + 1) + "," + (z + 1) + ")");
                childrenTiles[i].transform.parent = generatedTerrain.transform;
                childrenTiles[i].AddComponent<MeshFilter>();
                childrenTiles[i].AddComponent<MeshRenderer>();
                childrenTiles[i].AddComponent<MeshCollider>();

                mesh = new Mesh();
                childrenTiles[i].GetComponent<MeshFilter>().mesh = mesh;

                Texture2D heightmap = new Texture2D(0,0);
                int index = (int)UnityEngine.Random.Range(-100,100);
                if(index <= 0)
                {
                    heightmap = heightMap1;
                    Debug.Log("1");
                }
                else if (index > 0)
                {
                    heightmap = heightMap2;
                    Debug.Log("2");
                }

                Color[] pixels = heightmap.GetPixels(0, 0, heightmap.width, heightmap.height);           

                vertices = new Vector3[(tileSize + 1) * (tileSize + 1)];
                for (int iv = 0, zv = 0; zv <= tileSize; zv++)
                {
                    for (int xv = 0; xv <= tileSize; xv++)
                    {
                        Color color = pixels[(zv * heightmap.width) + xv];
                        if (xv == 0 || xv == tileSize || zv == 0 || zv == tileSize)
                        {
                            //vertices[iv] = new Vector3(xv, 0, zv);
                            vertices[iv] = new Vector3(xv, color.grayscale * heightMapScale, zv);
                        }
                        else
                        {
                            vertices[iv] = new Vector3(xv, color.grayscale * heightMapScale, zv);
                        }
                        iv++;
                    }
                }

                triangles = new int[tileSize * tileSize * 6];
                int vert = 0;
                int tris = 0;
                for (int zt = 0; zt < tileSize; zt++)
                {
                    for (int xt = 0; xt < tileSize; xt++)
                    {
                        triangles[tris + 0] = vert + 0;
                        triangles[tris + 1] = vert + tileSize + 1;
                        triangles[tris + 2] = vert + 1;
                        triangles[tris + 3] = vert + 1;
                        triangles[tris + 4] = vert + tileSize + 1;
                        triangles[tris + 5] = vert + tileSize + 2;
                        vert++;
                        tris += 6;
                    }
                    vert++;
                }

                uvs = new Vector2[vertices.Length];
                for (int iu = 0, zu = 0; zu <= tileSize; zu++)
                {
                    for (int xu = 0; xu <= tileSize; xu++)
                    {
                        uvs[iu] = new Vector2((float)(xu) / tileSize, (float)(zu) / tileSize);
                        iu++;
                    }
                }

                mesh.Clear();

                mesh.vertices = vertices;
                mesh.triangles = triangles;
                mesh.uv = uvs;

                childrenTiles[i].GetComponent<MeshRenderer>().material = terrainMaterial;

                mesh.RecalculateNormals();

                mesh.RecalculateBounds();
                MeshCollider meshCollider = childrenTiles[i].GetComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;

                childrenTiles[i].transform.position += new Vector3(tileSize*x,0,tileSize*z);
            }
        }
    }
}
