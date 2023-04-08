using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Tilemaps;
using UnityEngine.Tilemaps;
using UnityEditor;


[CustomEditor(typeof(MultiLayerBrush), true)]
public class MultiLayerBrushEditor : GridBrushEditor
{
    public GameObject lastTarget;
    private GridBrushBase.Tool m_lastTool;
    private Vector3Int m_lastPos;
    public bool m_mouseInScene;

    //Is our left or right arrow key currently pressed (while alt is pressed)
    private bool leftArrowKeyDown = false;
    private bool rightArrowKeyDown = false;

    //A class for simply holding the parameter variables in the last function call of PaintPreview()
    private class LastPreviewTarget {
        public GridLayout gridLayout; 
        public GameObject brushTarget;
        public Vector3Int position;
    
        public LastPreviewTarget(GridLayout gridLayout,
        GameObject brushTarget,
        Vector3Int position)
        {
            this.gridLayout = gridLayout;
            this.position = position;
            this.brushTarget = brushTarget;
        }
    }
    private LastPreviewTarget lastPreviewTarget;

    private EditorWindow lastFocusedWindow;


    //Clear all preview tiles in all tilemaps
    public override void ClearPreview()
    {
        if (lastTarget == null) { return; }

        Tilemap[] tilemaps = lastTarget.transform.parent.GetComponentsInChildren<Tilemap>();
        if (tilemaps == null || tilemaps.Length <= 0)
        {
            return;
        }

        MultiLayerBrush mlb = brush as MultiLayerBrush;
        Vector2Int objectSize = mlb.GetSelectedObjectSize();

        for (int i = 0; i < tilemaps.Length; i++)
        {
            for (int x = m_lastPos.x - objectSize.x; x < m_lastPos.x + objectSize.x; x++)
            {
                for (int y = m_lastPos.y - objectSize.y; y < m_lastPos.y + objectSize.y; y++)
                {
                    Vector3Int item = new Vector3Int(x, y, 0);
                    ClearTilemapPreview(tilemaps[i], item);
                }
            }
        }

        /*
        MultiLayerBrush multiLayerBrush = target as MultiLayerBrush;
        Vector2Int size = multiLayerBrush.GetSelectedObjectSize();

        for (int i = 0; i < tilemaps.Length; i++)
        {
            for (int x = m_lastPos.x - size.x; x < m_lastPos.x + size.x; x++)
            {
                for (int y = m_lastPos.y - size.y; y < m_lastPos.y + size.y; y++)
                {
                    Vector3Int item = new Vector3Int(x, y, 0);
                    ClearTilemapPreview(tilemaps[i], item);
                }
            }
        }*/

    }

    //Do stuff when the tool is selected. Mostly when paint is selected and the last tool was select
    public override void OnToolActivated(GridBrushBase.Tool tool)
    {
        /*if(m_lastTool == GridBrush.Tool.Select && tool == GridBrush.Tool.Paint)
        {
           hasSelection = true;
        }
        else if (m_lastTool == GridBrush.Tool.Paint && tool == GridBrush.Tool.Select)
        {
            hasSelection = true;
        }
        else
        {
            hasSelection = false;
        }
        */

        MultiLayerBrush multiLayerBrush = brush as MultiLayerBrush;
        if (multiLayerBrush != null)
        {
            multiLayerBrush.SetActiveTool(tool);
        }

        base.OnToolActivated(tool);
    }


    //Clear the selection if the tool is the selection
    public override void OnToolDeactivated(GridBrushBase.Tool tool)
    {

        m_lastTool = tool;

        base.OnToolDeactivated(tool);
    }

    /*
    [ExecuteInEditMode]
    public override void OnSelectionSceneGUI(GridLayout gridLayout, GameObject brushTarget)
    {
        base.OnSelectionSceneGUI(gridLayout, brushTarget);
        SpriteRenderer sr = (brush as MultiLayerBrush).objectToPaint.GetComponentInChildren<SpriteRenderer>();
        Texture2D tex = AssetPreview.GetAssetPreview(sr.sprite);
        tex.filterMode = FilterMode.Point;
        Handles.Label(m_lastPos, tex);
    }*/


    public override void OnMouseEnter()
    {
        base.OnMouseEnter();
        //We only want to execute the remaining code if the area is the scene view, NOT the tile palette. 
        if (EditorWindow.mouseOverWindow.GetType() != typeof(UnityEditor.SceneView))
        {
            return;
        }
        lastFocusedWindow = EditorWindow.focusedWindow;

        UnityEditor.SceneView.FocusWindowIfItsOpen(typeof(UnityEditor.SceneView));

        m_mouseInScene = true;

        MultiLayerBrush multiLayerBrush = brush as MultiLayerBrush;

        if (multiLayerBrush.GetActiveTool() != GridBrushBase.Tool.Paint)
        {
            return;
        }

        if (multiLayerBrush.m_spriteRenderers == null || multiLayerBrush.m_previewObjects == null)
        {
            return;
        }

        for (int i = 0; i < multiLayerBrush.m_spriteRenderers.Count; i++)
        {
            if (multiLayerBrush.m_previewObjects[i] == null)
            {
                continue;
            }

            multiLayerBrush.m_previewObjects[i].SetActive(true);
        }
    }


    /*When our mouse leaves the scene, clear the preview of all preview tiles and any sprite renderers
    contained by our selected brush object.*/
    public override void OnMouseLeave()
    {
        base.OnMouseLeave();
        if(lastFocusedWindow != null && lastFocusedWindow.GetType() != typeof(UnityEditor.SceneView))
        {
            UnityEditor.EditorWindow.FocusWindowIfItsOpen(lastFocusedWindow.GetType());
        }

        m_mouseInScene = false;

        MultiLayerBrush multiLayerBrush = brush as MultiLayerBrush;

        if (multiLayerBrush.m_spriteRenderers == null || multiLayerBrush.m_previewObjects == null)
        {
            return;
        }

        for (int i = 0; i < multiLayerBrush.m_spriteRenderers.Count; i++)
        {
            if (multiLayerBrush.m_previewObjects[i] == null)
            {
                continue;
            }

            multiLayerBrush.m_previewObjects[i].SetActive(false);
        }

    }

    //Handles keyboard input for hotkey use.
    public void HandleKeyboard()
    {
        Event currentEvent = Event.current;
        bool canExecute = true;

        //If we are releasing the left or right arrows, we mark them as not pressed.
        if(currentEvent.type == EventType.KeyUp && currentEvent.keyCode == KeyCode.LeftArrow)
        {
            leftArrowKeyDown = false;
        }

        if (currentEvent.type == EventType.KeyUp && currentEvent.keyCode == KeyCode.RightArrow)
        {
            rightArrowKeyDown = false;
        }

        //Can we execute hotkey functionality?
        if (currentEvent.type == EventType.KeyDown && (rightArrowKeyDown || leftArrowKeyDown))
        {
            canExecute = false;
        }

        /*We still want to run throught this code even if not executing hotkeys because we want to avoid
        scrolling left and right in the scene view (currentEvent.use())*/
        if(currentEvent.type == EventType.KeyDown && currentEvent.alt)
        {
            switch (currentEvent.keyCode)
            {
                case KeyCode.LeftArrow:
                    if(canExecute)
                    {
                        MultiLayerBrush MLB = brush as MultiLayerBrush;
                        int index = MLB.selectedObjectIndex - 1;
                        ChangeSelectedObject(MLB, index);

                        leftArrowKeyDown = true;
                    }
                    currentEvent.Use();
                    
                    break;
                case KeyCode.RightArrow:
                    if(canExecute)
                    {
                        MultiLayerBrush MLB = brush as MultiLayerBrush;
                        int index = MLB.selectedObjectIndex + 1;
                        ChangeSelectedObject(MLB, index);
                        rightArrowKeyDown = true;
                    }
                    currentEvent.Use();
                    break;
                default:
                  
                    break;
            }
   
        }
    }

    //Change the selected prefab object to that of the selected index
    private void ChangeSelectedObject(MultiLayerBrush MLB, int index)
    {
        if (index < 0)
        {
            index = MLB.paintableObjects.Count - 1;
        }
        if(index > MLB.paintableObjects.Count - 1)
        {
            index = 0;
        }
        MLB.selectedObjectIndex = index;
        MLB.ResetBrushProperties();

        if (lastPreviewTarget != null)
        {
            this.ClearPreview();
            this.PaintPreview(lastPreviewTarget.gridLayout, lastPreviewTarget.brushTarget, lastPreviewTarget.position);
        }
    }

    

    //We get all of the sprite renderers in the object we wish to paint and we draw them to the scene relative to the object size and brush position.
    //The sprite renderer objects are contained by the brush and are not added to the scene or interactable, they're for visual aid only.
    public override void OnPaintSceneGUI(GridLayout gridLayout, GameObject brushTarget, BoundsInt position, GridBrushBase.Tool tool, bool executing)
    {
        //Handle keyboard input to check for hotkeys alt+rightArrow and alt+leftArrow, for toggling through paintable objects.
        HandleKeyboard();
        base.OnPaintSceneGUI(gridLayout, brushTarget, position, tool, executing);

        MultiLayerBrush multiLayerBrush = brush as MultiLayerBrush;

        if (multiLayerBrush.m_spriteRenderers == null || multiLayerBrush.m_previewObjects == null)
        {
            return;
        }


        for (int i = 0; i < multiLayerBrush.m_spriteRenderers.Count; i++)
        {
            if (multiLayerBrush.m_previewObjects[i] == null || multiLayerBrush.m_spriteRenderers[i] == null)
            {
                continue;
            }

            multiLayerBrush.m_previewObjects[i].SetActive(false);
        }

        if (tool == GridBrushBase.Tool.Paint && m_mouseInScene)
        {
            for (int i = 0; i < multiLayerBrush.m_spriteRenderers.Count; i++)
            {
                if (multiLayerBrush.m_previewObjects[i] == null || multiLayerBrush.m_spriteRenderers[i] == null)
                {
                    continue;
                }
                multiLayerBrush.m_previewObjects[i].SetActive(true);
                multiLayerBrush.m_previewObjects[i].transform.position = m_lastPos + multiLayerBrush.m_spriteRenderers[i].transform.position;
            }
        }

    }



    public override void PaintPreview(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        //We need to cache the function params in case we want to clear the preview and redraw upon hotkey press.
        lastPreviewTarget = new LastPreviewTarget(gridLayout, brushTarget, position);

        MultiLayerBrush multiLayerBrush = target as MultiLayerBrush;
        if (brushTarget == null || multiLayerBrush == null || multiLayerBrush.tilemapLayers == null || multiLayerBrush.tilemapLayers.Count <= 0)
        {
            return;
        }

        //We want to get the current brush target (presumably an object with a tilemap). Then we get all tilemaps parented to our target's parent. 
        //It is assumed that all tilemaps are parented to the same gameobject.
        Tilemap[] tilemaps = brushTarget.transform.parent.GetComponentsInChildren<Tilemap>();
        if (tilemaps == null || tilemaps.Length <= 0)
        {
            return;
        }

        //Sort in ascending order of the tilemaps' order in layer.
        Tilemap[] sortedTilemaps = multiLayerBrush.SortTilemaps(tilemaps);
        tilemaps = sortedTilemaps;

        //Record the tilemaps' state.
        Undo.RecordObjects(tilemaps, "tilemaps");

        //Iterate through the lists. In case there is a discrepancy between the number of tilemaps in the scene and in the object, we use this while loop
        int i = 0;
        while (i < tilemaps.Length && i < multiLayerBrush.tilemapLayers.Count)
        {
            for (int x = multiLayerBrush.tilemapLayers[i].cellBounds.xMin; x < multiLayerBrush.tilemapLayers[i].cellBounds.xMax; x++)
            {
                for (int y = multiLayerBrush.tilemapLayers[i].cellBounds.yMin; y < multiLayerBrush.tilemapLayers[i].cellBounds.yMax; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    if (multiLayerBrush.tilemapLayers[i].HasTile(pos))
                    {
                        GridBrush.BrushCell brushCell = brush.cells[0];
                        SetTilemapPreviewCell(tilemaps[i], pos + position, multiLayerBrush.tilemapLayers[i].GetTile(pos), brushCell.matrix, brushCell.color);
                    }
                }
            }

            i++;
        }

        //Cache the last target and brush position
        m_lastPos = position;
        lastTarget = brushTarget;
    }

    //Sets the preview tile for a single tilemap tile.
    private static void SetTilemapPreviewCell(Tilemap map, Vector3Int location, TileBase tile, Matrix4x4 transformMatrix, Color color)
    {
        if (!(map == null))
        {
            map.SetEditorPreviewTile(location, tile);
            map.SetEditorPreviewTransformMatrix(location, transformMatrix);
            map.SetEditorPreviewColor(location, color);
        }
    }

    //Clears the preview tile on a tilemap at the given position.
    private static void ClearTilemapPreview(Tilemap map, Vector3Int location)
    {
        if (!(map == null))
        {
            map.SetEditorPreviewTile(location, (TileBase)null);
            map.SetEditorPreviewTransformMatrix(location, Matrix4x4.identity);
            map.SetEditorPreviewColor(location, Color.white);
        }
    }


}