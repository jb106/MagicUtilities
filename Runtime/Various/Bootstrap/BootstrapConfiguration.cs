using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MagicUtilities/Bootstrap Configuration")]
public class BootstrapConfig : ScriptableObject
{
    // Prefabs spawned once at launch, kept across scenes
    public List<GameObject> prefabs = new();
}