using System;
using System.Collections.Generic;
using AwesomeTechnologies.VegetationStudio;
using AwesomeTechnologies.VegetationSystem;
using UnityEditor;
using UnityEngine;

namespace DPS
{
#if UNITY_EDITOR
    public class DpsVSPImporter : MonoBehaviour
    {
        [Header("Set your terrain alone in a layer ")]
        public LayerMask TerrainLayer;
        [Header("-1 is ALL")]
        public int ImportOnlyTreeIndex = -1;
        public int ImportOnlyDetailIndex = -1;
        [Header("rng scale for trees")] 
        public bool UseRngTreeScaling;
        public Vector3 TreesRngMin = Vector3.one;
        public Vector3 TreesRngMax = Vector3.one;
        [Space]
        [Header("-will scale globally the scale for the asset/assets imported")]
        public float ScaleMp = 1;
        public bool GrassToTerrainNormal;

        public BiomeType TargetBiomeType = BiomeType.Default; 
        [Header("Link Terrain here")]
        public Terrain TerrainToImportFrom;
        [Tooltip("Read the identifier Int in the PersistentVegetationStorageTools.cs")]
        internal int ByteIdIdentifier = 0;


        private TerrainData _terrainData;
        private TreeInstance[] _treeInstances;
        public List<string> AddedIds = new List<string>();
        [ContextMenu("GetTerrainTrees")]
        public void GetData()
        {
            _terrainData = TerrainToImportFrom.terrainData;
            _treeInstances = _terrainData.treeInstances;
            

            float width = _terrainData.size.x;
            float height = _terrainData.size.z;
            float y = _terrainData.size.y;

            AddedIds.Clear();

            for (int i = 0; i < _terrainData.treePrototypes.Length; i++)
            {
                
                

                var p = AssetDatabase.GetAssetPath(_terrainData.treePrototypes[i].prefab);
                Debug.Log(p);
                string z = AssetDatabase.AssetPathToGUID(p);
                Debug.Log(z);
                var id = VegetationStudioManager.GetVegetationItemID(z);

                
                if(string.IsNullOrEmpty(id))
                {
                    id = VegetationStudioManager.AddVegetationItem(_terrainData.treePrototypes[i].prefab, VegetationType.Tree, false, TargetBiomeType);
                }
                AddedIds.Add(id);


                if(ImportOnlyTreeIndex > -1 && ImportOnlyTreeIndex != i){continue;} //skip if a specific index only is wanted
                if (!string.IsNullOrEmpty(id))
                {
                    VegetationStudioManager.RemoveVegetationItemInstances(id);
                }
            }

            for (int i = 0; i < _treeInstances.Length; i++)
            {
                var tree = _treeInstances[i];
                var treeprefab = _terrainData.treePrototypes[tree.prototypeIndex].prefab;
                if(ImportOnlyTreeIndex > -1 && ImportOnlyTreeIndex != tree.prototypeIndex){continue;} //skip if a specific index only is wanted
                var id = AddedIds[tree.prototypeIndex];
                //VegetationStudioManager.RemoveVegetationItemInstances(id);
                Vector3 position = new Vector3(
                                       tree.position.x * width,
                                       tree.position.y * y,
                                       tree.position.z * height) + TerrainToImportFrom.transform.position;


                Vector3 terraintreescale = new Vector3(tree.widthScale, tree.heightScale, tree.widthScale);
                Vector3 prefabscale = new Vector3(treeprefab.transform.localScale.x, treeprefab.transform.localScale.y, treeprefab.transform.localScale.z);
                Vector3 finalscale = Vector3.Scale(terraintreescale, prefabscale);


                Debug.Log("adding instance for " + tree.prototypeIndex.ToString() + " guid: " + id);
                VegetationStudioManager.AddVegetationItemInstance(id, position, finalscale * ScaleMp,
                    Quaternion.Euler(0f, Mathf.Rad2Deg * tree.rotation, 0f), true, (byte)ByteIdIdentifier, 1);

            }
        }

        [ContextMenu("GetTerrainDetailGrass")]
        public void GetDataDetails()
        {
            _terrainData = TerrainToImportFrom.terrainData;
            var details = _terrainData.detailPrototypes;




            AddedIds.Clear();

            for (int i = 0; i < details.Length; i++)
            {
                if(ImportOnlyDetailIndex > -1 && ImportOnlyDetailIndex != i){continue;} //skip if a specific index only is wanted
                var detail = details[i];

                
                string id = string.Empty;
                if (detail.usePrototypeMesh)
                {

                    var p = AssetDatabase.GetAssetPath(detail.prototype);
                    Debug.Log(p);
                    string z = AssetDatabase.AssetPathToGUID(p);
                    Debug.Log(z);
                    id = VegetationStudioManager.GetVegetationItemID(z);
                    if(string.IsNullOrEmpty(id))
                    {
                        id = VegetationStudioManager.AddVegetationItem(detail.prototype,VegetationType.Grass,false, TargetBiomeType);
                    }

                }
                else
                {
                    var p = AssetDatabase.GetAssetPath(detail.prototypeTexture);
                    Debug.Log(p);
                    string z = AssetDatabase.AssetPathToGUID(p);
                    Debug.Log(z);
                    id = VegetationStudioManager.GetVegetationItemID(z);
                    if(string.IsNullOrEmpty(id))
                    {
                        id = (VegetationStudioManager.AddVegetationItem(detail.prototypeTexture, VegetationType.Grass,false, TargetBiomeType));
                    }
                }
                AddedIds.Add(id);

                if (!string.IsNullOrEmpty(id))
                {
                    VegetationStudioManager.RemoveVegetationItemInstances(id);
                }

                // i will be the etail index (layer)
                int[,] map = _terrainData.GetDetailLayer(0, 0, _terrainData.detailWidth, _terrainData.detailHeight, i);

                var detailSizeX = _terrainData.detailResolution / _terrainData.size.x;
                var detailSizeZ = _terrainData.detailResolution / _terrainData.size.z;

                for (int y = 0; y < _terrainData.detailHeight; y++) //go trough vertical of the terrain texture
                {
                    for (int x = 0; x < _terrainData.detailWidth; x++) //go trough X
                    {
                        if (map[x, y] > 0)
                        {
                            //Debug.Log(y + " " + " " + x + " " + map[x, y]);

                            //generate an rng pos based on density
                            for (int j = 0; j < map[x, y]; j++)
                            {
                                var microPos = TerrainToImportFrom.transform.position;
                                microPos.x = y / detailSizeX + UnityEngine.Random.value + TerrainToImportFrom.transform.position.x;
                                microPos.z = x / detailSizeZ + UnityEngine.Random.value + TerrainToImportFrom.transform.position.z;


                                //accuracy of simple sample height is low, can use a raycast
                                microPos.y = TerrainToImportFrom.SampleHeight(microPos) +
                                             TerrainToImportFrom.transform.position.y;

                                var rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 359), 0);

                                //will raycast from above 1m down in order to align to terrain, may produce ugly results in certain cases
                                if (GrassToTerrainNormal)
                                {
                                    RaycastHit hit = new RaycastHit();
                                    Ray ray = new Ray(microPos + Vector3.up, Vector3.down);


                                    if (Physics.Raycast(ray, out hit, 2, TerrainLayer, QueryTriggerInteraction.Ignore))
                                    {
                                        microPos.y = hit.point.y;
                                        //Debug.Log("hit: " + hit.point);


                                        rotation = Quaternion.LookRotation(Vector3.Cross(hit.normal, Vector3.right));
                                        //Debug.Log("norm: "+hit.normal);
                                    }
                                }

                                var scaleXZ = UnityEngine.Random.Range(detail.minWidth, detail.maxWidth);
                                var scaleY = UnityEngine.Random.Range(detail.minHeight, detail.maxHeight);

                                Debug.Log("Te" + microPos.ToString());
                                VegetationStudioManager.AddVegetationItemInstance(id,
                                    microPos, 
                                    new Vector3(scaleXZ, scaleY, scaleXZ) * ScaleMp, 
                                    rotation, 
                                    true,
                                    (byte) ByteIdIdentifier, 1);

                            }
                        }

                    }
                }
            }


        }
    }
#endif
}