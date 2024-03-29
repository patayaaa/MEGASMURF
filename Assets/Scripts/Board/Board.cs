﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public enum Direction
{
    Up,
    Down,
    Left,
    Right
}

public class Board : MonoBehaviour
{

    public static Board Instance
    {
        get
        {
            if (Board.instance == null)
            {
                Board.instance = FindObjectOfType<Board>();
            }
            return Board.instance;
        }
        set
        {
            instance = value;
        }
    }

    public int Columns { get => columns; }
    public int Rows { get => rows; }

    private static Board instance;

    [Header("Dimensions")]
    [Range(1, 40f)] public float totalWidth = 20;
    [Range(1, 40f)] public float totalHeight = 20;
    [Range(2, 50)] private int columns = 15;
    [Range(2, 50)] private int rows = 15;
    public float tileDelay = 0.0000001f;

    [Header("Appear")]
    [SerializeField] private float appearSpeed = 10f;
    [SerializeField] private AnimationCurve appearCurve;
    private float appearProgress = 0f;
    private bool isAppearing = false;
    [SerializeField] private float unitAppearDelay = 0.1f;

    [Header("Level")]
    public List<Room> dungeon;
    [SerializeField] private Room debugRoom;
    //public float tilesOffset;

    public GameObject tilePrefab;

    public GameObject skillTree;

    public GameObject gameOverScreen;

    [HideInInspector]
    public Tile[,] tiles;
    private List<Tile> tileList = new List<Tile>();

    [HideInInspector]
    public Maestro maestro;

    public List<GameObject> environments;
    public List<Material> tileMaterials;

    private Room currentRoom;

    private int roomId;

    private GameObject currentEnvironment;
    private Material currentMaterial;

    private List<Tile> exitTiles;
    private List<Tile> spawnTiles;

    [HideInInspector]
    public List<Spawner> spawners;
    [HideInInspector]
    public List<ImmediateSpawner> immediateSpawners;

    private List<LevelElement> enemies = new List<LevelElement>();

    private void Update()
    {
        AppearUpdate();
    }

    public void ReinitBoard()
    {
        roomId = -1;
        exitTiles.Clear();
        spawnTiles.Clear();
        if (maestro != null)
        {
            Destroy(maestro.gameObject);
            maestro = null;
        }
        NextRoom();
    }

    public void InitializeBoard()
    {
        InitializeBoard(debugRoom);
    }

    public void InitializeBoard(Room room)
    {
        currentRoom = room;
        if (environments.Count > 0)
        {
            currentEnvironment = environments[0];
        }
        currentRoom.OrderElements();
        GenerateTiles();
        isAppearing = true;
        GenerateUnits();
    }

    public void InitializeEnvironment(GameObject environment)
    {
        if (environment != null)
        {
            if (currentEnvironment != null)
            {
                currentEnvironment.SetActive(false);
                currentEnvironment = environment;
                currentEnvironment.SetActive(true);
            }
            else
            {
                currentEnvironment = environment;
                currentEnvironment.SetActive(true);
            }
        }
    }

    public void InitializeMaterial(Material mat)
    {
        if (mat != null)
        {
            currentMaterial = mat;
        }
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public bool SpawnersActive()
    {
        foreach (Spawner s in spawners)
        {
            if (s.activeSpawn)
            {
                return true;
            }
        }
        foreach (ImmediateSpawner s in immediateSpawners)
        {
            if (s.activeSpawn)
            {
                return true;
            }
        }
        return false;
    }

    public void NextRoom()
    {
        if (roomId < environments.Count)
        {
            currentEnvironment = environments[roomId];
        }
        roomId++;
        if (roomId > 0)
        {
            skillTree.SetActive(true);
        }
        ClearRoom();
        if (roomId < dungeon.Count)
        {
            if (roomId < environments.Count)
            {
                InitializeEnvironment(environments[roomId]);
            }
            if(roomId < tileMaterials.Count)
            {
                InitializeMaterial(tileMaterials[roomId]);
            }
            InitializeBoard(dungeon[roomId]);
            BattleManager.Instance.ResetState();
            BattleManager.Instance.LightStart();
        }
        else
        {
            EndDungeon();
        }
    }

    public void NewSpawnersTurn()
    {
        foreach (Spawner s in spawners)
        {
            s.NewTurn();
        }
        foreach (ImmediateSpawner s in immediateSpawners)
        {
            s.NewTurn();
        }
    }

    public void ClearRoom()
    {
        GameManager.units = new List<Unit>();
        foreach (Tile t in exitTiles)
        {
            if (t.unit != null && (t.type == TileType.Ally || t.type == TileType.Obstacle))
            {
                GameManager.units.Add(t.unit);
                t.unit.UnspawnUnit();
            }
        }

        DestroyTiles();

        BattleManager.Instance.playerUnits[0].Clear();
        BattleManager.Instance.playerUnits[1].Clear();
    }

    public void EndDungeon()
    {

    }

    private void DestroyTiles()
    {
        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                if (tiles[i, j].unit != null && !GameManager.units.Contains(tiles[i, j].unit))
                {
                    tiles[i, j].unit.UnspawnUnit();
                }
                tiles[i, j].ResetAppeared();
                tiles[i, j].gameObject.SetActive(false);
            }
        }
    }

    private void GenerateTiles()
    {
        tiles = new Tile[columns, rows];
        spawnTiles = new List<Tile>();
        exitTiles = new List<Tile>();
        spawners = new List<Spawner>();
        tileList = new List<Tile>();
        immediateSpawners = new List<ImmediateSpawner>();
        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                float x = Utility.Interpolate(-totalWidth / 2, totalWidth / 2, 0, columns - 1, i);
                float z = Utility.Interpolate(-totalHeight / 2, totalHeight / 2, 0, rows - 1, j);

                Vector3 position = new Vector3(x, 0f, z);

                LevelElement tile = currentRoom.GetTile(i, j);
                if (tile)
                {
                    Tile newTile = PoolManager.Instance.GetEntityOfType(tile.GetType()) as Tile;
                    tileList.Add(newTile);
                    if (newTile != null)
                    {
                        newTile.gameObject.SetActive(true);
                        newTile.Coords = new Vector2Int(i, j);
                        newTile.transform.position = position;
                        tiles[i, j] = newTile;
                        tiles[i, j].MudAmount = 0;
                        if (tiles[i, j] is SpawnTile)
                        {
                            spawnTiles.Add(tiles[i, j]);
                        }
                        else if (tiles[i, j] is ExitTile)
                        {
                            ((ExitTile)tiles[i, j]).id = exitTiles.Count;
                            exitTiles.Add(tiles[i, j]);

                        }
                        if (currentMaterial != null)
                        {
                            tiles[i, j].SetMaterial(currentMaterial);
                        }
                        string name = "Tile (" + i + "," + j + ")";
                        newTile.gameObject.name = name;
                        newTile.transform.localScale = new Vector3(totalWidth / (columns - 1), totalWidth / (columns - 1), totalHeight / (rows - 1));

                        LevelElement levelElement = currentRoom.GetEntity(i, j);
                        if (levelElement)
                        {

                            if (levelElement is Enemy)
                            {
                                Enemy enemy = PoolManager.Instance.GetEntityOfType(levelElement.GetType()) as Enemy;
                                if (enemy != null)
                                {
                                    enemy.Regen();
                                    if (tiles[i, j] is ImmediateSpawner)
                                    {
                                        ((ImmediateSpawner)tiles[i, j]).spawnedType = enemy.UnitType;
                                        enemy.gameObject.SetActive(true);
                                        enemy.SpawnUnit(tiles[i, j]);
                                        immediateSpawners.Add((ImmediateSpawner)tiles[i, j]);
                                        enemies.Add(enemy);
                                    }
                                    else if (tiles[i, j] is Spawner)
                                    {
                                        ((Spawner)tiles[i, j]).spawnedType = enemy.UnitType;
                                        spawners.Add(((Spawner)tiles[i, j]));
                                    }
                                    else
                                    {
                                        enemy.gameObject.SetActive(true);
                                        enemy.SpawnUnit(tiles[i, j]);
                                        enemies.Add(enemy);
                                    }
                                }
                            }
                            else
                            {
                                LevelElement newEntity = PoolManager.Instance.GetEntityOfType(levelElement.GetType()) as LevelElement;

                                if (newEntity != null)
                                {
                                    newEntity.gameObject.SetActive(true);
                                    newEntity.transform.position = position;
                                }
                            }
                        }
                    }
                }
            }
        }
        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                tiles[i, j].CheckNeighbors();
            }
        }
    }

    private void AppearUpdate()
    {
        if (isAppearing)
        {
            appearProgress += Time.deltaTime * appearSpeed;
            float appear = appearCurve.Evaluate(appearProgress);
            int index = Mathf.FloorToInt(appear * tileList.Count);
            if (index < tileList.Count)
            {
                for (int i = 0; i < index; i++)
                {
                    tileList[i]?.Appear();
                }
            }

            if (appear >= 1)
            {
                isAppearing = false;
                for (int i = 0; i < tileList.Count; i++)
                {
                    tileList[i]?.Appear();
                }

                FinishedTiles();
            }
        }
    }

    private void FinishedTiles()
    {
        appearProgress = 0f;
        StartCoroutine(AppearLevelElements());
    }

    private IEnumerator AppearLevelElements()
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            enemies[i].Appear();
            yield return new WaitForSeconds(unitAppearDelay);
        }
    }

    private void OnDrawGizmosSelected()
    {
        for (int j = 0; j < rows; j++)
        {
            float z = Utility.Interpolate(-totalHeight / 2, totalHeight / 2, 0, rows - 1, j);
            float x = totalWidth / 2f;

            Vector3 from = new Vector3(-x, 0f, z);
            Vector3 to = new Vector3(x, 0f, z);
            Gizmos.DrawLine(from, to);
        }

        for (int i = 0; i < columns; i++)
        {
            float x = Utility.Interpolate(-totalWidth / 2, totalWidth / 2, 0, columns - 1, i);
            float z = totalHeight / 2f;

            Vector3 from = new Vector3(x, 0f, z);
            Vector3 to = new Vector3(x, 0f, -z);
            Gizmos.DrawLine(from, to);
        }
    }

    public void GenerateUnits()
    {
        if (maestro == null)
        {
            GameObject maestroObject = UnitFactory.Instance.CreateUnit(BaseUnitType.Maestro);
            if (maestroObject != null)
            {
                maestro = maestroObject.GetComponent<Maestro>();
                maestro.SpawnUnit(spawnTiles[0]);
            }
        }

        if (GameManager.units != null)
        {
            for (int i = 0; i < GameManager.units.Count; i++)
            {
                Unit u = GameManager.units[i];
                if (spawnTiles.Count > u.SpawnID)
                {
                    u.gameObject.SetActive(true);
                    u.SpawnUnit(spawnTiles[u.SpawnID]);
                }
            }
        }

        spawnTiles.Clear();
    }

    public Tile GetTile(int x, int y)
    {
        if (x < 0 || y < 0 || x > tiles.GetLength(0) - 1 || y > tiles.GetLength(1) - 1)
        {
            return null;
        }
        else
        {
            return tiles[x, y];
        }
    }

    public Tile[,] GetTiles()
    {
        return tiles;
    }

    public Tile GetTile(Vector2 v)
    {
        return GetTile((int)v.x, (int)v.y);
    }

    public List<Tile> GetTiles(List<Vector2> vectors)
    {
        List<Tile> tiles = new List<Tile>();
        foreach (Vector2 v in vectors)
        {
            Tile t = GetTile(v);
            if (t != null)
            {
                tiles.Add(t);
            }
        }
        return tiles;
    }

    public List<Tile> GetTilesInLine(Tile tile, Direction dir)
    {
        List<Tile> tiles = new List<Tile>();
        Vector2 v = tile.Coords;
        switch (dir)
        {
            case Direction.Down:
                v = new Vector2(v.x, v.y - 1);
                while (GetTile(v) != null)
                {
                    tiles.Add(GetTile(v));
                    v = new Vector2(v.x, v.y - 1);
                }
                break;
            case Direction.Up:
                v = new Vector2(v.x, v.y + 1);
                while (GetTile(v) != null)
                {
                    tiles.Add(GetTile(v));
                    v = new Vector2(v.x, v.y - 1);
                }
                break;
            case Direction.Left:
                v = new Vector2(v.x - 1, v.y);
                while (GetTile(v) != null)
                {
                    tiles.Add(GetTile(v));
                    v = new Vector2(v.x - 1, v.y);
                }
                break;
            case Direction.Right:
                v = new Vector2(v.x + 1, v.y);
                while (GetTile(v) != null)
                {
                    tiles.Add(GetTile(v));
                    v = new Vector2(v.x + 1, v.y);
                }
                break;
        }
        return tiles;
    }

    public List<Tile> GetTilesBetween(Tile t1, Tile t2, bool diagonales)
    {
        List<Tile> between = new List<Tile>();
        if (t1.Equals(t2) || !t1.IsInLine(t2))
        {
            return between;
        }
        if (t1.Coords.x == t2.Coords.x)
        {
            // Search Down
            if (t1.Coords.y > t2.Coords.y)
            {
                for (float i = t1.Coords.y - 1; i > t2.Coords.y && i >= 0; i--)
                {
                    between.Add(tiles[(int)(t1.Coords.x), (int)i]);
                }
            }
            // Search Up
            else
            {
                for (float i = t1.Coords.y + 1; i < t2.Coords.y && i < rows; i++)
                {
                    between.Add(tiles[(int)(t1.Coords.x), (int)i]);
                }
            }
        }
        else if (t1.Coords.y == t2.Coords.y)
        {
            // Search Left
            if (t1.Coords.x > t2.Coords.x)
            {
                for (float i = t1.Coords.x - 1; i > t2.Coords.x && i >= 0; i--)
                {
                    between.Add(tiles[(int)i, (int)(t1.Coords.y)]);
                }
            }
            // Search Right
            else
            {
                for (float i = t1.Coords.x + 1; i < t2.Coords.x && i < columns; i++)
                {
                    between.Add(tiles[(int)i, (int)(t1.Coords.y)]);
                }
            }
        }
        else if (diagonales)
        {
            if (t1.Coords.y > t2.Coords.y)
            {
                // Search Down Left
                if (t1.Coords.x > t2.Coords.x)
                {
                    for (float i = 1; t1.Coords.x - i > t2.Coords.x && t1.Coords.y - i > t2.Coords.y && t1.Coords.x - i >= 0 && t1.Coords.y - i >= 0; i++)
                    {
                        between.Add(tiles[(int)(t1.Coords.x - i), (int)(t1.Coords.y - i)]);
                    }
                }
                // Search Down Right
                if (t1.Coords.x < t2.Coords.x)
                {
                    for (float i = 1; t1.Coords.x + i < t2.Coords.x && t1.Coords.y - i > t2.Coords.y && t1.Coords.x + i < columns && t1.Coords.y - i >= 0; i++)
                    {
                        between.Add(tiles[(int)(t1.Coords.x + i), (int)(t1.Coords.y - i)]);
                    }
                }
            }
            else
            {
                // Search Up Left
                if (t1.Coords.x > t2.Coords.x)
                {
                    for (float i = 1; t1.Coords.x - i > t2.Coords.x && t1.Coords.y + i < t2.Coords.y && t1.Coords.x - i >= 0 && t1.Coords.y + i < rows; i++)
                    {
                        between.Add(tiles[(int)(t1.Coords.x - i), (int)(t1.Coords.y + i)]);
                    }
                }
                // Search Up Right
                {
                    for (float i = 1; t1.Coords.x + i < t2.Coords.x && t1.Coords.y + i < t2.Coords.y && t1.Coords.x + i < columns && t1.Coords.y + i < rows; i++)
                    {
                        between.Add(tiles[(int)(t1.Coords.x + i), (int)(t1.Coords.y + i)]);
                    }
                }
            }
        }
        return between;
    }
}
