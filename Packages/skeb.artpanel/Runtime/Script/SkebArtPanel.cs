#region
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Image;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
#endif
#endregion

namespace skeb.skebartpanel
{
    public enum eMode
    {
        [InspectorName("Client")] Client,
        [InspectorName("Creator")] Creator,
        [InspectorName("Random")] Random
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SkebArtPanel : UdonSharpBehaviour
    {
        private void DebugLog(string msg = "", string color = "yellow", string title = "SkebArtPanel")
        {
            Debug.Log($"[<color={color}>{title}</color>]{msg}");
        }

        #region Serialized Variables
        public MeshRenderer mesh;

        /// <summary>
        /// GUI用
        /// </summary>
        public bool isCustomMaterial;

        /// <summary>
        /// パネル番号
        /// </summary>
        public int panel_index = 0;

        /// <summary>
        /// ダウンロードしたイラストを入れるMaterial
        /// </summary>
        public Material mat_profile;

        /// <summary>
        /// 次にリロードする時間
        /// </summary>
        public int interval = 1;

        /// <summary>
        /// エディタ用
        /// </summary>
        public eMode mode;

        /// <summary>
        /// 　SkebのID
        /// </summary>
        public string screen_name = "hoge";
        #endregion

        #region Variables
        /// <summary>
        /// イラストをダウンロードする際の設定
        /// </summary>
        TextureInfo info;

        /// <summary>
        /// イメージダウンロード用
        /// </summary>
        VRCImageDownloader downloader = new VRCImageDownloader();

        /// <summary>
        /// 60個urlを格納
        /// </summary>
        public VRCUrl[] url_profiles = new VRCUrl[60];

        /// <summary>
        /// ダウンロードしたイメージを受け取るUdon
        /// </summary>
        UdonBehaviour TargetUdon;
        #endregion

        #region Unity Functions
        private void OnEnable()
        {
            //OnEnabledで発火するとエラーが出るので初期化だけ
            downloader = new VRCImageDownloader();
                    
            DebugLog("OnEnable");
        }

        private void Start()
        {
            ReloadImageLoop();
        }

#if UNITY_EDITOR
        private void OnDestroy()
        {
            if (downloader != null)
                downloader.Dispose();

            if (mat_profile != null)
                mat_profile.SetTexture("_MainTex", null);

            DebugLog("OnDestroy");
        }
#endif
        #endregion

        #region Func
        private int GetIndex()
        {
            DateTime time = DateTime.Now;
            return time.Minute / interval;
        }
        private void DownloadImage(VRCUrl url, Material mat)
        {
            if (url == null)
            {
                DebugLog("Url is invalid.", "red");
                return;
            }

            if (mat == null)
            {
                DebugLog("Material is invalid.", "red");
                return;
            }

            if (downloader == null)
                downloader = new VRCImageDownloader();

            if (TargetUdon == null)
                TargetUdon = GetComponent<UdonBehaviour>();

            downloader.DownloadImage(url, mat, TargetUdon, info);

            DebugLog($"Downloading Image -> {url}");
        }
        public void ReloadImageLoop()
        {
            if (mat_profile == null)
                return;

            float _interval = 60 * interval;
            int index = GetIndex();

            DebugLog($"Reloading image now. interval is {interval}min.");

            //画像をダウンロード
            VRCUrl url = url_profiles[index];
            DownloadImage(url, mat_profile);

            //_interval 秒後リロード
            SendCustomEventDelayedSeconds(nameof(ReloadImageLoop), _interval, VRC.Udon.Common.Enums.EventTiming.Update);
        }
        #endregion

        #region VRChat Function
        public override void OnImageLoadSuccess(IVRCImageDownload result)
        {
            DebugLog($"Image successfully downloaded.");
        }

        public override void OnImageLoadError(IVRCImageDownload result)
        {
            DebugLog($"OnImageLoadError", "red");
            DebugLog($"{result.ErrorMessage}", "red");
        }
        #endregion

#if UNITY_EDITOR && !COMPILER_UDONSHARP

        [CustomEditor(typeof(SkebArtPanel))]
        public class SkebArtPanel_Editor : Editor
        {
            #region variables
            //eRole _role = eRole.Client;
           
            /// <summary>
            /// 10パターン用のマテリアルのGUID
            /// </summary>
            string[] guids = new string[] { 
                "02716fbb94d80ae4c88f868b73908b00", "b557273905391dc45b68cea1b35a4a40", 
                "97566aeeaee35914e913384b42a1a193", "5205e0ba4aa7b44409bcd1136057627f",
                "dd52660c231bc9249bb480182764e449", "770e70378e776384995d8dacadb96caf",
                "da668dd5ccbcfb244a035befb062bc3e", "8f4a6ec1ad42c4642a059f0cb0085657",
                "1b17e9c661022fe4f880efa8f6d88277", "a9ac48fcc28d31840a405cc3e1f8bcb0" };

            static int MaxPanelLen = 9;
            #endregion

            #region Func
            private void RefleshMaterial(SkebArtPanel t)
            {
                if (t.mat_profile != null && t.mesh != null)
                {
                    t.mesh.sharedMaterials = new Material[] { t.mat_profile };
                    EditorUtility.SetDirty(t.mesh);
                }
            }

            private void UpdateUrl(SkebArtPanel t, eMode mode)
            {
                t.url_profiles = new VRCUrl[60];
                for (int i = 0; i < 60; i++)
                {
                    int index = i + (60 * t.panel_index);

                    if (mode == eMode.Random)
                    {
                        t.url_profiles[i] =
                            new VRCUrl($"https://vrcart.skeb.jp/api/v2/externals/vrc_art_frame/image" + $"?i={index}");
                    }
                    else if (mode == eMode.Client)
                    {
                        t.url_profiles[i] =
                            new VRCUrl("https://vrcart.skeb.jp/api/v2/externals/vrc_art_frame/image?screen_name=" + $"{t.screen_name}&i={index}&role=client");
                    }
                    else if (mode == eMode.Creator)
                    {
                        t.url_profiles[i] =
                            new VRCUrl("https://vrcart.skeb.jp/api/v2/externals/vrc_art_frame/image?screen_name=" + $"{t.screen_name}&i={index}&role=creator");
                    }
                }
            }

            private bool DrawFields(SkebArtPanel t)
            {
                bool isChanged = false;

                EditorGUI.BeginChangeCheck();
                t.mesh = EditorGUILayout.ObjectField("メッシュ / mesh", t.mesh, typeof(MeshRenderer), true) as MeshRenderer;
                if (EditorGUI.EndChangeCheck())
                {
                    //メッシュのマテリアルを更新
                    RefleshMaterial(t);
                    isChanged = true;
                }

                GUILayout.Space(10);

                EditorGUI.BeginChangeCheck();
                t.mode = (eMode)EditorGUILayout.EnumPopup("モード / Mode", t.mode);
                if (EditorGUI.EndChangeCheck())
                {
                    UpdateUrl(t, t.mode);
                    isChanged = true;
                }

                EditorGUI.BeginChangeCheck();
                GUILayout.Space(10);

                if (t.mode != eMode.Random)
                {
                    t.screen_name = EditorGUILayout.TextField("ユーザーID / UserID", t.screen_name);
                    GUILayout.Label("ユーザーIDは https://skeb.jp/@hoge の@以降にあるIDを入れてください。");
                    GUILayout.Label("Please enter the ID after the @ in https://skeb.jp/@hoge.");
                    GUILayout.Space(10);
                }

                t.panel_index = EditorGUILayout.IntSlider("パネル番号 / Panel", t.panel_index, 0, MaxPanelLen);

                //フィールドが変わった場合はURL生成し直し
                if (EditorGUI.EndChangeCheck())
                {
                    if (t.panel_index > MaxPanelLen)
                        t.panel_index = MaxPanelLen;

                    if (0 > t.panel_index)
                        t.panel_index = 0;

                    UpdateUrl(t, t.mode);

                    if (!t.isCustomMaterial)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[t.panel_index]);
                        if (!string.IsNullOrEmpty(path))
                            t.mat_profile = AssetDatabase.LoadAssetAtPath<Material>(path);
                    }

                    //メッシュのマテリアルを更新
                    RefleshMaterial(t);

                    isChanged = true;
                }

                //分刻みのインターバルを指定
                t.interval = EditorGUILayout.IntField("インターバル / Interval", t.interval);

                GUILayout.Space(10);

                //自身で作成したマテリアルを使用する場合はチェックを入れてもらう
                t.isCustomMaterial = EditorGUILayout.ToggleLeft("カスタムマテリアル / Use custom material", t.isCustomMaterial);

                //カスタムマテリアルがオンの場合はマテリアルを触れるように
                EditorGUI.BeginDisabledGroup(!t.isCustomMaterial);
                EditorGUI.BeginChangeCheck();
                t.mat_profile = EditorGUILayout.ObjectField("マテリアル / Material", t.mat_profile, typeof(Material), true) as Material;
                if (EditorGUI.EndChangeCheck())
                {
                    //メッシュのマテリアルを更新
                    RefleshMaterial(t);
                    isChanged = true;
                }

                EditorGUI.EndDisabledGroup();

                return isChanged; 
            }
            #endregion

            #region Unity Internal Func
            public override void OnInspectorGUI()
            {
                SkebArtPanel t = target as SkebArtPanel;

                if (DrawFields(t))
                    EditorUtility.SetDirty(target);

                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
            #endregion
        }

        public class SkebArtPanel_Window : EditorWindow
        {
            private static void DebugLog(string msg = "", string color = "yellow", string title = nameof(SkebArtPanel))
            {
                Debug.Log($"[<color={color}>{title}</color>]{msg}");
            }

            #region variable
            static string guid_prefab = "7c9c39d02dc19a84ba48ae9159e428b3";
            #endregion

            #region Func
            private static bool isStringEmptyOrDontExists(string path)
            {
                return string.IsNullOrEmpty(path) || !System.IO.File.Exists(path);
            }

            private static GameObject GetPrefabFromGUID(string guid, string path = "")
            {
                string _path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(_path))
                {
                    //guid、path両方から取得出来ない場合は返す
                    if (isStringEmptyOrDontExists(path))
                        return null;

                    _path = path;
                }

                return AssetDatabase.LoadAssetAtPath<GameObject>(_path);
            }
            #endregion

            [MenuItem("SkebArtPanel/プレハブ設置 (Setup Prefab)")]
            private static void SetupPrefab()
            {
                GameObject prefab = GetPrefabFromGUID(guid_prefab, "Packages/skeb.artpanel/Runtime/SkebArtPanel.prefab");
                if (prefab == null)
                {
                    DebugLog("PrefabがPackages内に存在しません。\nSkeb Art Panelの再配置を行ってください。", "red");
                    return;
                }

                GameObject panel = Instantiate(prefab);
                panel.name = prefab.name;
                Undo.RegisterCreatedObjectUndo(panel, "Create SkebArtPanel");
                EditorGUIUtility.PingObject(panel);

                DebugLog("設置しました！", "green");
            }
        }
#endif
    }
}
