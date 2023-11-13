
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

namespace skeb.vrcartframe
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VRCArtFrame : UdonSharpBehaviour
    {
        private void DebugLog(string msg = "", string color = "yellow", string title = "VRCArtFrame")
        {
            Debug.Log($"[<color={color}>{title}</color>]{msg}");
        }

        #region serialize variables
        /// <summary>
        /// ダウンロードしたイラストを入れるMaterial
        /// </summary>
        public Material mat_profile;

        /// <summary>
        /// 次にリロードする時間
        /// </summary>
        public int interval = 1;

        /// <summary>
        /// 　SkebのID
        /// </summary>
        [HideInInspector]
        public string screen_name = "nalgami";
        #endregion

        #region variables
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
                //発火時だとUdonReceiverがエラーを吐くためちょっと待たせる
                currentTime += Time.deltaTime;
                if (currentTime > nextTime)
                {
                    int Index = GetIndex();

                    VRCUrl url = url_profiles[Index];
                    DownloadImage(url, mat_profile);
                    currentTime = 0.0f;
                    nextTime = 60 * interval;

                    DebugLog($"Reloading image now. interval is {interval}min.");
                }
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
        #endregion

        private void DownloadImage(VRCUrl url, Material mat)
        {
            if (downloader == null)
                downloader = new VRCImageDownloader();

            if (TargetUdon == null)
                TargetUdon = GetComponent<UdonBehaviour>();

            downloader.DownloadImage(url, mat, TargetUdon, info);

            DebugLog($"Downloading Image -> {url}");
        }

        public override void OnImageLoadSuccess(IVRCImageDownload result)
        {
            DebugLog($"images successfully downloaded");
        }

        public override void OnImageLoadError(IVRCImageDownload result)
        {
            DebugLog($"OnImageLoadError");
            DebugLog($"{result.ErrorMessage}");
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        [CustomEditor(typeof(VRCArtFrame))]
        public class VRCArtFrame_Editor : Editor
        {
            public override void OnInspectorGUI()
            {
                UdonSharpGUI.DrawConvertToUdonBehaviourButton(target);
                UdonSharpGUI.DrawProgramSource(target);
                UdonSharpGUI.DrawSyncSettings(target);
                UdonSharpGUI.DrawUtilities(target);

                GUILayout.Space(10);

                VRCArtFrame t = target as VRCArtFrame;

                EditorGUI.BeginChangeCheck();
                t.screen_name = EditorGUILayout.TextField("スクリーン名", t.screen_name);
                if (EditorGUI.EndChangeCheck())
                {
                    t.url_profiles = new VRCUrl[60];
                    for (int i = 0; i < 60; i++)
                    {
                        t.url_profiles[i] = new VRCUrl("https://vrcart.skeb.jp/api/v2/externals/vrc_art_frame/image?screen_name=" + t.screen_name + $"&i={i}");
                    }
                }

                GUILayout.Space(5);
                t.mat_profile = EditorGUILayout.ObjectField("絵を入れるマテリアル", t.mat_profile, typeof(Material), true) as Material;
                if (GUILayout.Button("マテリアル初期化"))
                {
                    string path = AssetDatabase.GUIDToAssetPath("02716fbb94d80ae4c88f868b73908b00");
                    if (!string.IsNullOrEmpty(path))
                        t.mat_profile = AssetDatabase.LoadAssetAtPath<Material>(path);
                }

                GUILayout.Space(5);
                t.interval = EditorGUILayout.IntField("インターバル (単位 : 分)", t.interval);

                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
        }
#endif
    }

}
