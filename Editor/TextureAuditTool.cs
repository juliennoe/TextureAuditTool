using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.AddressableAssets;

namespace JulienNoe.Tools.TextureAuditTool
{
    public class TextureAuditTool : EditorWindow
    {
        private Vector2 scrollPos;
        private Vector2 toggleScroll;
        private Dictionary<string, TextureData> textureDataDict = new Dictionary<string, TextureData>();
        private List<SpriteAtlas> spriteAtlases = new List<SpriteAtlas>();

        // Column toggles
        private bool showMipmap         = true;
        private bool showPowerOfTwo     = true;
        private bool showMultipleOfFour = true;
        private bool showInSpriteAtlas  = true;
        private bool showAddressable    = true;
        private bool showResolution     = true;
        private bool showFormat         = true;
        private bool showFileSize       = true;
        private bool showMemory         = true;
        private bool showPath           = true;

        // Sort options dropdown
        private string[] sortOptions = new[] { "Name", "MipMap", "Power of 2", "Multiple of 4", "In Atlas", "Addressable" };
        private int sortOption = 0;

        // Group foldouts
        private bool foldoutSprites  = true;
        private bool foldoutTextures = true;

        // Search string
        private string searchString = string.Empty;

        [MenuItem("Tools/Texture Audit")]
        public static void ShowWindow() => GetWindow<TextureAuditTool>("Texture Audit");

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Texture Audit Tool", EditorStyles.boldLabel);

            // Search bar
            EditorGUILayout.BeginHorizontal("box");
            searchString = EditorGUILayout.TextField(searchString, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                searchString = string.Empty;
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            // Sort dropdown
            sortOption = EditorGUILayout.Popup("Sort by", sortOption, sortOptions);

            // Column toggles scrollable
            toggleScroll = EditorGUILayout.BeginScrollView(toggleScroll, false, false, GUILayout.Height(EditorGUIUtility.singleLineHeight + 6));
            EditorGUILayout.BeginHorizontal("box");
            showMipmap         = GUILayout.Toggle(showMipmap,         "MipMap");
            showPowerOfTwo     = GUILayout.Toggle(showPowerOfTwo,     "Power of 2");
            showMultipleOfFour = GUILayout.Toggle(showMultipleOfFour, "Multiple of 4");
            showInSpriteAtlas  = GUILayout.Toggle(showInSpriteAtlas,  "In Atlas");
            showAddressable    = GUILayout.Toggle(showAddressable,    "Addressable");
            showResolution     = GUILayout.Toggle(showResolution,     "Resolution");
            showFormat         = GUILayout.Toggle(showFormat,         "Format");
            showFileSize       = GUILayout.Toggle(showFileSize,       "File Size");
            showMemory         = GUILayout.Toggle(showMemory,         "Memory");
            showPath           = GUILayout.Toggle(showPath,           "Path");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Scan"))
                ScanTextures();

            if (textureDataDict.Count > 0)
                DrawGroupedTable();
        }

        private void LoadSpriteAtlases()
        {
            spriteAtlases.Clear();
            var atlasGuids = AssetDatabase.FindAssets("t:SpriteAtlas", new[] { "Assets" });
            foreach (var guid in atlasGuids)
            {
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AssetDatabase.GUIDToAssetPath(guid));
                if (atlas != null)
                    spriteAtlases.Add(atlas);
            }
        }

        private void ScanTextures()
        {
            LoadSpriteAtlases();
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex == null) continue;

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                if (!textureDataDict.TryGetValue(path, out var data))
                {
                    data = new TextureData { path = path, name = tex.name };
                    textureDataDict[path] = data;
                }

                data.mipmap         = importer.mipmapEnabled;
                data.powerOfTwo     = IsPowerOfTwo(tex.width) && IsPowerOfTwo(tex.height);
                data.multipleOfFour = tex.width % 4 == 0 && tex.height % 4 == 0;
                data.isSprite       = importer.textureType == TextureImporterType.Sprite;

                // Sprite Atlas detection
                var sprites = new List<Sprite>();
                var mainSpr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (mainSpr != null) sprites.Add(mainSpr);
                sprites.AddRange(AssetDatabase.LoadAllAssetRepresentationsAtPath(path).OfType<Sprite>());
                data.inAtlas = sprites.Any(spr => spriteAtlases.Any(atlas => atlas.CanBindTo(spr)));

                // Addressable detection (always compile)
                data.addressable = false;
                var settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings != null && settings.FindAssetEntry(guid) != null)
                    data.addressable = true;

                data.resolution = $"{tex.width}×{tex.height}";
                var defSettings = importer.GetDefaultPlatformTextureSettings();
                var overSettings = importer.GetPlatformTextureSettings("Standalone");
                var chosen = overSettings.overridden ? overSettings : defSettings;
                data.format = chosen.format.ToString();

                long sizeBytes = new FileInfo(Path.GetFullPath(path)).Length;
                data.fileSize = $"{(sizeBytes / 1024f):F2} KB";

                int bpp = 4;
                long gpuBytes = (long)tex.width * tex.height * bpp;
                if (importer.mipmapEnabled)
                    gpuBytes = (long)(gpuBytes * 1.3333f);
                data.memory = $"{(gpuBytes / 1024f):F1} KB";
            }
        }

        private void DrawGroupedTable()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // Prepare entries
            var all = textureDataDict.Values
                .Where(d => string.IsNullOrEmpty(searchString) || d.name.IndexOf(searchString, System.StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            // Apply sorting based on dropdown
            switch (sortOption)
            {
                case 0: all = all.OrderBy(d => d.name).ToList(); break;
                case 1: all = all.OrderByDescending(d => d.mipmap).ThenBy(d => d.name).ToList(); break;
                case 2: all = all.OrderByDescending(d => d.powerOfTwo).ThenBy(d => d.name).ToList(); break;
                case 3: all = all.OrderByDescending(d => d.multipleOfFour).ThenBy(d => d.name).ToList(); break;
                case 4: all = all.OrderByDescending(d => d.inAtlas).ThenBy(d => d.name).ToList(); break;
                case 5: all = all.OrderByDescending(d => d.addressable).ThenBy(d => d.name).ToList(); break;
            }

            var sprites = all.Where(d => d.isSprite).ToList();
            var textures = all.Where(d => !d.isSprite).ToList();

            // Sprites group
            foldoutSprites = EditorGUILayout.Foldout(foldoutSprites, $"Sprites ({sprites.Count})", true);
            if (foldoutSprites)
            {
                DrawTableHeader();
                foreach (var d in sprites) DrawTableRow(d);
            }

            // Textures group
            foldoutTextures = EditorGUILayout.Foldout(foldoutTextures, $"Textures ({textures.Count})", true);
            if (foldoutTextures)
            {
                DrawTableHeader();
                foreach (var d in textures) DrawTableRow(d);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawTableHeader()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name", GUILayout.Width(150));
            if (showMipmap)         EditorGUILayout.LabelField("MipMap",    GUILayout.Width(60));
            if (showPowerOfTwo)     EditorGUILayout.LabelField("Power2",    GUILayout.Width(60));
            if (showMultipleOfFour) EditorGUILayout.LabelField("Mult4",     GUILayout.Width(60));
            if (showInSpriteAtlas)  EditorGUILayout.LabelField("In Atlas",  GUILayout.Width(60));
            if (showAddressable)    EditorGUILayout.LabelField("Addressable",GUILayout.Width(80));
            if (showResolution)     EditorGUILayout.LabelField("Resolution",GUILayout.Width(80));
            if (showFormat)         EditorGUILayout.LabelField("Format",    GUILayout.Width(80));
            if (showFileSize)       EditorGUILayout.LabelField("Size",      GUILayout.Width(80));
            if (showMemory)         EditorGUILayout.LabelField("Memory",    GUILayout.Width(80));
            if (showPath)           EditorGUILayout.LabelField("Path",      GUILayout.Width(300));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTableRow(TextureData d)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(d.name, GUILayout.Width(150)))
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(d.path));
            if (showMipmap)         EditorGUILayout.Toggle(d.mipmap,         GUILayout.Width(60));
            if (showPowerOfTwo)     EditorGUILayout.Toggle(d.powerOfTwo,     GUILayout.Width(60));
            if (showMultipleOfFour) EditorGUILayout.Toggle(d.multipleOfFour, GUILayout.Width(60));
            if (showInSpriteAtlas)  EditorGUILayout.Toggle(d.inAtlas,        GUILayout.Width(60));
            if (showAddressable)    EditorGUILayout.Toggle(d.addressable,    GUILayout.Width(80));
            if (showResolution)     EditorGUILayout.LabelField(d.resolution,   GUILayout.Width(80));
            if (showFormat)         EditorGUILayout.LabelField(d.format,       GUILayout.Width(80));
            if (showFileSize)       EditorGUILayout.LabelField(d.fileSize,     GUILayout.Width(80));
            if (showMemory)         EditorGUILayout.LabelField(d.memory,       GUILayout.Width(80));
            if (showPath)           EditorGUILayout.LabelField(new GUIContent(TruncatePath(d.path, 50), d.path), GUILayout.Width(300));
            EditorGUILayout.EndHorizontal();
        }

        private bool IsPowerOfTwo(int x) => (x & (x - 1)) == 0;

        private string TruncatePath(string path, int maxLength)
        {
            if (path.Length <= maxLength) return path;
            int part = (maxLength - 3) / 2;
            return path.Substring(0, part) + "..." + path.Substring(path.Length - part);
        }

        private class TextureData
        {
            public string name;
            public bool mipmap, powerOfTwo, multipleOfFour, isSprite, inAtlas, addressable;
            public string resolution, format, fileSize, memory, path;
        }
    }
}
