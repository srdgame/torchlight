using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class TorchLightLevelRandomGenerater
{
    static int DIRECTION_N = 0x0001;
    static int DIRECTION_E = 0x0010;
    static int DIRECTION_S = 0x0100;
    static int DIRECTION_W = 0x1000;

    int GetChunkDirection(LevelChunk Chunk)
    {
        if (Chunk.ChunkName.EndsWith("_N")) return DIRECTION_N;
        if (Chunk.ChunkName.EndsWith("_S")) return DIRECTION_S;
        if (Chunk.ChunkName.EndsWith("_W")) return DIRECTION_W;
        if (Chunk.ChunkName.EndsWith("_E")) return DIRECTION_E;
        if (Chunk.ChunkName.EndsWith("EW")) return (DIRECTION_E | DIRECTION_W);
        if (Chunk.ChunkName.EndsWith("NS")) return (DIRECTION_N | DIRECTION_S);
        if (Chunk.ChunkName.EndsWith("NE")) return (DIRECTION_N | DIRECTION_E);
        if (Chunk.ChunkName.EndsWith("NW")) return (DIRECTION_N | DIRECTION_W);
        if (Chunk.ChunkName.EndsWith("SE")) return (DIRECTION_S | DIRECTION_E);
        if (Chunk.ChunkName.EndsWith("SW")) return (DIRECTION_S | DIRECTION_W);
        Debug.LogError("Chunk Direction Error : " + Chunk.ChunkName);
        return 0;
    }

    int GetOppsiteDirection(int Dir)
    {
        if (Dir == DIRECTION_E) return DIRECTION_W;
        if (Dir == DIRECTION_W) return DIRECTION_E;
        if (Dir == DIRECTION_N) return DIRECTION_S;
        if (Dir == DIRECTION_S) return DIRECTION_N;
        return 0;
    }

    public Vector3 GetDirectionOffset(int Dir)
    {
        Vector3 Offset = Vector3.zero;

        if ((Dir & DIRECTION_N) != 0) Offset.z = -1.0f;
        if ((Dir & DIRECTION_S) != 0) Offset.z = 1.0f;
        if ((Dir & DIRECTION_W) != 0) Offset.x = 1.0f;
        if ((Dir & DIRECTION_E) != 0) Offset.x = -1.0f;

        return Offset;
    }

    public Vector3 GetDirectionOffset(LevelChunk Chunk)
    {
        return GetDirectionOffset(GetChunkDirection(Chunk));
    }

    LevelBuildInfo CurLevelInfo = null;

    public class LevelChunk
    {
        public string       ChunkName;
        public Vector3      Offset = Vector3.zero;
        public List<string> SceneNames = new List<string>();
    }

    public class LevelBuildInfo
    {
        public string LevelName;
        public string DisplayName;

        public int ChunkWidthOffset = 0;
        public int ChunkHeightOffset = 0;

        public string BackgroundMusic;
        public Color AmbientColor = Color.white;
        public Color DirectionLightColor = Color.white;
        public Color FogColor = Color.white;
        public float FogBegin = 0.0f;
        public float FogEnd = 100.0f;

        public Vector3 DirectionLightDir = new Vector3(-1.0f, -1.0f, 1.0f);
        public int MinChunkNum = 0;
        public int MaxChunkNum = 1;

        public List<LevelChunk> LevelChunks = new List<LevelChunk>();
    }

    public bool LoadLevelRuleFile(string LevelRuleFile)
    {
        StreamReader Reader = EditorTools.GetStreamReaderFromAsset(LevelRuleFile);

        CurLevelInfo = new LevelBuildInfo();
        while (!Reader.EndOfStream)
        {
            string Line = Reader.ReadLine().Trim();

            string Tag = "", Value = "";
            EditorTools.ParseTag(Line, ref Tag, ref Value);

            if (Tag == "LEVELNAME")         CurLevelInfo.LevelName              = Value;
            if (Tag == "BGMUSIC")           CurLevelInfo.BackgroundMusic        = Value;
            if (Tag == "AMBIENT")           CurLevelInfo.AmbientColor           = EditorTools.ParseColor(Value);
            if (Tag == "DIRECTION_COLOR")   CurLevelInfo.DirectionLightColor    = EditorTools.ParseColor(Value);
            if (Tag == "DIRECTION_DIR")     CurLevelInfo.DirectionLightDir      = EditorTools.ParseVector3(Value);
            if (Tag == "FOG_COLOR")         CurLevelInfo.FogColor               = EditorTools.ParseColor(Value);
            if (Tag == "FOG_BEGIN")         CurLevelInfo.FogBegin               = float.Parse(Value);
            if (Tag == "FOG_END")           CurLevelInfo.FogEnd                 = float.Parse(Value);
            if (Tag == "MINCHUNK")          CurLevelInfo.MinChunkNum            = int.Parse(Value);
            if (Tag == "MAXCHUNK")          CurLevelInfo.MaxChunkNum            = int.Parse(Value);

            if (Line == "[CHUNKTYPE]")
            {
                LevelChunk Chunk = new LevelChunk();
                while (Line != "[/CHUNKTYPE]")
                {
                    EditorTools.ParseTag(Line, ref Tag, ref Value);
                    if (Tag == "CHUNK_NAME") Chunk.ChunkName = Value;
                    if (Tag == "CHUNK_FILE") Chunk.SceneNames.Add(Value);

                    Line = Reader.ReadLine().Trim();
                }

                CurLevelInfo.LevelChunks.Add(Chunk);
            }
        }

        Reader.Close();
        return true;
    }

    public List<LevelChunk> BuildLevel()
    {
        if (CurLevelInfo == null) return new List<LevelChunk>();

        List<LevelChunk>    ChunkLists  = new List<LevelChunk>();
        if (CurLevelInfo.LevelChunks.Count == 1)
        {
            ChunkLists.Add(CurLevelInfo.LevelChunks[0]);
        }
        else
        {
            int ChunkNum = Random.Range(CurLevelInfo.MinChunkNum, CurLevelInfo.MaxChunkNum + 1);

            ChunkNum = Mathf.Min(ChunkNum, TorchLightConfig.TorchLightStartaChunkNum_MAX - 2);

            List<LevelChunk> EnteranceChunks    = new List<LevelChunk>();
            List<LevelChunk> ExitChunks         = new List<LevelChunk>();
            List<LevelChunk> LinkChunks         = new List<LevelChunk>();

            foreach (LevelChunk Chunk in CurLevelInfo.LevelChunks)
            {
                if (Chunk.ChunkName.IndexOf("ENTRANCE") != -1)
                    EnteranceChunks.Add(Chunk);
                else if (Chunk.ChunkName.IndexOf("EXIT") != -1)
                    ExitChunks.Add(Chunk);
                else if (Chunk.ChunkName.IndexOf("ROOM") == -1)
                    LinkChunks.Add(Chunk);
            }

            LevelChunk EnteranceChunk = EnteranceChunks[Random.Range(0, EnteranceChunks.Count)];
            ChunkLists.Add(EnteranceChunk);

            if (ChunkNum == 0)
            {
                LevelChunk ExitChunk = null;
                foreach (LevelChunk Chunk in ExitChunks)
                {
                    int ExitDir = GetOppsiteDirection(GetChunkDirection(EnteranceChunk));
                    if (ExitDir == GetChunkDirection(Chunk))
                    {
                        ExitChunk = Chunk;
                        break;
                    }
                }
                ExitChunk.Offset = GetDirectionOffset(ChunkLists[0]);
                ChunkLists.Add(ExitChunk);
            }
            else
            {
                Vector3 Offset = Vector3.zero;

                HashSet<LevelChunk> UsedChunks = new HashSet<LevelChunk>();
                int CurNeedDirection = GetOppsiteDirection(GetChunkDirection(EnteranceChunk));
                while(ChunkNum != 0)
                {
                    int NextChunkDir = 0;
                    foreach (LevelChunk Chunk in LinkChunks)
                    {
                        NextChunkDir = GetChunkDirection(Chunk);
                        if ((NextChunkDir & CurNeedDirection) != 0 && !UsedChunks.Contains(Chunk))
                        {
                            Offset += GetDirectionOffset(GetOppsiteDirection(CurNeedDirection));
                            Chunk.Offset = Offset;

                            UsedChunks.Add(Chunk);
                            ChunkLists.Add(Chunk);
                            CurNeedDirection = GetOppsiteDirection(NextChunkDir & (~CurNeedDirection));
                            break;
                        }
                    }
                    ChunkNum--;
                }

                foreach(LevelChunk Chunk in ExitChunks)
                {
                    int NextChunkDir = GetChunkDirection(Chunk);
                    if ((CurNeedDirection & NextChunkDir) != 0)
                    {
                        Offset += GetDirectionOffset(GetOppsiteDirection(CurNeedDirection));
                        Chunk.Offset = Offset;
                        ChunkLists.Add(Chunk);
                        break;
                    }
                }
            }
        }

        return ChunkLists;
    }

    public void LoadLevelRuleFileToScene(FStrata Strata, bool SplitToSubScene, string RelateSavePath)
    {
		string RuleFilePath = Strata.RuleSet;
        if (LoadLevelRuleFile(RuleFilePath))
        {
            string Prefix = RelateSavePath.Replace('/', '-');
            string ScenePath = TorchLightConfig.TorchLightSceneFolder + RelateSavePath;
			
            List<TorchLightLevelRandomGenerater.LevelChunk> LevelChunks = BuildLevel();

            // if we spliting the scene into subscenes, we need a full scene for navmesh building
            // here we save a full scene for navmesh, and the resource is cached for later subscenes.
            if (SplitToSubScene)
            {
                EditorApplication.NewScene();
                System.GC.Collect();
                {
                    // Set Global Render Settings, directional light, fog etc.
                    SetGlobalRenderSetting(Strata, false);

                    // Here we create a Gameobject to hold subScene infos for addtion async loading
                    // for Unity Appilcation.LoadLevelXXX, the parameter LevelName is only the .unity file's Name NOT include the path
                    GameObject SubSceneObj = new GameObject("SubSceneInfos");
                    SubSceneInfo Info = SubSceneObj.AddComponent<SubSceneInfo>();
                    for (int i = 0; i < LevelChunks.Count; i++)
                        Info.AllSubScenes.Add(Prefix + "SubScene-" + i);

                    // Minimap GameObject if for Minimap builder
                    GameObject Minimap = new GameObject("Minimap");
                    GameObject CamObj = new GameObject("Camera");
                    {
                        CamObj.transform.parent = Minimap.transform;
                        CamObj.AddComponent<GUILayer>();
                        Camera Cam = CamObj.GetComponent<Camera>();
                        {
                            Cam.backgroundColor = Color.black;
                            Cam.transform.position = new Vector3(0.0f, 100.0f, 0.0f);
                            Cam.transform.rotation = Quaternion.Euler(new Vector3(90.0f, 0.0f, 0.0f));
                            Cam.isOrthoGraphic = true;
                            Cam.orthographicSize = 120.0f;
                        }
                    }

                    GameObject Chunks = new GameObject("Chunks");
                    {
                        Chunks.AddComponent<LevelChunkShower>();
                        Chunks.transform.parent = Minimap.transform;
                    }

                    // if we split to subscenes, we need a fully scene to build navmesh
                    foreach (TorchLightLevelRandomGenerater.LevelChunk Chunk in LevelChunks)
                    {
                        // Load Objects into Scene
                        string Path = TorchLightConfig.TorchLightConvertedLayoutFolder + Chunk.SceneNames[0];
                        GameObject Level = TorchLightLevelBuilder.LoadLevelLayoutToScene(Path);
                        Level.transform.position = Chunk.Offset * 100.0f;

                        Level.SetActiveRecursively(true);

                        // Add a Node to the Minimap GameObject
                        GameObject MinimapNode = new GameObject(Chunk.SceneNames[0]);
                        {
                            MinimapNode.transform.position = Level.transform.position;
                            MinimapNode.transform.parent = Chunks.transform;
                        }
                    }
                }
                // Assets/Scenes/DungeonName/StartaName/DungoneName-StartaName.unity
                EditorApplication.SaveScene(ScenePath + Prefix.Substring(0, Prefix.Length - 1) + ".unity");
            }

            int SubSceneIndex = 0;
            foreach (TorchLightLevelRandomGenerater.LevelChunk Chunk in LevelChunks)
            {
				// Create a new Scene
                if (SplitToSubScene)
                {
                    EditorApplication.NewScene();
                    System.GC.Collect();
                }
				
				// Load Objects into Scene
                string Path = TorchLightConfig.TorchLightConvertedLayoutFolder + Chunk.SceneNames[0];
                GameObject Level = TorchLightLevelBuilder.LoadLevelLayoutToScene(Path);
                Level.transform.position = Chunk.Offset * 100.0f;

                Level.SetActiveRecursively(true);
				
				// Set Global Render Settings, directional light, fog etc.
                SetGlobalRenderSetting(Strata, (SubSceneIndex == 0 && !SplitToSubScene) || SplitToSubScene);
				
				// Here we create a Gameobject to hold subScene infos for addtion async loading
				// for Unity Appilcation.LoadLevelXXX, the parameter LevelName is only the .unity file's Name NOT include the path
                if (SubSceneIndex == 0 && SplitToSubScene)
                {
                    GameObject SubSceneObj = new GameObject("SubSceneInfos");
                    SubSceneInfo Info   = SubSceneObj.AddComponent<SubSceneInfo>();

                    // Add Other subscene into load list, we START from the second subscene.
                    for (int i = 1; i < LevelChunks.Count; i++ )
                        Info.AllSubScenes.Add(Prefix + "SubScene-" + i);
                    // handly set diasble, here the SubSceneInfo is just for test.
                    SubSceneObj.SetActiveRecursively(false);
                }
				
				// Save this scene to .unity file
				// Assets/Scenes/DungeonName/StartaName/DungoneName-StartaName-SubScene-N.unity
				if (SplitToSubScene)
					EditorApplication.SaveScene(ScenePath + Prefix + "SubScene-" + SubSceneIndex + ".unity");
				
				SubSceneIndex++;
            }

            if (SplitToSubScene)
            {
                EditorApplication.NewScene();
                System.GC.Collect();
            }
        }
    }

	public void SetGlobalRenderSetting(FStrata Strata, bool CreateDirectionLight)
	{
        RenderSettings.ambientLight = Strata.AmbientColor;
        RenderSettings.fogColor = Strata.FogColor;
        RenderSettings.fogStartDistance = Strata.FogStart;
        RenderSettings.fogEndDistance = Strata.FogEnd;

        if (CreateDirectionLight)
        {
            GameObject LightObj = new GameObject("DirectionalLight");
            Light ALight = LightObj.AddComponent<Light>();
            ALight.type = LightType.Directional;
            ALight.color = Strata.LightColor;
            ALight.intensity = 0.2f;
            ALight.transform.rotation = Quaternion.Euler(new Vector3(50.0f, -30.0f, 0.0f));
        }
		
		Object.DestroyImmediate(Camera.mainCamera.gameObject);
	}
}
