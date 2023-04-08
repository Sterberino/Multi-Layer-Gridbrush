using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
[CustomGridBrush(false, true, false, "RandomMultiLayerBrush")]
public class RandomMultiLayerBrush : MultiLayerBrush
{
    public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        base.Paint(gridLayout, brushTarget, position);

        Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)Time.time.ToString().GetHashCode());
        int replacementIndex = random.NextInt(0, this.paintableObjects.Count);
        this.selectedObjectIndex = replacementIndex;

        ResetBrushProperties();
    }
}