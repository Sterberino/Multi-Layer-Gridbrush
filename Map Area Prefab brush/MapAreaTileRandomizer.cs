using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
public class MapAreaTileRandomizer : MonoBehaviour, IMapAreaRandomizer
{
    public bool Test;

    public List<TileBase> defaultTiles;

    [System.Serializable]
    public struct TileReplacements
    {
        public List<TileBase> replacements;
    }

    public List<TileReplacements> tileReplacements;
    public List<Tilemap> tilemapsInMapArea;

    public void Randomize()
    {
        if(tilemapsInMapArea == null || tilemapsInMapArea.Count == 0)
        {
            Tilemap[] tm = transform.root.gameObject.GetComponentsInChildren<Tilemap>();

            tilemapsInMapArea = new List<Tilemap>(tm);
        }

        foreach(TileReplacements replacementList in tileReplacements)
        {
            if(replacementList.replacements.Count != defaultTiles.Count)
            {
                Debug.LogError("A tiles replacement list does not match the size of the default tiles list. Could not replace tiles.");
                return;
            }
        }

        Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)Time.time.ToString().GetHashCode());
        int replacementIndex = random.NextInt(0, tileReplacements.Count);

        //Select a set of replacement tiles. 
        List<TileBase> selectedReplacementTiles = tileReplacements[replacementIndex].replacements; 
        
        //Create a hashset and initialize it.
        HashSet<TileBase> tileHashset = new HashSet<TileBase>();
        foreach(TileBase tile in defaultTiles)
        {
            tileHashset.Add(tile);
        }

        for(int i = 0; i < tilemapsInMapArea.Count; i++)
        {
            tilemapsInMapArea[i].CompressBounds();

            BoundsInt bounds = tilemapsInMapArea[i].cellBounds;
            TileBase[] tiles = tilemapsInMapArea[i].GetTilesBlock(bounds);

            int tilesReplaced = 0;
            for(int j = 0; j < tiles.Length; j++)
            {
                //If we find a tile in our tilemap that matches one of the default tiles that we replace,
                //then we get the index of that tile and use that index to get the proper replacement for our selected replacement list.
                if (tileHashset.Contains(tiles[j]))
                {
                    int index = GetTileIndex(tiles[j]);
                    tiles[j] = selectedReplacementTiles[index];

                    tilesReplaced++;
                }
            }
            tilemapsInMapArea[i].SetTilesBlock(bounds ,tiles);
        }
    }

    private int GetTileIndex(TileBase tile)
    {
        for(int i = 0; i  < defaultTiles.Count; i++)
        {
            if(tile == defaultTiles[i])
            {
                return i;
            }
        }

        return -1;
    }

    private void OnValidate()
    {
        if(Test)
        {
            Test = false;
            Randomize();

        }
    }
}
