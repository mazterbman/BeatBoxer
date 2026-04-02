using System;
using System.Collections.Generic;
using System.IO;
using Game.Scripts.ScriptableObject;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
public class OsuTimingEditor : EditorWindow
{
    private TextAsset osuFile;
    private AudioClip audioClip;
    private string settingsName = "New Timing Settings";
    private string savePath = "Assets";
    
    [MenuItem("Tools/OSU Timing Parser")]
    public static void ShowWindow()
    {
        GetWindow<OsuTimingEditor>("OSU Timing Parser");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("OSU Timing Parser", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        osuFile = (TextAsset)EditorGUILayout.ObjectField("OSU File", osuFile, typeof(TextAsset), false);
        audioClip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip (Optional)", audioClip, typeof(AudioClip), false);
        settingsName = EditorGUILayout.TextField("Settings Name", settingsName);
        
        EditorGUILayout.BeginHorizontal();
        savePath = EditorGUILayout.TextField("Save Path", savePath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string selectedPath = EditorUtility.SaveFolderPanel("Select Save Folder", savePath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                savePath = GetRelativePath(selectedPath);
            }
        }
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        GUI.enabled = osuFile != null;
        if (GUILayout.Button("Generate Timing Settings", GUILayout.Height(40)))
        {
            GenerateTimingSettings();
        }
        GUI.enabled = true;
    }
    
    private void GenerateTimingSettings()
    {
        if (osuFile == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select an OSU file.", "OK");
            return;
        }
        
        try
        {
            // Парсим данные из .osu файла
            List<TimingValue> timingValues = ParseOsuData(osuFile.text);
            
            if (timingValues == null || timingValues.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No timing values found in the OSU file.", "OK");
                return;
            }
            
            // Создаем ScriptableObject
            TrackSettings settings = ScriptableObject.CreateInstance<TrackSettings>();
            settings.TimingValues = timingValues;
            settings.AudioClip = audioClip;
            
            // Проверяем и создаем директорию
            string fullPath = Path.Combine(Application.dataPath, savePath.Replace("Assets/", ""));
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
            
            // Сохраняем ассет
            string assetPath = Path.Combine(savePath, $"{settingsName}.asset");
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            
            AssetDatabase.CreateAsset(settings, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Success", $"Timing Settings created successfully at:\n{assetPath}\n\nTotal notes: {timingValues.Count}", "OK");
            
            // Выбираем созданный ассет в Project окне
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to generate timing settings:\n{e.Message}", "OK");
            Debug.LogError($"OSU parsing error: {e}");
        }
    }
    
    private List<TimingValue> ParseOsuData(string osuContent)
    {
        List<TimingValue> timingValues = new List<TimingValue>();
        
        // Находим секцию [HitObjects]
        int hitObjectsIndex = osuContent.IndexOf("[HitObjects]");
        if (hitObjectsIndex == -1)
        {
            Debug.LogError("Section [HitObjects] not found in the file");
            return timingValues;
        }
        
        // Находим начало данных после секции
        int dataStart = osuContent.IndexOf('\n', hitObjectsIndex);
        if (dataStart == -1)
        {
            Debug.LogError("No data in [HitObjects] section");
            return timingValues;
        }
        
        string hitObjectsData = osuContent.Substring(dataStart + 1);
        
        // Разделяем строку на отдельные объекты
        string[] lines = hitObjectsData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            
            TimingValue timing = ParseHitObject(line);
            if (timing != null)
            {
                timingValues.Add(timing);
            }
        }
        
        return timingValues;
    }
    
    private TimingValue ParseHitObject(string hitObjectLine)
    {
        // Убираем двоеточие в конце, если есть
        string line = hitObjectLine.TrimEnd(':');
        
        // Разделяем по запятым
        string[] parts = line.Split(',');
        
        if (parts.Length < 4)
        {
            Debug.LogWarning($"Invalid line: {hitObjectLine}");
            return null;
        }
        
        // Парсим координаты
        if (!int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y))
        {
            Debug.LogWarning($"Failed to parse coordinates: {hitObjectLine}");
            return null;
        }
        
        // Парсим время (в миллисекундах)
        if (!int.TryParse(parts[2], out int timeMs))
        {
            Debug.LogWarning($"Failed to parse time: {hitObjectLine}");
            return null;
        }
        
        // Парсим тип нажатия
        if (!int.TryParse(parts[3], out int hitType))
        {
            Debug.LogWarning($"Failed to parse hit type: {hitObjectLine}");
            return null;
        }
        
        float timeSeconds = timeMs / 1000f;
        
        // Определяем тип стрелки по координатам
        ArrowDirection arrowDirection = GetArrowDirectionFromCoordinates(x, y);
        ArrowType arrowType = (hitType == 128 && parts.Length >= 6) ? ArrowType.Hold : ArrowType.Click;

        // Создаем тайминг для всех типов нот
        // hitType: 1 = обычный клик, 128 = зажатие, 5 = клик+whistle, 9 = клик+finish
        switch (arrowType)
        {
            case ArrowType.Click:
                return new TimingValue
                {
                    TimeStart = timeSeconds,
                    TimeEnd = 0,
                    ArrowDirection = arrowDirection,
                    ArrowType = arrowType
                };
                break;
            case ArrowType.Hold:
                // Парсим время конца зажатия
                if (!int.TryParse(parts[5].Split(':')[0], out int timeEndMs))
                {
                    Debug.LogWarning($"Failed to parse hit type: {hitObjectLine}");
                    return null;
                }
                float timeEndSeconds = timeEndMs / 1000f;
            
                // Для зажатой клавиши создаем тайминг на начало зажатия
                // Если нужно добавить логику для окончания, можно добавить дополнительный тайминг
                return new TimingValue
                {
                    TimeStart = timeSeconds,
                    TimeEnd = timeEndSeconds,
                    ArrowDirection = arrowDirection,
                    ArrowType = arrowType
                };
                break;
        }
        
        return null;
    }
    
    private ArrowDirection GetArrowDirectionFromCoordinates(int x, int y)
    {
        // В вашем файле все ноты имеют y=192
        // x=64 (левая сторона) -> Down
        // x=192 (правая сторона) -> Up
        // x=256, x=320 - для 4-х кнопок (по необходимости)
        
        if (x == 64)
        {
            return ArrowDirection.Down;
        }
        else if (x == 192)
        {
            return ArrowDirection.Up;
        }
        else if (x == 320)
        {
            return ArrowDirection.Left; // Если нужно для 4-х кнопок
        }
        else if (x == 448)
        {
            return ArrowDirection.Right; // Если нужно для 4-х кнопок
        }
        else
        {
            Debug.LogWarning($"Unknown x coordinate: {x}, using Down as default");
            return ArrowDirection.Down;
        }
    }
    
    private string GetRelativePath(string absolutePath)
    {
        string dataPath = Application.dataPath;
        if (absolutePath.StartsWith(dataPath))
        {
            return "Assets" + absolutePath.Substring(dataPath.Length);
        }
        return absolutePath;
    }
}
#endif