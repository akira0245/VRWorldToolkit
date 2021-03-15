﻿using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRWorldToolkit.DataStructures;

namespace VRWorldToolkit
{
    public class TextureDetails
    {
        private int? uncrunchedCount = null;
        private int? normalMaps = null;
        private int? cubemaps = null;
        private long? storageSize = null;

        private readonly Dictionary<Texture, TextureImporter> textureList = new Dictionary<Texture, TextureImporter>();

        public void AddTexture(TextureImporter textureImporter, Texture texture)
        {
            if (!textureList.ContainsKey(texture) && textureImporter != null)
            {
                textureList.Add(texture, textureImporter);
            }
        }

        public int TextureCount => textureList.Count;

        public int UncrunchedCount
        {
            get
            {
                if (uncrunchedCount is null)
                {
                    uncrunchedCount = textureList.Count(x => !x.Value.crunchedCompression);
                }

                return (int) uncrunchedCount;
            }
        }

        public int NormalMaps
        {
            get
            {
                if (normalMaps is null)
                {
                    normalMaps = textureList.Count(x => x.Value.textureType == TextureImporterType.NormalMap);
                }

                return (int) normalMaps;
            }
        }

        public int Cubemaps
        {
            get
            {
                if (cubemaps is null)
                {
                    cubemaps = textureList.Count(x => x.Value.textureShape == TextureImporterShape.TextureCube);
                }

                return (int) cubemaps;
            }
        }

        public long StorageSize
        {
            get
            {
                if (storageSize is null)
                {
                    storageSize = textureList.Sum(x => EditorTextureUtil.GetStorageMemorySize(x.Key));
                }

                return (long) storageSize;
            }
        }

        public IEnumerable<TextureImporter> GetImporters
        {
            get { return textureList.Select(x => x.Value).ToArray(); }
        }

        public void ResetStats()
        {
            uncrunchedCount = null;
            storageSize = null;
        }
    }

    public class ImporterSettingsManager
    {
        // Mip Maps
        public bool DontChangeMipMaps { get; private set; }
        public bool StreamingMipMap { get; private set; } = true;
        public bool GenerateMipMaps { get; private set; } = true;
        public bool DontChangeAniso { get; private set; } = true;
        public int AnisoLevel { get; private set; } = 1;
        public OverrideWhenSize OverrideAnisoWhen { get; private set; }

        // Max Texture Size
        public int MaxTextureSize { get; private set; } = 2048;
        public OverrideWhenSize OverrideMaxTextureSizeWhen { get; private set; } = OverrideWhenSize.BiggerThan;

        // Crunch compression
        public bool CrunchCompression { get; private set; } = true;
        public int CompressionQuality { get; private set; } = 50;
        public DontOverrideWhen DontOverrideCrunchWhen { get; private set; }
        public OverrideWhenSize OverrideCrunchCompressionSizeWhen { get; private set; }

        // Ignores
        public bool IgnoreCubemaps { get; private set; } = true;
        public OverrideWhenSize OverrideCubemapSettingsWhen { get; private set; } = OverrideWhenSize.SmallerThan;
        public int CubemapSize { get; private set; } = 512;
        public bool IgnoreNormalMaps { get; private set; } = true;

        private readonly string[] maxTextureNames = {"32", "64", "128", "256", "512", "1024", "2048", "4096", "8192"};
        private readonly int[] maxTextureSizes = {32, 64, 128, 256, 512, 1024, 2048, 4096, 8192};

        public enum OverrideWhenSize
        {
            Always,
            SmallerThan,
            BiggerThan
        }

        public enum DontOverrideWhen
        {
            Never,
            AlreadyDisabled,
            AlreadyEnabled
        }

        public void DrawSettings()
        {
            GUILayout.Label("Mip Maps", Styles.BoldWrap);
            DontChangeMipMaps = EditorGUILayout.Toggle("Don't Change", DontChangeMipMaps);
            using (new EditorGUI.DisabledScope(DontChangeMipMaps))
            {
                StreamingMipMap = EditorGUILayout.Toggle("Streaming Mip Maps", StreamingMipMap);
                GenerateMipMaps = EditorGUILayout.Toggle("Generate Mip Maps", GenerateMipMaps);
            }

            GUILayout.Label("Aniso Level", Styles.BoldWrap);
            DontChangeAniso = EditorGUILayout.Toggle("Don't Change", DontChangeAniso);
            using (new EditorGUI.DisabledScope(DontChangeAniso))
            {
                AnisoLevel = EditorGUILayout.IntSlider("Aniso Level", AnisoLevel, 0, 16);
                using (new EditorGUI.IndentLevelScope())
                {
                    OverrideAnisoWhen = (OverrideWhenSize) EditorGUILayout.EnumPopup("Override When", OverrideAnisoWhen);
                }
            }

            GUILayout.Label("Size", Styles.BoldWrap);
            MaxTextureSize = EditorGUILayout.IntPopup("Max Size", MaxTextureSize, maxTextureNames, maxTextureSizes);
            using (new EditorGUI.IndentLevelScope())
            {
                OverrideMaxTextureSizeWhen = (OverrideWhenSize) EditorGUILayout.EnumPopup("Override When", OverrideMaxTextureSizeWhen);
            }

            GUILayout.Label("Crunch Compression", Styles.BoldWrap);
            CrunchCompression = EditorGUILayout.Toggle("Use Crunch Compression", CrunchCompression);
            using (new EditorGUI.DisabledScope(!CrunchCompression))
            {
                CompressionQuality = EditorGUILayout.IntSlider(CompressionQuality, 1, 100);
                using (new EditorGUI.IndentLevelScope())
                {
                    OverrideCrunchCompressionSizeWhen = (OverrideWhenSize) EditorGUILayout.EnumPopup("Override When", OverrideCrunchCompressionSizeWhen);
                }
            }

            using (new EditorGUI.IndentLevelScope())
            {
                DontOverrideCrunchWhen = (DontOverrideWhen) EditorGUILayout.EnumPopup("Don't Override When", DontOverrideCrunchWhen);
            }

            GUILayout.Label("Ignore", Styles.BoldWrap);
            IgnoreCubemaps = EditorGUILayout.Toggle("Cubemaps", IgnoreCubemaps);
            using (new EditorGUI.IndentLevelScope())
            {
                using (new EditorGUI.DisabledScope(!IgnoreCubemaps))
                {
                    OverrideCubemapSettingsWhen = (OverrideWhenSize) EditorGUILayout.EnumPopup("Ignore When", OverrideCubemapSettingsWhen);
                    using (new EditorGUI.DisabledScope(OverrideCubemapSettingsWhen == OverrideWhenSize.Always))
                    {
                        CubemapSize = EditorGUILayout.IntPopup("Ignore Size", CubemapSize, maxTextureNames, maxTextureSizes);
                    }
                }
            }

            IgnoreNormalMaps = EditorGUILayout.Toggle("Normal maps", IgnoreNormalMaps);
        }

        public void ProcessTextures(TextureDetails details)
        {
            try
            {
                AssetDatabase.StartAssetEditing();

                var importers = details.GetImporters;
                var count = details.TextureCount;
                var current = 1;
                foreach (var importer in importers)
                {
                    EditorUtility.DisplayProgressBar("Applying New Settings", importer.assetPath, (float) current / count);

                    if (IgnoreNormalMaps && importer.textureType == TextureImporterType.NormalMap)
                        continue;

                    if (IgnoreCubemaps && importer.textureShape == TextureImporterShape.TextureCube)
                    {
                        var oldMaxTextureSize = importer.maxTextureSize;
                        var newMaxTextureSize = MaxTextureSize;

                        switch (OverrideCubemapSettingsWhen)
                        {
                            case OverrideWhenSize.SmallerThan:
                                if (oldMaxTextureSize > newMaxTextureSize) continue;
                                break;
                            case OverrideWhenSize.BiggerThan:
                                if (oldMaxTextureSize < newMaxTextureSize) continue;
                                break;
                        }
                    }

                    if (!DontChangeMipMaps)
                    {
                        importer.mipmapEnabled = GenerateMipMaps;
                        importer.streamingMipmaps = StreamingMipMap;
                    }

                    if (!DontChangeAniso)
                    {
                        var oldAnisoLevel = importer.anisoLevel;
                        var newAnisoLevel = AnisoLevel;

                        switch (OverrideAnisoWhen)
                        {
                            case OverrideWhenSize.Always:
                                importer.anisoLevel = AnisoLevel;
                                break;
                            case OverrideWhenSize.SmallerThan:
                                if (oldAnisoLevel < newAnisoLevel)
                                {
                                    importer.anisoLevel = newAnisoLevel;
                                }

                                break;
                            case OverrideWhenSize.BiggerThan:
                                if (oldAnisoLevel > newAnisoLevel)
                                {
                                    importer.maxTextureSize = newAnisoLevel;
                                }

                                break;
                        }
                    }

                    var skipCrunchCompression = false;

                    if (DontOverrideCrunchWhen != DontOverrideWhen.Never)
                    {
                        switch (DontOverrideCrunchWhen)
                        {
                            case DontOverrideWhen.AlreadyDisabled:
                                if (!importer.crunchedCompression) skipCrunchCompression = true;
                                break;
                            case DontOverrideWhen.AlreadyEnabled:
                                if (importer.crunchedCompression) skipCrunchCompression = true;
                                break;
                        }
                    }

                    if (!skipCrunchCompression)
                    {
                        var oldMaxTextureSize = importer.maxTextureSize;
                        var newMaxTextureSize = MaxTextureSize;
                        importer.crunchedCompression = CrunchCompression;

                        if (importer.crunchedCompression)
                        {
                            switch (OverrideMaxTextureSizeWhen)
                            {
                                case OverrideWhenSize.Always:
                                    importer.maxTextureSize = newMaxTextureSize;
                                    break;
                                case OverrideWhenSize.SmallerThan:
                                    if (oldMaxTextureSize < newMaxTextureSize)
                                    {
                                        importer.maxTextureSize = newMaxTextureSize;
                                    }

                                    break;
                                case OverrideWhenSize.BiggerThan:
                                    if (oldMaxTextureSize > newMaxTextureSize)
                                    {
                                        importer.maxTextureSize = newMaxTextureSize;
                                    }

                                    break;
                            }
                        }
                    }

                    importer.SaveAndReimport();
                    current++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                details.ResetStats();
                AssetDatabase.StopAssetEditing();
            }
        }
    }

    public class MassTextureImporter : EditorWindow
    {
        [MenuItem("VRWorld Toolkit/Quick Functions/Mass Texture Importer", false, 5)]
        public static void ShowWindow()
        {
            var window = GetWindow(typeof(MassTextureImporter));
            window.titleContent = new GUIContent("Mass Texture Importer");
            window.minSize = new Vector2(400, 510);
            window.Show();
        }

        private TextureDetails details;

        private Vector2 scrollPos;

        private ImporterSettingsManager importerSettingsManager = new ImporterSettingsManager();

        private void OnGUI()
        {
            if (details is null || details.TextureCount == 0)
            {
                TextureSelection();
            }
            else
            {
                MainUI();
            }
        }

        private void TextureSelection()
        {
            GUILayout.FlexibleSpace();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                GUILayout.Label("Get all textures from:");

                GUILayout.FlexibleSpace();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Scene", GUILayout.Width(80), GUILayout.Height(20)))
                {
                    details = GetAllTexturesFromScene();
                }

                GUILayout.Space(5);

                if (GUILayout.Button("Assets", GUILayout.Width(80), GUILayout.Height(20)))
                {
                    details = GetAllTexturesFromAssets();
                }

                GUILayout.FlexibleSpace();
            }

            GUILayout.FlexibleSpace();
        }

        private void MainUI()
        {
            GUILayout.Label("Selected Textures", Styles.BoldWrap);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label("<b>Texture Count:</b> " + details.TextureCount, Styles.LabelRichText);
                    GUILayout.Label("<b>Uncrunched Count:</b> " + details.UncrunchedCount, Styles.LabelRichText);
                    GUILayout.Label("<b>Storage Size:</b> " + EditorUtility.FormatBytes(details.StorageSize), Styles.LabelRichText);
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label("<b>Normal Maps:</b> " + details.NormalMaps, Styles.LabelRichText);
                    GUILayout.Label("<b>Cubemaps:</b> " + details.Cubemaps, Styles.LabelRichText);
                }
            }

            importerSettingsManager.DrawSettings();

            GUILayout.Space(5);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Go Back"))
                {
                    details = new TextureDetails();
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Revert"))
                {
                    importerSettingsManager = new ImporterSettingsManager();
                }

                if (GUILayout.Button("Apply"))
                {
                    if (EditorUtility.DisplayDialog("Process Importers?", $"About to process Texture Import settings on {details.TextureCount} textures, this can take a while depending on the amount and size of them.\n\nDo you want to continue?", "Ok", "Cancel"))
                    {
                        importerSettingsManager.ProcessTextures(details);
                    }
                }
            }
        }


        private static TextureDetails GetAllTexturesFromScene()
        {
            var details = new TextureDetails();
            var checkedMaterials = new List<Material>();

            var allGameObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject));
            for (var i = 0; i < allGameObjects.Length; i++)
            {
                var gameObject = allGameObjects[i] as GameObject;

                if (gameObject.hideFlags != HideFlags.None || EditorUtility.IsPersistent(gameObject.transform.root.gameObject)) continue;

                var renderers = gameObject.GetComponents<Renderer>();
                for (var j = 0; j < renderers.Length; j++)
                {
                    var renderer = renderers[j];
                    for (var k = 0; k < renderer.sharedMaterials.Length; k++)
                    {
                        var material = renderer.sharedMaterials[k];

                        if (material == null || checkedMaterials.Contains(material))
                            continue;

                        checkedMaterials.Add(material);

                        var shader = material.shader;

                        for (var l = 0; l < ShaderUtil.GetPropertyCount(shader); l++)
                        {
                            if (ShaderUtil.GetPropertyType(shader, l) == ShaderUtil.ShaderPropertyType.TexEnv)
                            {
                                var texture = material.GetTexture(ShaderUtil.GetPropertyName(shader, l));

                                var textureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;

                                if (textureImporter != null)
                                {
                                    details.AddTexture(textureImporter, texture);
                                }
                            }
                        }
                    }
                }
            }

            return details;
        }

        private static TextureDetails GetAllTexturesFromAssets()
        {
            var details = new TextureDetails();

            var assetGuidStrings = AssetDatabase.FindAssets("t:texture2D", new[] {"Assets"});

            for (var i = 0; i < assetGuidStrings.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(assetGuidStrings[i]);
                var texture = AssetDatabase.LoadAssetAtPath<Texture>(path);
                var textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

                details.AddTexture(textureImporter, texture);
            }

            return details;
        }
    }
}