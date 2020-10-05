﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using VRWorldToolkit.DataStructures;

namespace VRWorldToolkit
{
    public class BuildReportTreeView : TreeView
    {
        private BuildReport report;

        enum TreeColumns
        {
            Type,
            Size,
            Percentage,
            Name,
            Extension,
        }

        public BuildReportTreeView(TreeViewState state, MultiColumnHeader multicolumnHeader, BuildReport report) : base(state, multicolumnHeader)
        {
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            multicolumnHeader.sortingChanged += OnSortingChanged;

            this.report = report;

            if (report != null && report.summary.result == BuildResult.Succeeded)
            {
                Reload();
            }
        }

        private class BuildListAsset
        {
            public string assetType { get; set; }
            public string fullPath { get; set; }
            public int size { get; set; }
            public double percentage { get; set; }

            public BuildListAsset() { }

            public BuildListAsset(string assetType, string fullPath, int size)
            {
                this.assetType = assetType;
                this.fullPath = fullPath;
                this.size = size;
            }
        }

        public class BuildReportItem : TreeViewItem
        {
            public Texture previewIcon { get; set; }
            public string assetType { get; set; }
            public string name { get; set; }
            public string path { get; set; }
            public string extension { get; set; }
            public int size { get; set; }
            public double percentage { get; set; }

            public BuildReportItem(int id, Texture previewIcon, string type, string name, string path, string extension, int size, double percentage) : base(id)
            {
                this.previewIcon = previewIcon;
                this.assetType = type;
                this.name = name;
                this.path = path;
                this.extension = extension;
                this.size = size;
                this.percentage = percentage;
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = -1, depth = -1 };

            var serializedReport = new SerializedObject(report);

            var bl = new List<BuildListAsset>();

            var appendices = serializedReport.FindProperty("m_Appendices");

            var summary = serializedReport.FindProperty("m_Summary");

            for (int i = 0; i < appendices.arraySize; i++)
            {
                var appendix = appendices.GetArrayElementAtIndex(i);

                if (appendix.objectReferenceValue.GetType() != typeof(UnityEngine.Object)) continue;

                var serializedAppendix = new SerializedObject(appendix.objectReferenceValue);

                if (serializedAppendix.FindProperty("m_ShortPath") is null) continue;

                int size = serializedAppendix.FindProperty("m_Overhead").intValue;

                var contents = serializedAppendix.FindProperty("m_Contents");

                for (int j = 0; j < contents.arraySize; j++)
                {
                    var entry = contents.GetArrayElementAtIndex(j);

                    var fullPath = entry.FindPropertyRelative("buildTimeAssetPath").stringValue;

                    var assetImporter = AssetImporter.GetAtPath(fullPath);

                    var type = assetImporter != null ? assetImporter.GetType().Name : "Unknown";

                    if (type.EndsWith("Importer"))
                    {
                        type = type.Remove(type.Length - 8);
                    }

                    var byteSize = entry.FindPropertyRelative("packedSize").intValue;

                    var asset = new BuildListAsset(type, fullPath, byteSize);

                    bl.Add(asset);
                }
            }

            var results = bl
                .GroupBy(x => x.fullPath)
                .Select(cx => new BuildListAsset()
                {
                    assetType = cx.First().assetType,
                    fullPath = cx.First().fullPath,
                    size = cx.Sum(x => x.size),
                })
                .OrderByDescending(x => x.size)
                .ToList();

            var totalSize = results.Sum(x => x.size);

            for (int i = 0; i < results.Count; i++)
            {
                results[i].percentage = (double)results[i].size / totalSize;
            }

            for (int i = 0; i < results.Count; i++)
            {
                var asset = results[i];

                root.AddChild(new BuildReportItem(i, AssetDatabase.GetCachedIcon(asset.fullPath), asset.assetType, asset.fullPath == "" ? "Unknown" : Path.GetFileName(asset.fullPath), asset.fullPath, Path.GetExtension(asset.fullPath), asset.size, asset.percentage));
            }

            return root;
        }

        /// <summary>
        /// Set new report for treeview
        /// </summary>
        /// <param name="report">New report to set</param>
        public void SetReport(BuildReport report)
        {
            // Set new report
            this.report = report;

            // Reload the treeview
            if (HasReport())
            {
                base.Reload();
            }
        }

        /// <summary>
        /// Check if treeview has build report and make sure the build wasn't failed
        /// </summary>
        /// <returns>If build is set and the build succeeded</returns>
        public bool HasReport()
        {
            if (report != null && report.summary.result == BuildResult.Succeeded) return true;

            return false;
        }

        struct CategoryStats
        {
            public string name;
            public int size;
        }

        /// <summary>
        /// Draw overall stats view of the current build report
        /// </summary>
        public void DrawOverallStats()
        {
            if (HasReport())
            {
                List<BuildReportItem> stats = base.GetRows().Cast<BuildReportItem>().ToList();

                int totalSize = stats.Sum(x => x.size);

                var grouped = stats
                    .GroupBy(x => x.assetType)
                    .Select(cx => new CategoryStats()
                    {
                        name = cx.First().assetType,
                        size = cx.Sum(x => x.size),
                    }).OrderByDescending(x => x.size)
                    .ToArray();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                for (int i = 0; i < grouped.Length; i++)
                {
                    var item = grouped[i];

                    string name = "";

                    switch (item.name)
                    {
                        case "Mono":
                            name = "Scripts";
                            break;
                        case "Model":
                        case "Texture":
                        case "Shader":
                        case "Asset":
                        case "TrueTypeFont":
                        case "Plugin":
                        case "Prefab":
                            name = item.name + "s";
                            break;
                        default:
                            name = item.name;
                            break;
                    }

                    if (GUILayout.Button(name + " " + EditorUtility.FormatBytes(item.size) + " " + ((double)item.size / totalSize).ToString("P"), EditorStyles.label))
                    {
                        base.searchString = item.name;
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(float treeViewWidth)
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = EditorGUIUtility.IconContent("FilterByType"),
                    contextMenuText = "Type",
                    headerTextAlignment = TextAlignment.Left,
                    canSort = false,
                    width = 20,
                    minWidth = 20,
                    maxWidth = 20,
                    autoResize = true,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Size", "Uncompressed size of asset"),
                    contextMenuText = "Size",
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 60,
                    minWidth = 60,
                    maxWidth = 70,
                    autoResize = true,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("%", "Percentage out of all assets"),
                    contextMenuText = "Percentage",
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 60,
                    minWidth = 60,
                    maxWidth = 70,
                    autoResize = true,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Name"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 250,
                    minWidth = 60,
                    autoResize = true,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Type", "File type"),
                    contextMenuText = "Type",
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 60,
                    minWidth = 60,
                    maxWidth = 100,
                    autoResize = true,
                    allowToggleVisibility = true
                }
            };

            var state = new MultiColumnHeaderState(columns);
            return state;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var buildReportItem = (BuildReportItem)args.item;

            for (var visibleColumnIndex = 0; visibleColumnIndex < args.GetNumVisibleColumns(); visibleColumnIndex++)
            {
                // Get the current cell rect and index
                var rect = args.GetCellRect(visibleColumnIndex);
                var columnIndex = (TreeColumns)args.GetColumn(visibleColumnIndex);

                //Set label style to white if cell is selected otherwise to normal
                var labelStyle = args.selected ? Styles.TreeViewLabelSelected : Styles.TreeViewLabel;

                //Handle drawing of the columns
                switch (columnIndex)
                {
                    case TreeColumns.Type:
                        GUI.Label(rect, buildReportItem.previewIcon, GUIStyle.none);
                        break;
                    case TreeColumns.Name:
                        if (args.selected && buildReportItem.path != "")
                        {
                            EditorGUI.LabelField(rect, buildReportItem.path, labelStyle);
                        }
                        else
                        {
                            EditorGUI.LabelField(rect, buildReportItem.name, labelStyle);
                        }
                        break;
                    case TreeColumns.Size:
                        EditorGUI.LabelField(rect, EditorUtility.FormatBytes(buildReportItem.size), labelStyle);
                        break;
                    case TreeColumns.Percentage:
                        EditorGUI.LabelField(rect, buildReportItem.percentage.ToString("P"), labelStyle);
                        break;
                    case TreeColumns.Extension:
                        EditorGUI.LabelField(rect, buildReportItem.extension, labelStyle);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(columnIndex), columnIndex, null);
                }
            }
        }

        /// <summary>
        /// Handle double clicks inside the treeview
        /// </summary>
        /// <param name="id"></param>
        protected override void DoubleClickedItem(int id)
        {
            base.DoubleClickedItem(id);

            // Get the clicked item
            var clickedItem = (BuildReportItem)base.FindItem(id, base.rootItem);

            //Ping clicked asset in project window
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(clickedItem.path));
        }

        /// <summary>
        /// Handle context clicks inside the treeview
        /// </summary>
        /// <param name="id">ID of the clicked treeview item</param>
        protected override void ContextClickedItem(int id)
        {
            base.ContextClickedItem(id);

            // Get the clicked item
            var clickedItem = (BuildReportItem)base.FindItem(id, base.rootItem);

            //base.SetSelection(new IList<int>());

            // Create new 
            GenericMenu menu = new GenericMenu();

            // Create the menu items
            menu.AddItem(new GUIContent("Copy Name"), false, ReplaceClipboard, clickedItem.name + clickedItem.extension);
            menu.AddItem(new GUIContent("Copy Path"), false, ReplaceClipboard, (string)clickedItem.path);

            // Show the menu
            menu.ShowAsContext();

            // Function to replace clipboard contents
            void ReplaceClipboard(object input)
            {
                EditorGUIUtility.systemCopyBuffer = (string)input;
            }
        }

        /// <summary>
        /// Check if current item matches the search string
        /// </summary>
        /// <param name="item">Item to match</param>
        /// <param name="search">Search string</param>
        /// <returns></returns>
        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            // Cast match item for parameter acess
            var textureTreeViewItem = (BuildReportItem)item;

            // Try to match the search string to item name or asset type and return true if it does
            if (textureTreeViewItem.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                textureTreeViewItem.assetType.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) return true;

            // Return false if the search string doesn't match
            return false;
        }

        /// <summary>
        /// Handle treeview columns sorting changes
        /// </summary>
        private void OnSortingChanged(MultiColumnHeader multiColumnHeader)
        {
            if (!(multiColumnHeader.sortedColumnIndex > -1)) return;

            // Get treeview items
            var items = rootItem.children.Cast<BuildReportItem>();

            // Sort items by sorted column
            switch (multiColumnHeader.sortedColumnIndex)
            {
                case 1:
                case 2:
                    items = items.OrderBy(x => x.size);
                    break;
                case 3:
                    items = items.OrderBy(x => x.name);
                    break;
                case 4:
                    items = items.OrderBy(x => x.extension);
                    break;
                default:
                    break;
            }

            // Reverse list if not sorted ascending
            if (!multiColumnHeader.IsSortedAscending(multiColumnHeader.sortedColumnIndex))
            {
                items = items.Reverse();
            }

            // Cast collection back to a list
            rootItem.children = items.Cast<TreeViewItem>().ToList();

            // Build rows again with the new sorting
            BuildRows(rootItem);
        }
    }
}
