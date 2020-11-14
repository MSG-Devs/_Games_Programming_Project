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

    public Material terrainMaterial;

    public Texture2D heightMap;
    public int heightMapScale;

    Mesh mesh;

    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;

    public int tileSize = 250;

    [Range(1,15)]
    public int xGridSize = 1;
    [Range(1,15)]
    public int zGridSize = 1;

    AnimBool showGridVariables, showTileVariables;

    private void OnEnable()
    {
        EditorWindow.GetWindow<Terrain_Generator>("Terrain Generator").minSize = new Vector2(500,500);

        showGridVariables = new AnimBool(false);
        showGridVariables.valueChanged.AddListener(Repaint);

        showTileVariables = new AnimBool(false);
        showTileVariables.valueChanged.AddListener(Repaint);
    }

    void OnGUI()
    {
        GUILayout.BeginVertical("Box");

        GUILayout.Label("Terrain Generator", EditorStyles.boldLabel);

        GUILayout.Space(10);

        GUILayout.Label("Please insert your preferred material into the field below.", EditorStyles.boldLabel);
        terrainMaterial = (Material)EditorGUILayout.ObjectField(terrainMaterial, typeof(Material), true);

        GUILayout.Space(10);

        GUILayout.Label("Please insert your preferred height map into the field below.", EditorStyles.boldLabel);
        heightMap = (Texture2D)EditorGUILayout.ObjectField(heightMap, typeof(Texture2D), true, GUILayout.Width(100), GUILayout.Height(100));
        heightMapScale = EditorGUILayout.IntField("Scale", heightMapScale);

        GUILayout.Space(10);

        GUILayout.Label("Please enter the size that you want the tiles to be", EditorStyles.boldLabel);
        tileSize = EditorGUILayout.IntField("Tile Size", tileSize);

        GUILayout.EndVertical(); 

        GUILayout.Space(10);

        showGridVariables.target = EditorGUILayout.ToggleLeft("Edit Grid Size", showGridVariables.target);

        GUILayout.Space(10);

        if (EditorGUILayout.BeginFadeGroup(showGridVariables.faded))
        {

            GUILayout.BeginVertical("Box");

            showTileVariables.target = false;

            xGridSize = EditorGUILayout.IntField("Grid Length", xGridSize);

            zGridSize = EditorGUILayout.IntField("Grid height", zGridSize);

            GUILayout.EndVertical();

        }
        else
        {
            GUILayout.Space(10);

            GUILayout.BeginVertical("Box");

            GUILayout.Label("Please select a tile to change the property of a single tile.", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            for (int z = 0; z < zGridSize; z++)
            {
                GUILayout.BeginHorizontal();
                for (int x = 0; x < xGridSize; x++)
                {
                    GUILayout.Button("",GUILayout.Width(25), GUILayout.Height(25));
                }
                GUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;

            GUILayout.Label("Toggle below represents a tile being selected.");
            //Grid
            showTileVariables.target = EditorGUILayout.ToggleLeft("Show a tiles variables", showTileVariables.target);

            if (EditorGUILayout.BeginFadeGroup(showTileVariables.faded))
            {
                EditorGUI.indentLevel++;

                GUILayout.Button("Variables for tiles here.");

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();

            GUILayout.EndVertical();
        }
        EditorGUILayout.EndFadeGroup();

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

        generatedTerrain = new GameObject("Generated Terrain");
        generatedTerrain.AddComponent<MeshFilter>();
        generatedTerrain.AddComponent<MeshRenderer>();
        generatedTerrain.AddComponent<MeshCollider>();

        childrenTiles = new GameObject[xGridSize * zGridSize];

        for (int i = 0, z = 0; z < zGridSize; z++)
        {
            for (int x = 0; x < xGridSize; x++)
            {
                childrenTiles[i] = new GameObject("X: " + (x + 1) + " Z: " + (z + 1));
                childrenTiles[i].transform.parent = generatedTerrain.transform;
                childrenTiles[i].AddComponent<MeshFilter>();
                childrenTiles[i].AddComponent<MeshRenderer>();
                childrenTiles[i].AddComponent<MeshCollider>();
            }
        }


        mesh = new Mesh();
        generatedTerrain.GetComponent<MeshFilter>().mesh = mesh;

        Color[] pixels = heightMap.GetPixels(0,0,heightMap.width,heightMap.height);

        vertices = new Vector3[(tileSize + 1) * (tileSize + 1)];
        for (int i = 0, z = 0; z <= tileSize; z++)
        {
            for (int x = 0; x <= tileSize; x++)
            {
                //float y = pixels[i].grayscale;
                Color color = pixels[(z * heightMap.width) + x];
                vertices[i] = new Vector3(x, color.grayscale*heightMapScale, z);
                i++;
            }
        }

        triangles = new int[tileSize * tileSize * 6];
        int vert = 0;
        int tris = 0;
        for (int z = 0; z < tileSize; z++)
        {
            for (int x = 0; x < tileSize; x++)
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
        for (int i = 0, z = 0; z <= tileSize; z++)
        {
            for (int x = 0; x <= tileSize; x++)
            {
                uvs[i] = new Vector2((float) (x) / tileSize, (float) (z) / tileSize);
                i++;
            }
        }
        UpdateMesh();
    }
    private void Update()
    {

    }
    public void UpdateMesh()
    {
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        generatedTerrain.GetComponent<MeshRenderer>().material = terrainMaterial;

        mesh.RecalculateNormals();

        mesh.RecalculateBounds();
        MeshCollider meshCollider = generatedTerrain.GetComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }



}
