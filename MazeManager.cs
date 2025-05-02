using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MazeManager : MonoBehaviour
{
    public const int NUMBER_OF_MAZES = 6; // A Cube has 6 sides, so each level will always be comprised of 6 dimensionally connected mazes

    public const string MAZE = "Maze";

    public const string SPAWN_SPACE = "Spawn_Space";
    public const string GOAL_SPACE = "Finish";
    public const string WALL_SPACE = "Wall_Space";
    public const string TRANSITION_SPACE = "Transition_Space";
    public const string FREE_SPACE = "Free_Space";

    private static bool DEBUGGER_FAIL_SAFE = true;

    [SerializeField]
    private bool mazeExists = false;

    [SerializeField]
    private GameObject MazeGO;
    [SerializeField]
    private GameObject WallGO;
    [SerializeField]
    private GameObject TransitionTileGO;
    [SerializeField]
    private GameObject GoalGO;
    [SerializeField]
    private GameObject backgroundGO;

    private static GameObject activeMaze;

    public static int[] mazeCenter = new int[2];

    public static Dictionary<GameObject, Dictionary<Vector3, GameObject>> masterMazeMap = new Dictionary<GameObject, Dictionary<Vector3, GameObject>>();
    public static Dictionary<GameObject, GameObject> transitionTilesMap = new Dictionary<GameObject, GameObject>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (mazeExists)
        {
            foreach (Transform childMaze in transform)
            {
                if (childMaze.gameObject.tag == MAZE)
                {
                    activeMaze = childMaze.gameObject;
                    break;
                }
            }
            MapMaze();
        }
    }

    public static string GetSpaceType(Vector3 coordinates)
    {
        string type = FREE_SPACE;

        if (masterMazeMap[activeMaze].ContainsKey(coordinates))
        {
            type = masterMazeMap[activeMaze][coordinates].tag;
        }

        return type;
    }

    public static bool AttemptMazeTransition(Vector3 coordinates)
    {
        bool success = false;

        if (masterMazeMap[activeMaze].ContainsKey(coordinates) 
            && masterMazeMap[activeMaze][coordinates].tag == TRANSITION_SPACE 
            && transitionTilesMap.ContainsKey(masterMazeMap[activeMaze][coordinates]))
        {
            activeMaze.SetActive(false);
            activeMaze = transitionTilesMap[masterMazeMap[activeMaze][coordinates]];
            activeMaze.SetActive(true);

            success = true;
        }

        return success;
    }

    public static float BuildMaze(int mazeSize = 5)
    {
        List<bool[,]> mazes = new List<bool[,]>();
        Dictionary<string, List<Vector3>> mazesMetadata = new Dictionary<string, List<Vector3>>();
        
        // Generate all mazes
        int i = 0;
        while (i < NUMBER_OF_MAZES)
        {
            bool[,] generatedMaze = GenerateMaze(mazeSize);

            if (i == 0)
            {
                mazes.Add(generatedMaze);

                List<int[]> cornerCoordinates = GetAllMazeCorners(generatedMaze);
                int[] spawnCoordinates = cornerCoordinates[UnityEngine.Random.Range(0, cornerCoordinates.Count)];
                mazesMetadata[SPAWN_SPACE] = new List<Vector3>() { new Vector3(spawnCoordinates[0], spawnCoordinates[1], i) };

                int[] transitionTileCoordinates = FindFurthestCorner(generatedMaze, spawnCoordinates, cornerCoordinates);
                mazesMetadata[TRANSITION_SPACE] = new List<Vector3>() { new Vector3(transitionTileCoordinates[0], transitionTileCoordinates[1], i) };
                i++;
            }
            else if (i != NUMBER_OF_MAZES - 1)
            {
                int[] prevMazeTransitionTileCoords = new int[] { (int) mazesMetadata[TRANSITION_SPACE][mazesMetadata[TRANSITION_SPACE].Count - 1].x, (int) mazesMetadata[TRANSITION_SPACE][mazesMetadata[TRANSITION_SPACE].Count - 1].y };

                if (!generatedMaze[prevMazeTransitionTileCoords[0], prevMazeTransitionTileCoords[1]])
                {
                    mazes.Add(generatedMaze);

                    List<int[]> cornerCoordinates = GetAllMazeCorners(generatedMaze);
                    mazesMetadata[TRANSITION_SPACE].Add(new Vector3(prevMazeTransitionTileCoords[0], prevMazeTransitionTileCoords[1], i));

                    int[] transitionTileCoordinates = FindFurthestCorner(generatedMaze, prevMazeTransitionTileCoords, cornerCoordinates);
                    mazesMetadata[TRANSITION_SPACE].Add(new Vector3(transitionTileCoordinates[0], transitionTileCoordinates[1], i));
                    i++;
                }
            }
            else
            {
                // Last Maze to Generate, so must handle Goal implementation
                int[] prevMazeTransitionTileCoords = new int[] { (int) mazesMetadata[TRANSITION_SPACE][mazesMetadata[TRANSITION_SPACE].Count - 1].x, (int) mazesMetadata[TRANSITION_SPACE][mazesMetadata[TRANSITION_SPACE].Count - 1].y };

                if (!generatedMaze[prevMazeTransitionTileCoords[0], prevMazeTransitionTileCoords[1]])
                {
                    mazes.Add(generatedMaze);

                    List<int[]> cornerCoordinates = GetAllMazeCorners(generatedMaze);
                    mazesMetadata[TRANSITION_SPACE].Add(new Vector3(prevMazeTransitionTileCoords[0], prevMazeTransitionTileCoords[1], i));

                    int[] goalCoordinates = FindFurthestCorner(generatedMaze, prevMazeTransitionTileCoords, cornerCoordinates);
                    mazesMetadata[GOAL_SPACE] = new List<Vector3>() { new Vector3(goalCoordinates[0], goalCoordinates[1], i) };
                    i++;
                }
            }
        }

        // Move player to spawn coordinates
        GameObject.FindGameObjectWithTag("Player").transform.position = new Vector3(mazesMetadata[SPAWN_SPACE][0].x, GameObject.FindGameObjectWithTag("Player").transform.position.y, mazesMetadata[SPAWN_SPACE][0].y);

        List<GameObject> mazeGameObjects = new List<GameObject>();
        // Instantiate Mazes with transition tiles
        for (i = 0; i < NUMBER_OF_MAZES; i++)
            mazeGameObjects.Add(InstantiateMaze(mazes[i], mazesMetadata[TRANSITION_SPACE].FindAll( (transitionTileVector) => (int) transitionTileVector.z == i)));

        // After getting last maze, add the goal and hide any mazes after the first one is created
        GameObject GoalGOInstance = GameObject.Find("MasterMaze").GetComponent<MazeManager>().GoalGO;
        Instantiate(GoalGOInstance, new Vector3(mazesMetadata[GOAL_SPACE][0].x, 0, mazesMetadata[GOAL_SPACE][0].y), GoalGOInstance.transform.rotation, mazeGameObjects[mazeGameObjects.Count - 1].transform);

        foreach(GameObject mazeGO in mazeGameObjects)
            mazeGO.SetActive(false);

        mazeCenter = new int[]{ mazeSize, mazeSize };
        mazeGameObjects[0].SetActive(true);
        activeMaze = mazeGameObjects[0];
        GameObject.Find("MasterMaze").GetComponent<MazeManager>().backgroundGO.transform.position = new Vector3(mazeSize, -0.5f, mazeSize);
        GameObject.Find("MasterMaze").GetComponent<MazeManager>().backgroundGO.transform.localScale = new Vector3(mazeSize * 0.20f, 1, mazeSize * 0.20f);
        GameObject.Find("MasterMaze").GetComponent<MazeManager>().MapMaze();

        return 5 * mazeSize * NUMBER_OF_MAZES + ((mazeSize / 20) * 60) + ((mazeSize / 25) * 60) + ((mazeSize / 30) * 60) + ((mazeSize / 35) * 60) + ((mazeSize / 40) * 60);
    }

    public static float BuildSingleLevelCustomMaze(int mazeSize = 101)
    {
        // Generate maze
        bool[,] generatedMaze = GenerateMaze(mazeSize);
        Dictionary<string, List<Vector3>> mazesMetadata = new Dictionary<string, List<Vector3>>();

        // Identify Spawn Space
        List<int[]> cornerCoordinates = GetAllMazeCorners(generatedMaze);
        int[] spawnCoordinates = cornerCoordinates[UnityEngine.Random.Range(0, cornerCoordinates.Count)];
        mazesMetadata[SPAWN_SPACE] = new List<Vector3>() { new Vector3(spawnCoordinates[0], spawnCoordinates[1], 0) };

        // Identify Goal Space
        int[] goalCoordinates = cornerCoordinates[UnityEngine.Random.Range(0, cornerCoordinates.Count)];
        mazesMetadata[GOAL_SPACE] = new List<Vector3>() { new Vector3(goalCoordinates[0], goalCoordinates[1], 0) };

        // Move player to spawn coordinates
        GameObject.FindGameObjectWithTag("Player").transform.position = new Vector3(mazesMetadata[SPAWN_SPACE][0].x, GameObject.FindGameObjectWithTag("Player").transform.position.y, mazesMetadata[SPAWN_SPACE][0].y);

        // Instantiate Mazes with transition tiles
        activeMaze = InstantiateMaze(generatedMaze, new List<Vector3>());

        // Add the goal game object
        GameObject GoalGOInstance = GameObject.Find("MasterMaze").GetComponent<MazeManager>().GoalGO;
        Instantiate(GoalGOInstance, new Vector3(mazesMetadata[GOAL_SPACE][0].x, 0, mazesMetadata[GOAL_SPACE][0].y), GoalGOInstance.transform.rotation, activeMaze.transform);

        mazeCenter = new int[]{ mazeSize, mazeSize };
        activeMaze.SetActive(true);
        GameObject.Find("MasterMaze").GetComponent<MazeManager>().backgroundGO.transform.position = new Vector3(mazeSize, -0.5f, mazeSize);
        GameObject.Find("MasterMaze").GetComponent<MazeManager>().backgroundGO.transform.localScale = new Vector3(mazeSize * 0.20f, 1, mazeSize * 0.20f);
        GameObject.Find("MasterMaze").GetComponent<MazeManager>().MapMaze();

        return -1;
    }

    private static GameObject InstantiateMaze(bool[,] maze, List<Vector3> transitionTiles)
    {
        GameObject MazeGOInstance = GameObject.Find("MasterMaze").GetComponent<MazeManager>().MazeGO;
        GameObject WallGOInstance = GameObject.Find("MasterMaze").GetComponent<MazeManager>().WallGO;
        GameObject TransitionTileGOInstance = GameObject.Find("MasterMaze").GetComponent<MazeManager>().TransitionTileGO;
        GameObject mazeGameObject = Instantiate(MazeGOInstance, GameObject.Find("MasterMaze").transform);

        for (int i = 0; i < maze.GetLength(0); i++)
            for (int j = 0; j < maze.GetLength(1); j++)
                if (maze[i, j])
                    Instantiate(WallGOInstance, new Vector3(i, 0, j), WallGOInstance.transform.rotation, mazeGameObject.transform);

        foreach(Vector3 transitionTile in transitionTiles)
            Instantiate(TransitionTileGOInstance, new Vector3(transitionTile.x, 0, transitionTile.y), TransitionTileGOInstance.transform.rotation, mazeGameObject.transform);

        return mazeGameObject;
    }

    private static int[] FindFurthestCorner(bool[,] maze, int[] startCoordinates, List<int[]> cornersCoordinates)
    {
        List<int[]> traversedCoords = new List<int[]>();
        List<int[]> intersections = new List<int[]>();
        List<string> visitedNodes = new List<string>();
        int[] furthestCorner = new int[] { -1, -1 };
        int[] currTile = new int[] { startCoordinates[0], startCoordinates[1] };
        int distanceFromStart = 0;
        int highestDistance = -1;
        bool moveAvailable = true;

        visitedNodes.Add("[" + currTile[0] + "," + currTile[1] + "]");
        traversedCoords.Add(currTile);

        do
        {
            // If we traversed back into a previously visited intersection, remove it
            if (intersections.Any(coord => coord.SequenceEqual(currTile)))
                intersections.RemoveAt(intersections.Count - 1);

            // Check if we are in a corner and add as new furthest corner if its distance is indeed bigger
            if (cornersCoordinates.Any(coord => coord.SequenceEqual(currTile)) && distanceFromStart > highestDistance)
            {
                furthestCorner = currTile.Clone() as int[];
                highestDistance = distanceFromStart;
            }

            // Go over all surrounding tiles and identify if more options are available
            List<int[]> validCoords = new List<int[]>();
            if (!maze[currTile[0], currTile[1] + 1] && !visitedNodes.Contains("[" + currTile[0] + "," + (currTile[1] + 1) + "]")) // Right
            {
                validCoords.Add(new int[] { currTile[0], currTile[1] + 1 });
            }
            if (!maze[currTile[0] - 1, currTile[1]] && !visitedNodes.Contains("[" + (currTile[0] - 1) + "," + currTile[1] + "]")) // Up
            {
                validCoords.Add(new int[] { currTile[0] - 1, currTile[1] });
            }
            if (!maze[currTile[0], currTile[1] - 1] && !visitedNodes.Contains("[" + currTile[0] + "," + (currTile[1] - 1) + "]")) // Left
            {
                validCoords.Add(new int[] { currTile[0], currTile[1] - 1 });
            }
            if (!maze[currTile[0] + 1, currTile[1]] && !visitedNodes.Contains("[" + (currTile[0] + 1) + "," + currTile[1] + "]")) // Down
            {
                validCoords.Add(new int[] { currTile[0] + 1, currTile[1] });
            }

            // If valid moves are available, then move; else go back as long as there are intersections available
            if (validCoords.Count != 0)
            {
                if (validCoords.Count > 1)
                {
                    intersections.Add(currTile);
                }

                moveAvailable = true;
                currTile = validCoords[0];
                traversedCoords.Add(currTile);
                visitedNodes.Add("[" + currTile[0] + "," + currTile[1] + "]");
                distanceFromStart++;
            }
            else
            {
                moveAvailable = false;
                distanceFromStart--;

                traversedCoords.RemoveAt(traversedCoords.Count - 1);
                currTile = traversedCoords[traversedCoords.Count - 1];
            }

        } while((intersections.Count > 0 || moveAvailable) && DEBUGGER_FAIL_SAFE);

        return furthestCorner;
    }

    private static List<int[]> GetAllMazeCorners(bool[,] maze)
    {
        List<int[]> coordinates = new List<int[]>();

        for (int i = 1; i < maze.GetLength(0) - 1; i++)
            for (int j = 1; j < maze.GetLength(1) - 1; j++)
            {
                if (!maze[i, j])
                {
                    List<bool> surroundingTiles = new List<bool>() { maze[i - 1, j], maze[i + 1, j], maze[i, j - 1], maze[i, j + 1]};

                    int wallCount = 0;
                    foreach(bool isWall in surroundingTiles)
                        if(isWall)
                            wallCount++;

                    if (wallCount == 3)
                        coordinates.Add(new int[] { i, j });
                }
            }

        return coordinates;
    }

    // Generates a maze boolean map using Eller's Algorithm
    // TRUE => IS_WALL
    // FALSE => IS_FREE_SPACE
    private static bool[,] GenerateMaze(int mazeSize)
    {
        Dictionary<int, List<int[]>> sets = new Dictionary<int, List<int[]>>();
        Dictionary<string, int> cellSetMapping = new Dictionary<string, int>();
        int trueMazeSize = mazeSize + (mazeSize - 1) + 2;

        // Create template waffle
        bool[,] maze = new bool[trueMazeSize, trueMazeSize];
        for (int i = 0; i < maze.GetLength(0); i++)
            for (int j = 0; j < maze.GetLength(1); j++)
                maze[i, j] = (i == 0 || i == maze.GetLength(0) - 1 || j == 0 || j == maze.GetLength(0) - 1) ? true : ((i % 2 == 1 && j % 2 == 1) ? false : true);

        // Identify all open cells idexes and initialize sets dictionary
        int groupId = 0;
        List<int> openCellsIdxs = new List<int>();
        for (int j = 1; j < maze.GetLength(1) - 1; j++)
        {
            if (j % 2 == 1)
            {
                openCellsIdxs.Add(j);

                groupId++;
                sets[groupId] = new List<int[]> { new int[] { 1, j } };
                cellSetMapping["[1," + j + "]"] = groupId;
            }
        }

        // Execute algorithm for all rows, except the last one
        for (int i = 0; i < openCellsIdxs.Count; i++)
        {
            // Skip normal procedure for last row
            if (i == openCellsIdxs.Count - 1)
                break;

            // Randomly converge horizontally
            for (int j = 1; j < openCellsIdxs.Count; j++)
            {
                int groupAId = cellSetMapping["[" + openCellsIdxs[i] + "," + openCellsIdxs[j - 1] + "]"];
                int groupBId = cellSetMapping["[" + openCellsIdxs[i] + "," + openCellsIdxs[j] + "]"];

                if (groupAId != groupBId)
                {
                    maze[openCellsIdxs[i], openCellsIdxs[j] - 1] = (UnityEngine.Random.Range(0, 2) == 1) ? true : false;

                    if (!maze[openCellsIdxs[i], openCellsIdxs[j] - 1])
                    {
                        foreach (int[] coordinate in sets[groupBId])
                            cellSetMapping["[" + coordinate[0] + "," + coordinate[1] + "]"] = groupAId;
                        sets[groupAId].AddRange(sets[groupBId]);
                        sets.Remove(groupBId);
                    }
                }
            }

            // Vertical collapse
            List<int> collapsedGroups = new List<int>();
            for (int j = 0; j < openCellsIdxs.Count; j++)
            {
                int currGroupId = cellSetMapping["[" + openCellsIdxs[i] + "," + openCellsIdxs[j] + "]"];

                // Ensure group collapses at least once
                if (!collapsedGroups.Contains(currGroupId))
                {
                    int[] randomCoordIdx = sets[currGroupId][UnityEngine.Random.Range(0, sets[currGroupId].Count)];
                    while (randomCoordIdx[0] != openCellsIdxs[i])
                        randomCoordIdx = sets[currGroupId][UnityEngine.Random.Range(0, sets[currGroupId].Count)];

                    maze[openCellsIdxs[i] + 1, randomCoordIdx[1]] = false;
                    sets[currGroupId].Add(new int[] { openCellsIdxs[i + 1], randomCoordIdx[1] });
                    cellSetMapping["[" + openCellsIdxs[i + 1] + "," + randomCoordIdx[1] + "]"] = currGroupId;
                    collapsedGroups.Add(currGroupId);
                }

                // Skip random collapse if already collapsed
                if (!maze[openCellsIdxs[i] + 1, openCellsIdxs[j]])
                    continue;

                // UnityEngine.Randomly collapse group cells vertically
                maze[openCellsIdxs[i] + 1, openCellsIdxs[j]] = (UnityEngine.Random.Range(0, 2) == 1) ? true : false;
                if (!maze[openCellsIdxs[i] + 1, openCellsIdxs[j]])
                {
                    sets[currGroupId].Add(new int[] { openCellsIdxs[i + 1], openCellsIdxs[j] });
                    cellSetMapping["[" + openCellsIdxs[i + 1] + "," + openCellsIdxs[j] + "]"] = currGroupId;
                }
                else
                {
                    int newGroupId = 1;
                    while (sets.ContainsKey(newGroupId))
                        newGroupId++;

                    sets[newGroupId] = new List<int[]>() { new int[] { openCellsIdxs[i + 1], openCellsIdxs[j] } };
                    cellSetMapping["[" + openCellsIdxs[i + 1] + "," + openCellsIdxs[j] + "]"] = newGroupId;
                }
            }
        }

        // Handle last row - join any adjacent, but historically disjoint, cells
        for (int j = 1; j < openCellsIdxs.Count; j++)
        {
            int groupAId = cellSetMapping["[" + openCellsIdxs[openCellsIdxs.Count - 1] + "," + openCellsIdxs[j - 1] + "]"];
            int groupBId = cellSetMapping["[" + openCellsIdxs[openCellsIdxs.Count - 1] + "," + openCellsIdxs[j] + "]"];

            if (groupAId != groupBId)
            {
                maze[openCellsIdxs[openCellsIdxs.Count - 1], openCellsIdxs[j] - 1] = false;

                foreach (int[] coordinate in sets[groupBId])
                    cellSetMapping["[" + coordinate[0] + "," + coordinate[1] + "]"] = groupAId;
                sets[groupAId].AddRange(sets[groupBId]);
                sets.Remove(groupBId);
            }
        }

        return maze;
    }

    /* Disjointed Eller's Algorithm Maze Maker */
    //public static void BuildMaze()
    //{
    //    int mazeSize = 5;
    //
    //    Dictionary<int, int> sets = new Dictionary<int, int>();
    //    Dictionary<int, int> jointsHistory = new Dictionary<int, int>();
    //    int trueMazeSize = mazeSize + (mazeSize - 1) + 2;
    //
    //    // TRUE => IS_WALL
    //    // FALSE => IS_FREE_SPACE
    //
    //    // Create template waffle
    //    bool[,] maze = new bool[trueMazeSize, trueMazeSize];
    //    for (int i = 0; i < maze.GetLength(0); i++)
    //        for (int j = 0; j < maze.GetLength(1); j++)
    //            maze[i, j] = (i == 0 || i == maze.GetLength(0) - 1 || j == 0 || j == maze.GetLength(0) - 1) ? true : ((i % 2 == 1 && j % 2 == 1) ? false : true);
    //
    //    // Identify all open cells idexes and initialize sets dictionary
    //    int groupId = 0;
    //    List<int> openCellsIdxs = new List<int>();
    //    for (int j = 1; j < maze.GetLength(1) - 1; j++)
    //    {
    //        if (j % 2 == 1)
    //        {
    //            openCellsIdxs.Add(j);
    //
    //            groupId++;
    //            sets[j] = groupId;
    //        }
    //    }
    //
    //    Dictionary<int, int> newSets = new Dictionary<int, int>();
    //
    //    // 
    //    for (int i = 0; i < openCellsIdxs.Count; i++)
    //    {
    //        // Skip normal procedure for last row
    //        if (i == openCellsIdxs.Count - 1)
    //            break;
    //
    //        newSets[openCellsIdxs[0]] = sets[openCellsIdxs[0]];
    //
    //        // Randomly converge horizontally
    //        for (int j = 1; j < openCellsIdxs.Count; j++)
    //        {
    //            newSets[openCellsIdxs[j]] = sets[openCellsIdxs[j]];
    //
    //            if (sets[openCellsIdxs[j]] != sets[openCellsIdxs[j - 1]])
    //            {
    //                maze[openCellsIdxs[i], openCellsIdxs[j] - 1] = (Random.Range(0, 2) == 1) ? true : false;
    //                
    //                if(!maze[openCellsIdxs[i], openCellsIdxs[j] - 1])
    //                {
    //                    newSets[openCellsIdxs[j]] = newSets[openCellsIdxs[j - 1]];
    //                    jointsHistory[openCellsIdxs[j]] = openCellsIdxs[j - 1];
    //                }
    //            }
    //        }
    //
    //        sets.Clear();
    //        sets = new Dictionary<int, int>(newSets);
    //        newSets.Clear();
    //
    //        Dictionary<int, int>  horizontalJoints = new Dictionary<int, int>(jointsHistory);
    //        int prevGroupId = sets[openCellsIdxs[0]];
    //        int prevGroupStartIdx = 0;
    //        bool prevGroupCollapsed = false;
    //
    //        // Vertical collapse
    //        for (int j = 0; j < openCellsIdxs.Count; j++)
    //        {
    //            int currGroupId = sets[openCellsIdxs[j]];
    //
    //            // Safety net for any groups that did not collapse
    //            // And update of related variables
    //            if(currGroupId != prevGroupId)
    //            {
    //                if(!prevGroupCollapsed)
    //                {
    //                    int randomColIdx = openCellsIdxs[Random.Range(prevGroupStartIdx, j)];
    //                    maze[openCellsIdxs[i] + 1, randomColIdx] = false;
    //                    newSets[randomColIdx] = sets[randomColIdx];
    //
    //                    if (horizontalJoints.ContainsKey(randomColIdx))
    //                        jointsHistory[randomColIdx] = randomColIdx - 2;
    //                }
    //
    //                prevGroupStartIdx = j;
    //                prevGroupId = currGroupId;
    //                prevGroupCollapsed = false;
    //            }
    //
    //            // Randomly collapse group cells vertically
    //            maze[openCellsIdxs[i] + 1, openCellsIdxs[j]] = (Random.Range(0, 2) == 1) ? true : false;
    //            if(!maze[openCellsIdxs[i] + 1, openCellsIdxs[j]])
    //            {
    //                prevGroupCollapsed = true;
    //                newSets[openCellsIdxs[j]] = sets[openCellsIdxs[j]];
    //
    //                //if (j != 0 && maze[openCellsIdxs[i] + 1, openCellsIdxs[j - 1]])
    //                //    jointsHistory.Remove(openCellsIdxs[j]);
    //            }
    //            else
    //            {
    //                int newGroupId = (10000 * (i + 1)) + i;
    //                while (sets.ContainsValue(newGroupId) || newSets.ContainsValue(newGroupId))
    //                    newGroupId++;
    //
    //                newSets[openCellsIdxs[j]] = newGroupId;
    //
    //                if (jointsHistory.ContainsKey(openCellsIdxs[j]))
    //                    jointsHistory.Remove(openCellsIdxs[j]);
    //            }
    //        }
    //
    //        // Safety net for last group since it doesn't execute inside loop
    //        if (!prevGroupCollapsed)
    //        {
    //            int randomColIdx = openCellsIdxs[Random.Range(prevGroupStartIdx, openCellsIdxs.Count)];
    //            maze[openCellsIdxs[i] + 1, randomColIdx] = false;
    //            newSets[randomColIdx] = sets[randomColIdx];
    //
    //            if (horizontalJoints.ContainsKey(randomColIdx))
    //                jointsHistory[randomColIdx] = randomColIdx - 2;
    //        }
    //
    //        sets.Clear();
    //        sets = new Dictionary<int, int>(newSets);
    //        newSets.Clear();
    //    }
    //
    //
    //    // Handle last row - join any adjacent, but historically disjoint, cells
    //    for (int j = 1; j < openCellsIdxs.Count; j++)
    //        if ((j == 1 && maze[openCellsIdxs[openCellsIdxs.Count - 1] - 1, openCellsIdxs[0]]) 
    //            || (!jointsHistory.ContainsKey(openCellsIdxs[j]) && sets[openCellsIdxs[j]] != sets[openCellsIdxs[j - 1]]))
    //            maze[openCellsIdxs[openCellsIdxs.Count - 1], openCellsIdxs[j] - 1] = false;
    //
    //    string mazeString = "";
    //    for (int i = 0; i < maze.GetLength(0); i++)
    //    {
    //        for (int j = 0; j < maze.GetLength(1); j++)
    //            mazeString += maze[i, j] ? "1\t" : "0\t";
    //        mazeString += "\n";
    //    }
    //
    //    Debug.Log(mazeString);
    //
    //    //MapMaze();
    //    return;
    //}

    private void MapMaze()
    {
        // Map all sub-mazes
        foreach(Transform childMaze in transform)
        {
            if (childMaze.gameObject.tag == MAZE)
            {
                Dictionary<Vector3, GameObject> mazeMap = new Dictionary<Vector3, GameObject>();
                foreach(Transform child in childMaze)
                {
                    mazeMap[child.position] = child.gameObject;
                }
                masterMazeMap[childMaze.gameObject] = mazeMap;
            }
        }

        // Map all sub-mazes' transition tiles to their sibling tile's maze
        int idx = 0;
        GameObject prevMaze = null;
        foreach(Transform childMaze in transform)
        {
            if (idx == 0)
            {
                prevMaze = childMaze.gameObject;
                idx++;
                continue;
            }

            foreach (Transform child in childMaze)
            {
                if (child.gameObject.tag == TRANSITION_SPACE 
                    && masterMazeMap[prevMaze].ContainsKey(child.gameObject.transform.position)
                    && masterMazeMap[prevMaze][child.gameObject.transform.position].tag == TRANSITION_SPACE)
                {
                    transitionTilesMap[child.gameObject] = prevMaze;
                    transitionTilesMap[masterMazeMap[prevMaze][child.gameObject.transform.position]] = childMaze.gameObject;
                }
            }

            prevMaze = childMaze.gameObject;
        }
    }
}
