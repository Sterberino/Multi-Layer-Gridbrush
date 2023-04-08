using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.Tilemaps;

[CreateAssetMenu]
[CustomGridBrush(false, true, false, "MultiLayerBrush")]
public class MultiLayerBrush : UnityEditor.Tilemaps.GridBrush
{
    [ReadOnly]
    public GameObject objectToPaint;

    public bool randomizeIfAble;

    [ReadOnly]
    [Tooltip("The size of the largest tilemap bounds")]
    [SerializeField]
    private Vector2Int objectSize;

    private GameObject previouslyCachedObject;

    public int selectedObjectIndex;
    public List<GameObject> paintableObjects;

    [HideInInspector]
    public List<Tilemap> tilemapLayers;

    [HideInInspector]
    public List<GameObject> m_previewObjects;
    [HideInInspector]
    public List<SpriteRenderer> m_spriteRenderers;

    private GridBrushBase.Tool m_ActiveTool;

    public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        if(!SceneView.lastActiveSceneView.in2DMode)
        {
            Debug.LogWarning("Your sceneview is not in 2D mode. Object placement may not function properly.");
        }

        //Get all the tilemaps parented to the brush targets transform and sort them
        Tilemap[] tilemaps = brushTarget.transform.parent.GetComponentsInChildren<Tilemap>();
        if (tilemaps == null || tilemaps.Length <= 0)
        {
            return;
        }

        Tilemap[] sortedTilemaps = SortTilemaps(tilemaps);
        tilemaps = sortedTilemaps;
        Undo.RecordObjects(tilemaps, "tilemaps");

        Collider2D[] tilemapColliders = brushTarget.transform.parent.GetComponentsInChildren<Collider2D>();
      
        if (tilemapColliders.Length > 0)
        {
            Undo.RecordObjects(tilemapColliders, "tilemap colliders");
        }

        //We are going to drop in our prefab and unpack it only, so all children objects that are nested prefabs remain prefabs.
        GameObject objectToPaintPrefabInstance = PrefabUtility.InstantiatePrefab(objectToPaint) as GameObject;
        PrefabUtility.UnpackPrefabInstance(objectToPaintPrefabInstance, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);

        if (randomizeIfAble)
        {
            //If there is a randomization script somewhere in our map area prefab, get it and randomize the prefab before placing
            IMapAreaRandomizer[] randomizers = objectToPaintPrefabInstance.GetComponentsInChildren<IMapAreaRandomizer>();
            foreach (IMapAreaRandomizer randomizer in randomizers)
            {
                randomizer.Randomize();
            }
        }
       
        Tilemap [] localTilemapLayers = objectToPaintPrefabInstance.GetComponentsInChildren<Tilemap>();

        int i = 0;
        while (i < tilemaps.Length && i < localTilemapLayers.Length)
        {
            //Get tile block and set tile block will go here and replace this logic for improving performance

            for (int x = localTilemapLayers[i].cellBounds.xMin; x < localTilemapLayers[i].cellBounds.xMax; x++)
            {
                for (int y = localTilemapLayers[i].cellBounds.yMin; y < localTilemapLayers[i].cellBounds.yMax; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    if (localTilemapLayers[i].HasTile(pos))
                    {
                        TileBase tile = localTilemapLayers[i].GetTile(pos);
                        tilemaps[i].SetTile(pos + position, tile);
                    }
                }
            }

            i++;
        }

        for (i = 0; i < tilemaps.Length; i++)
        {
            tilemaps[i].CompressBounds();
        }

        TilemapCollider2D collider = brushTarget.GetComponentInChildren<TilemapCollider2D>();
        if (collider != null)
        {
            collider.ProcessTilemapChanges();
        }

        Vector3 mousePosition = Event.current.mousePosition;
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        mousePosition = new Vector3(ray.origin.x, ray.origin.y, 0);

        List<Transform> tilemapTransforms = new List<Transform>();
        for (i = 0; i < tilemapLayers.Count; i++)
        {
            tilemapTransforms.Add(tilemapLayers[i].transform);
        }

        HashSet<GameObject> instantiatedObjects = new HashSet<GameObject>();

        Transform[] transforms = objectToPaintPrefabInstance.GetComponentsInChildren<Transform>();
        foreach (Transform t in transforms)
        {
            //The only transform with no parent is the prefab instance. If t is null, we've deleted it.
            if (t.parent == null || instantiatedObjects.Contains(t.gameObject) || t == null)
            {
                continue;
            }

            //Delete the tilemaps present in the prefab instance, we set the main scene tilemap tiles equal to these tiles at the brush location.
            if (t.GetComponent<Tilemap>())
            {
                DestroyImmediate(t.gameObject, false);
                continue;
            }

            //Unparent it from prefab instance
            t.parent = null;

            //Set the object's position in the scene and move it to bottom of scene hierarchy.
            Vector3 vec = mousePosition;
            int x = vec.x < 0 ? Mathf.FloorToInt(vec.x) : (int)vec.x;
            int y = vec.y < 0 ? Mathf.FloorToInt(vec.y) : (int)vec.y;
            t.position += new Vector3Int(x, y, 0);
            t.SetAsLastSibling();

            GetAllGameObjectsRecursive(t.gameObject, instantiatedObjects);
            Undo.RegisterCreatedObjectUndo(t.gameObject, "gameObject");

        }

        //Destroy the prefab instance we created of our brush prefab object, since we have painted all of its tiles and unparented all its chldren.
        DestroyImmediate(objectToPaintPrefabInstance, false);
    }

    private void GetAllGameObjectsRecursive(GameObject g, HashSet<GameObject> hashSet)
    {
        if (!hashSet.Contains(g))
        {
            hashSet.Add(g);
        }
        else
        {
            return;
        }

        foreach (Transform t in g.GetComponentsInChildren<Transform>())
        {
            if (hashSet.Contains(t.gameObject))
            {
                continue;
            }
            else
            {
                hashSet.Add(t.gameObject);
                GetAllGameObjectsRecursive(t.gameObject, hashSet);
            }

        }
    }

    public void SetActiveTool(GridBrushBase.Tool tool)
    {
        m_ActiveTool = tool;
    }

    public GridBrushBase.Tool GetActiveTool()
    {
        return m_ActiveTool;
    }

    public override void BoxErase(GridLayout gridLayout, GameObject brushTarget, BoundsInt position)
    {
        Tilemap[] tilemaps = brushTarget.transform.parent.GetComponentsInChildren<Tilemap>();
        if (tilemaps == null || tilemaps.Length <= 0)
        {
            Debug.Log("No tilemaps found");
            return;
        }


        for (int i = 0; i < tilemaps.Length; i++)
        {
            for (int x = position.xMin; x < position.xMax; x++)
            {
                for (int y = position.yMin; y < position.yMax; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    tilemaps[i].SetTile(pos, null);

                }
            }
        }

        for (int i = 0; i < tilemaps.Length; i++)
        {
            tilemaps[i].CompressBounds();
        }

        TilemapCollider2D collider = brushTarget.GetComponentInChildren<TilemapCollider2D>();
        if (collider != null)
        {
            collider.ProcessTilemapChanges();
        }

    }

    public override void Erase(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {

        Tilemap[] tilemaps = brushTarget.transform.parent.GetComponentsInChildren<Tilemap>();
        if (tilemaps == null || tilemaps.Length <= 0)
        {
            Debug.Log("No tilemaps found");
            return;
        }
        Undo.RecordObjects(tilemaps, "tilemaps");

        Vector3Int min = position - pivot;
        BoundsInt bounds = new BoundsInt(min, size);

        BoxErase(gridLayout, brushTarget, bounds);

    }

    public void DeactivatePreviewObjects(GridBrushBase newBrush)
    {
        if (newBrush != this)
        {
            if (m_spriteRenderers == null || m_previewObjects == null)
            {
                return;
            }

            for (int i = 0; i < m_spriteRenderers.Count; i++)
            {
                if (m_previewObjects[i] == null)
                {
                    continue;
                }

                m_previewObjects[i].SetActive(false);
            }
        }
    }


    public void OnValidate()
    {
        ResetBrushProperties();
    }

    public void ResetBrushProperties()
    {
        GridPaintingState.brushChanged -= DeactivatePreviewObjects;
        GridPaintingState.brushChanged += DeactivatePreviewObjects;

        if (selectedObjectIndex >= paintableObjects.Count || selectedObjectIndex < 0)
        {
            selectedObjectIndex = 0;
            return;
        }

        if (paintableObjects.Count == 0)
        {
            Debug.LogError("Cannot select paintable object because list of paintable objects is empty.");
            return;
        }

        objectToPaint = paintableObjects[selectedObjectIndex];

        if (previouslyCachedObject != objectToPaint)
        {
            previouslyCachedObject = objectToPaint;

            Tilemap[] tilemaps = previouslyCachedObject.GetComponentsInChildren<Tilemap>();
            Tilemap[] sortedTilemaps = SortTilemaps(tilemaps);
            tilemapLayers.Clear();

            if (sortedTilemaps != null)
            {
                for (int i = 0; i < sortedTilemaps.Length; i++)
                {
                    tilemapLayers.Add(sortedTilemaps[i]);
                }
            }

            objectSize = SetSelectedObjectSize();

            if (m_previewObjects == null)
            {
                m_previewObjects = new List<GameObject>();
            }
            if (m_spriteRenderers == null)
            {
                m_spriteRenderers = new List<SpriteRenderer>();
            }


            UnityEditor.EditorApplication.delayCall += () =>
            {
                //Destroy preview objects and clear lists.
                for (int i = 0; i < m_previewObjects.Count; i++)
                {

                    if (m_previewObjects[i] != null)
                    {
                        DestroyImmediate(m_previewObjects[i]);
                    }
                }
            };

            UnityEditor.EditorApplication.delayCall += () =>
            {
                m_previewObjects.Clear();
                m_spriteRenderers.Clear();

                SpriteRenderer[] renderers = objectToPaint.GetComponentsInChildren<SpriteRenderer>();

                for (int i = 0; i < renderers.Length; i++)
                {
                    
                    //Add the sprite renderer to cached list of sprite renderers in the prefab object
                    m_spriteRenderers.Add(renderers[i]);

                    //Create an object which will not be saved to scene or visible in hierarchy, or set scene as dirty.
                    GameObject go = EditorUtility.CreateGameObjectWithHideFlags(
                        "Sprite Preview " + i,
                        HideFlags.HideAndDontSave);

                    //Add a sprite renderer to the gameobject and copy the current renderers values to it.
                    SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                    EditorUtility.CopySerialized(renderers[i], sr);

                    go.SetActive(false);
                    //Add the gameobject to list of preview objects.
                    m_previewObjects.Add(go);
                }

            };


        }
    }

    /// <summary>
    /// Sorts an array of tilemaps by sorting order in ascending order. Returns the sorted array as a new array.
    /// </summary>
    public Tilemap[] SortTilemaps(Tilemap[] originalTilemaps)
    {
        if (originalTilemaps.Length > 0)
        {

            Tilemap[] sortedTilemaps = originalTilemaps;

            foreach (Tilemap t in originalTilemaps)
            {
                int sortingOrder = t.GetComponent<TilemapRenderer>().sortingOrder;
                sortedTilemaps[sortingOrder] = t;
            }

            return sortedTilemaps;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Sorts a list of tilemaps by sorting order in ascending order. Returns the list as an array.
    /// </summary>
    public Tilemap[] SortTilemaps(List<Tilemap> originalTilemaps)
    {
        if (originalTilemaps.Count > 0)
        {

            Tilemap[] sortedTilemaps = new Tilemap[originalTilemaps.Count];

            foreach (Tilemap t in originalTilemaps)
            {
                int sortingOrder = t.GetComponent<TilemapRenderer>().sortingOrder;
                sortedTilemaps[sortingOrder] = t;
            }

            return sortedTilemaps;
        }
        else
        {
            return null;
        }
    }

    public Vector2Int GetSelectedObjectSize()
    {
        return objectSize;
    }

    public Vector2Int SetSelectedObjectSize()
    {
        Vector2Int vec = new Vector2Int(0, 0);

        for(int i = 0; i < tilemapLayers.Count; i++)
        {
            if (tilemapLayers[i].cellBounds.size.x > vec.x)
            {
                vec.x = tilemapLayers[i].cellBounds.size.x;
            }
            if (tilemapLayers[i].cellBounds.size.y > vec.y)
            {
                vec.y = tilemapLayers[i].cellBounds.size.y;
            }
        }

        return vec;
    }

    //Should work?
    /*
    public override void Pick(GridLayout gridLayout, GameObject brushTarget, BoundsInt position, Vector3Int pickStart)
    {
        Reset();
        
        if (brushTarget == null)
            return;
        Tilemap [] tilemaps = brushTarget.transform.parent.GetComponentsInChildren<Tilemap>();
        if (tilemaps == null)
        {
            return;
        }
        tilemaps = SortTilemaps(tilemaps);
        UpdateMultiBrushSizeAndPivot(new Vector3Int(position.size.x, position.size.y, 1), new Vector3Int(pickStart.x, pickStart.y, 0), tilemaps.Length);
        
        for(int i = 0; i < tilemaps.Length; i++)
        {
            foreach (Vector3Int pos in position.allPositionsWithin)
            {
                Vector3Int brushPosition = new Vector3Int(pos.x - position.x, pos.y - position.y, 0);
                BrushCell cell = new BrushCell();
                cell = PickCell(cell, pos, brushPosition, tilemaps[i]);
                m_cells[i][GetCellIndex(brushPosition)] = cell;
            }
        }
   
    }
    private BrushCell PickCell(BrushCell brushCell,Vector3Int position, Vector3Int brushPosition, Tilemap tilemap)
    {
        if (tilemap == null)
            return null;
        if(ValidateCellPosition(position))
        {
            brushCell.color = tilemap.GetColor(position);
            brushCell.tile = tilemap.GetTile(position);
            brushCell.matrix = tilemap.GetTransformMatrix(position);
            return brushCell;
        }
        else
        {
            return null;
        }
        
    }
    private bool ValidateCellPosition(Vector3Int position)
    {
        var valid =
            position.x >= 0 && position.x < size.x &&
            position.y >= 0 && position.y < size.y &&
            position.z >= 0 && position.z < size.z;
        if (!valid)
            throw new ArgumentException(string.Format("Position {0} is an invalid cell position. Valid range is between [{1}, {2}).", position, Vector3Int.zero, size));
        return valid;
    }
    
    
    new public void Reset()
    {
        UpdateMultiBrushSizeAndPivot(Vector3Int.one, Vector3Int.zero, 0);
    }
    public void UpdateMultiBrushSizeAndPivot(Vector3Int size, Vector3Int pivot, int numTilemaps)
    {
        m_size = size;
        m_pivot = pivot;
        SizeUpdated(numTilemaps);
    }
    public void SizeUpdated(int numTilemaps)
    {
        
        m_cells = new List<BrushCell[]>(numTilemaps);
        if (numTilemaps == 0) { return; }
        for(int i = 0; i < numTilemaps; i++)
        {
            BrushCell[] arr = new BrushCell[m_size.x * m_size.y * m_size.z];
            m_cells.Add(arr);
            BoundsInt bounds = new BoundsInt(Vector3Int.zero, m_size);
            foreach (Vector3Int pos in bounds.allPositionsWithin)
            {
                m_cells[i][GetCellIndex(pos)] = new BrushCell();
            }
        }
    
    }
     */
}