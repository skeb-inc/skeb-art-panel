
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Image;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UdonSharpEditor;
#endif

namespace skeb.skebartpanel
{
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
        /// 納品者として表示するか
        /// </summary>
        public bool isCreator = false;

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
        /// 初回用に1.0f
        /// </summary>
        private float nextTime = 1.0f;

        /// <summary>
        /// インターバルのためのタイマー
        /// </summary>
        private float currentTime;

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
            //ブースが表示されると発火
            currentTime = 0;

            //OnEnabledで発火するとエラーが出るので初期化だけ
            downloader = new VRCImageDownloader();
                    
            DebugLog("OnEnable");
        }

        private void Update()
        {
            if (mat_profile != null)
            {
                ReloadImageLoop();
            }
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
            if (downloader == null)
                downloader = new VRCImageDownloader();

            if (TargetUdon == null)
                TargetUdon = GetComponent<UdonBehaviour>();

            downloader.DownloadImage(url, mat, TargetUdon, info);

            DebugLog($"Downloading Image -> {url}");
        }
        private void ReloadImageLoop()
        {
            if (currentTime > nextTime)
            {
                int Index = GetIndex();

                VRCUrl url = url_profiles[Index];
                DownloadImage(url, mat_profile);
                currentTime = 0.0f;
                nextTime = 60 * interval;

                DebugLog($"Reloading image now. interval is {interval}min.");
            }
            else
            {
                currentTime += Time.deltaTime;
            }
        }
        #endregion

        #region VRChat Function
        public override void OnImageLoadSuccess(IVRCImageDownload result)
        {
            DebugLog($"images successfully downloaded");
        }

        public override void OnImageLoadError(IVRCImageDownload result)
        {
            DebugLog($"OnImageLoadError");
            DebugLog($"{result.ErrorMessage}");
        }
        #endregion

#if UNITY_EDITOR && !COMPILER_UDONSHARP

        [CustomEditor(typeof(SkebArtPanel))]
        public class VRCArtFrame_Editor : Editor
        {
            enum eRole
            {
                [InspectorName("Client")] Client,
                [InspectorName("Creator")] Creator
            }

            eRole _role = eRole.Client;

            /// <summary>
            /// 
            /// </summary>
            string[] guids = new string[] { 
                "02716fbb94d80ae4c88f868b73908b00", "b557273905391dc45b68cea1b35a4a40", 
                "97566aeeaee35914e913384b42a1a193", "5205e0ba4aa7b44409bcd1136057627f",
                "dd52660c231bc9249bb480182764e449", "770e70378e776384995d8dacadb96caf",
                "da668dd5ccbcfb244a035befb062bc3e", "8f4a6ec1ad42c4642a059f0cb0085657",
                "1b17e9c661022fe4f880efa8f6d88277", "a9ac48fcc28d31840a405cc3e1f8bcb0" };

            static int MaxPanelLen = 9;

            private void RefleshMaterial(SkebArtPanel t)
            {
                if (t.mat_profile != null && t.mesh != null)
                {
                    t.mesh.materials = new Material[] { t.mat_profile };
                    EditorUtility.SetDirty(t.mesh);
                }
            }

            private void DrawFields(SkebArtPanel t)
            {
                bool isChanged = false;

                _role = t.isCreator ? eRole.Creator : eRole.Client;

                EditorGUI.BeginChangeCheck();
                t.mesh = EditorGUILayout.ObjectField("メッシュ / mesh", t.mesh, typeof(MeshRenderer), true) as MeshRenderer;
                if (EditorGUI.EndChangeCheck())
                {
                    //メッシュのマテリアルを更新
                    RefleshMaterial(t);
                    isChanged = true;
                }

                GUILayout.Space(15);

                EditorGUI.BeginChangeCheck();
                t.screen_name = EditorGUILayout.TextField("ユーザーID / UserID", t.screen_name);
                GUILayout.Label("ユーザーIDは https://skeb.jp/@hoge の@以降にあるIDを入れてください。");
                GUILayout.Label("Please enter the ID after the @ in https://skeb.jp/@hoge.");

                GUILayout.Space(10);

                _role = (eRole)EditorGUILayout.EnumPopup("モード / Mode", _role);
                t.panel_index = EditorGUILayout.IntSlider("パネル番号 / Panel", t.panel_index, 0, MaxPanelLen);

                //フィールドが変わった場合はURL生成し直し
                if (EditorGUI.EndChangeCheck())
                {
                    //Playモードになると初期化されるのでこの方法で格納
                    t.isCreator = _role == eRole.Creator;

                    if (t.panel_index > MaxPanelLen)
                        t.panel_index = MaxPanelLen;

                    if (0 > t.panel_index)
                        t.panel_index = 0;

                    t.url_profiles = new VRCUrl[60];
                    for (int i = 0; i < 60; i++)
                    {
                        string _role = t.isCreator ? "&role=creator" : "&role=client";

                        t.url_profiles[i] = new VRCUrl("https://vrcart.skeb.jp/api/v2/externals/vrc_art_frame/image?screen_name=" + $"{t.screen_name}&i={i + (60 * t.panel_index)}&role={_role}");
                    }

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

                if (isChanged)
                    EditorUtility.SetDirty(target);
            }

            public override void OnInspectorGUI()
            {
                SkebArtPanel t = target as SkebArtPanel;

                DrawFields(t);

                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
        }
#endif
    }
}
