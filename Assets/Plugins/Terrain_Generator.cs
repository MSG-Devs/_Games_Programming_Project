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

    Mesh[] mesh;

    Vector3[][] vertices;
    int[] triangles;
    Vector2[] uvs;

    float[][] heightMapdata;

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
    public void GenerateTerrain()
    {
        if (generatedTerrain != null)
        {
            DestroyImmediate(generatedTerrain);
        }

        mesh = new Mesh[xGridSize * zGridSize];

        heightMapdata = new float[xGridSize * zGridSize][];

        for (int x = 0; x < heightMapdata.Length; x++)
        {
            heightMapdata[x] = new float[(tileSize + 1) * (tileSize + 1)];
        }

        vertices = new Vector3[xGridSize * zGridSize][];

        for (int x = 0; x < vertices.Length; x++)
        {
            vertices[x] = new Vector3[(tileSize + 1) * (tileSize + 1)];
        }

        tileSize = 250;

        generatedTerrain = new GameObject("Generated Terrain");

        childrenTiles = new GameObject[xGridSize * zGridSize];

        //draw the verts

        for (int i = 0, z = 0; z < zGridSize; z++)
        {
            for (int x = 0; x < xGridSize; x++)
            {
                childrenTiles[i] = new GameObject("(" + (x + 1) + "," + (z + 1) + ")");
                childrenTiles[i].transform.parent = generatedTerrain.transform;
                childrenTiles[i].AddComponent<MeshFilter>();
                childrenTiles[i].AddComponent<MeshRenderer>();
                childrenTiles[i].AddComponent<MeshCollider>();

                mesh[i] = new Mesh();
                childrenTiles[i].GetComponent<MeshFilter>().mesh = mesh[i];

                Texture2D heightmap = new Texture2D(0, 0);
                int index = (int)UnityEngine.Random.Range(-100, 100);
                if (index <= 0)
                {
                    heightmap = heightMap1;
                }
                else if (index > 0)
                {
                    heightmap = heightMap2;
                }

                Color[] pixels = heightmap.GetPixels(0, 0, heightmap.width, heightmap.height);

                for (int iv = 0, zv = 0; zv <= tileSize; zv++)
                {
                    for (int xv = 0; xv <= tileSize; xv++)
                    {
                        Color color = pixels[(zv * heightmap.width) + xv];

                        vertices[i][iv] = new Vector3(xv, color.grayscale * heightMapScale, zv);

                        heightMapdata[i][iv] = vertices[i][iv].y;

                        iv++;
                    }
                }
                i++;
            }
        }

        //smooth the vertices

        //
        //current tile: heightMapdata[i][iv]
        //

        //
        //N tile: heightMapdata[i + xGridSize][iv]
        //

        //
        //E tile: heightMapdata[i + 1][iv]
        //

        //
        //S tile: heightMapdata[i - xGridSize][iv]
        //

        //
        //W tile: heightMapdata[i - 1][iv]
        //

        //
        //NE tile: heightMapdata[i + xGridSize + 1][iv]
        //

        //
        //SE tile: heightMapdata[i - xGridSize + 1][iv]
        //

        //
        //SW tile: heightMapdata[i - xGridSize - 1][iv]
        //

        //
        //NW tile: heightMapdata[i + xGridSize - 1][iv]
        //

        for (int i = 0, z = 0; z < zGridSize; z++)
        {
            for (int x = 0; x < xGridSize; x++)
            {
                if (z == 0)
                {
                    if (x == 0)
                    {
                        for (int iv = 0, zv = 0; zv <= tileSize; zv++)
                        {
                            for (int xv = 0; xv <= tileSize; xv++)
                            {
                                //bottom left
                                if (xv == 0 && zv == 0)
                                {
                                    //Do nothing
                                }
                                //bottom right
                                else if (xv == tileSize && zv == 0)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i + 1][iv]) / 2;
                                }
                                //top left
                                else if (xv == 0 && zv == tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i + xGridSize][iv]) / 2;
                                }
                                //top right
                                else if (xv == tileSize && zv == tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i + xGridSize][iv] + heightMapdata[i + xGridSize + 1][iv] + heightMapdata[i + 1][iv]) / 4;
                                }
                                //Bottom face
                                else if (xv > 0 && xv < tileSize && zv == 0)
                                {
                                   //Do nothing
                                }
                                //left face
                                else if (xv == 0 && zv > 0 && zv < tileSize)
                                {
                                    //Do nothing
                                }
                                //right face
                                else if (xv == tileSize && zv > 0 && zv < tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv]+heightMapdata[i+1][iv])/2;
                                }
                                //top face
                                else if (xv > 0 && xv < tileSize && zv == tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i + xGridSize][iv]) / 2;
                                }
                                iv++;
                            }
                        }
                    }
                    else if (x > 0 && x < xGridSize - 1)
                    {
                        for (int iv = 0, zv = 0; zv <= tileSize; zv++)
                        {
                            for (int xv = 0; xv <= tileSize; xv++)
                            {
                                //bottom left
                                if (xv == 0 && zv == 0)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i - 1][iv]) / 2;
                                }
                                //bottom right
                                else if (xv == tileSize && zv == 0)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i + 1][iv]) / 2;
                                }
                                //top left
                                else if (xv == 0 && zv == tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv]) / 4;
                                }
                                //top right
                                else if (xv == tileSize && zv == tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv]) / 4;
                                }
                                //Bottom face
                                else if (xv > 0 && xv < tileSize && zv == 0)
                                {
                                    //Do nothing
                                }
                                //left face
                                else if (xv == 0 && zv > 0 && zv < tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i - 1][iv]) / 2;
                                }
                                //right face
                                else if (xv == tileSize && zv > 0 && zv < tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i + 1][iv]) / 2;
                                }
                                //top face
                                else if (xv > 0 && xv < tileSize && zv == tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i + xGridSize][iv]) / 2;
                                }
                                iv++;
                            }
                        }
                    }
                    else if (x == xGridSize - 1)
                    {
                        for (int iv = 0, zv = 0; zv <= tileSize; zv++)
                        {
                            for (int xv = 0; xv <= tileSize; xv++)
                            {
                                //bottom left
                                if (xv == 0 && zv == 0)
                                {
                                    vertices[i][0].y = (heightMapdata[i][0] + heightMapdata[i - 1][tileSize]) / 2;
                                }
                                //bottom right
                                else if (xv == tileSize && zv == 0)
                                {
                                    //Do nothing
                                }
                                //top left
                                else if (xv == 0 && zv == tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv]) / 4;
                                }
                                //top right
                                else if (xv == tileSize && zv == tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv]) / 2;
                                }
                                //Bottom face
                                else if (xv > 0 && xv < tileSize && zv == 0)
                                {
                                    //Do nothing
                                }
                                //left face
                                else if (xv == 0 && zv > 0 && zv < tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i - 1][iv]) / 2;
                                }
                                //right face
                                else if (xv == tileSize && zv > 0 && zv < tileSize)
                                {
                                    //Do nothing
                                }
                                //top face
                                else if (xv > 0 && xv < tileSize && zv == tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i + xGridSize][iv]) / 2;
                                }
                                iv++;
                            }
                        }
                    }
                }
                else if (z > 0 && z < zGridSize - 1)
                {
                    if (x == 0)
                    {
                        for (int iv = 0, zv = 0; zv <= tileSize; zv++)
                        {
                            for (int xv = 0; xv <= tileSize; xv++)
                            {
                                //bottom left
                                if (xv == 0 && zv == 0)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv]) / 2;
                                }
                                //bottom right
                                else if (xv == tileSize && zv == 0)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv]) / 4;
                                }
                                //top left
                                else if (xv == 0 && zv == tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv]) / 2;
                                }
                                //top right
                                else if (xv == tileSize && zv == tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv]) / 4;
                                }
                                //Bottom face
                                else if (xv > 0 && xv < tileSize && zv == 0)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i - xGridSize][iv]) / 2;
                                }
                                //left face
                                else if (xv == 0 && zv > 0 && zv < tileSize)
                                {
                                    //Do nothing
                                }
                                //right face
                                else if (xv == tileSize && zv > 0 && zv < tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i + 1][iv]) / 2;
                                }
                                //top face
                                else if (xv > 0 && xv < tileSize && zv == tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i + xGridSize][iv]) / 2;
                                }
                                iv++;
                            }
                        }
                    }
                    else if (x > 0 && x < xGridSize - 1)
                    {
                        for (int iv = 0, zv = 0; zv <= tileSize; zv++)
                        {
                            for (int xv = 0; xv <= tileSize; xv++)
                            {
                                //bottom left
                                if (xv == 0 && zv == 0)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv]) / 4;
                                }
                                //bottom right
                                else if (xv == tileSize && zv == 0)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv]) / 4;
                                }
                                //top left
                                else if (xv == 0 && zv == tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv]) / 4;
                                }
                                //top right
                                else if (xv == tileSize && zv == tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv]) / 4;
                                }
                                //Bottom face
                                else if (xv > 0 && xv < tileSize && zv == 0)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i - xGridSize][iv]) / 2;
                                }
                                //left face
                                else if (xv == 0 && zv > 0 && zv < tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i - 1][iv]) / 2;
                                }
                                //right face
                                else if (xv == tileSize && zv > 0 && zv < tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i + 1][iv]) / 2;
                                }
                                //top face
                                else if (xv > 0 && xv < tileSize && zv == tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i + xGridSize][iv]) / 2;
                                }
                                iv++;
                            }
                        }
                    }
                    else if (x == xGridSize - 1)
                    {
                        for (int iv = 0, zv = 0; zv <= tileSize; zv++)
                        {
                            for (int xv = 0; xv <= tileSize; xv++)
                            {
                                //bottom left
                                if (xv == 0 && zv == 0)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv]) / 4;
                                }
                                //bottom right
                                else if (xv == tileSize && zv == 0)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv]) / 2;
                                }
                                //top left
                                else if (xv == 0 && zv == tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv]) / 4;
                                }
                                //top right
                                else if (xv == tileSize && zv == tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv]) / 2;
                                }
                                //Bottom face
                                else if (xv > 0 && xv < tileSize && zv == 0)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i - xGridSize][iv]) / 2;
                                }
                                //left face
                                else if (xv == 0 && zv > 0 && zv < tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i - 1][iv]) / 2;
                                }
                                //right face
                                else if (xv == tileSize && zv > 0 && zv < tileSize)
                                {
                                    //Do nothing
                                }
                                //top face
                                else if (xv > 0 && xv < tileSize && zv == tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i + xGridSize][iv]) / 2;
                                }
                                iv++;
                            }
                        }
                    }
                }
                else if (z == zGridSize - 1)
                {
                    if (x == 0)
                    {
                        for (int iv = 0, zv = 0; zv <= tileSize; zv++)
                        {
                            for (int xv = 0; xv <= tileSize; xv++)
                            {
                                //bottom left
                                if (xv == 0 && zv == 0)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv]) / 2;
                                }
                                //bottom right
                                else if (xv == tileSize && zv == 0)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv]) / 4;
                                }
                                //top left
                                else if (xv == 0 && zv == tileSize)
                                {
                                    //Do nothing
                                }
                                //top right
                                else if (xv == tileSize && zv == tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv]) / 2;
                                }
                                //Bottom face
                                else if (xv > 0 && xv < tileSize && zv == 0)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i - xGridSize][iv]) / 2;
                                }
                                //left face
                                else if (xv == 0 && zv > 0 && zv < tileSize)
                                {
                                    //Do nothing
                                }
                                //right face
                                else if (xv == tileSize && zv > 0 && zv < tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i + 1][iv]) / 2;
                                }
                                //top face
                                else if (xv > 0 && xv < tileSize && zv == tileSize)
                                {
                                    //Do nothing
                                }
                                iv++;
                            }
                        }
                    }
                    else if (x > 0 && x < xGridSize - 1)
                    {
                        for (int iv = 0, zv = 0; zv <= tileSize; zv++)
                        {
                            for (int xv = 0; xv <= tileSize; xv++)
                            {
                                //bottom left
                                if (xv == 0 && zv == 0)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv]) / 4;
                                }
                                //bottom right
                                else if (xv == tileSize && zv == 0)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv]) / 4;
                                }
                                //top left
                                else if (xv == 0 && zv == tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv]) / 2;
                                }
                                //top right
                                else if (xv == tileSize && zv == tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv]) / 2;
                                }
                                //Bottom face
                                else if (xv > 0 && xv < tileSize && zv == 0)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i - xGridSize][iv]) / 2;
                                }
                                //left face
                                else if (xv == 0 && zv > 0 && zv < tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i - 1][iv]) / 2;
                                }
                                //right face
                                else if (xv == tileSize && zv > 0 && zv < tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i + 1][iv]) / 2;
                                }
                                //top face
                                else if (xv > 0 && xv < tileSize && zv == tileSize)
                                {
                                    //Do nothing
                                }
                                iv++;
                            }
                        }
                    }
                    else if (x == xGridSize - 1)
                    {
                        for (int iv = 0, zv = 0; zv <= tileSize; zv++)
                        {
                            for (int xv = 0; xv <= tileSize; xv++)
                            {
                                //bottom left
                                if (xv == 0 && zv == 0)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv]) / 4;
                                }
                                //bottom right
                                else if (xv == tileSize && zv == 0)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv]) / 2;
                                }
                                //top left
                                else if (xv == 0 && zv == tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv]) / 2;
                                }
                                //top right
                                else if (xv == tileSize && zv == tileSize)
                                {
                                    //Do nothing
                                }
                                //Bottom face
                                else if (xv > 0 && xv < tileSize && zv == 0)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i - xGridSize][iv]) / 2;
                                }
                                //left face
                                else if (xv == 0 && zv > 0 && zv < tileSize)
                                {
                                    vertices[i][iv].y = (heightMapdata[i][iv] + heightMapdata[i - 1][iv]) / 2;
                                }
                                //right face
                                else if (xv == tileSize && zv > 0 && zv < tileSize)
                                {
                                    //Do nothing
                                }
                                //top face
                                else if (xv > 0 && xv < tileSize && zv == tileSize)
                                {
                                    //Do nothing
                                }
                                iv++;
                            }
                        }
                    }
                }
                i++;
            }
        }

        //draw the meshes

        for (int i = 0, z = 0; z < zGridSize; z++)
        {
            for (int x = 0; x < xGridSize; x++)
            {
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

                uvs = new Vector2[vertices[i].Length];
                for (int iu = 0, zu = 0; zu <= tileSize; zu++)
                {
                    for (int xu = 0; xu <= tileSize; xu++)
                    {
                        uvs[iu] = new Vector2((float)(xu) / tileSize, (float)(zu) / tileSize);
                        iu++;
                    }
                }

                mesh[i].Clear();

                mesh[i].vertices = vertices[i];
                mesh[i].triangles = triangles;
                mesh[i].uv = uvs;

                childrenTiles[i].GetComponent<MeshRenderer>().material = terrainMaterial;

                mesh[i].RecalculateNormals();

                mesh[i].RecalculateBounds();
                MeshCollider meshCollider = childrenTiles[i].GetComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh[i];

                childrenTiles[i].transform.position += new Vector3(tileSize * x, 0, tileSize * z);

                i++;
            }
        }
    }
}
