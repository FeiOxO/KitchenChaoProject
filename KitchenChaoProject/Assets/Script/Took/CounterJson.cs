using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>单条柜台布局（与 JSON 字段对应）。rotation 使用欧拉角（度），与 Unity Inspector 一致。</summary>
[Serializable]
public class CounterLayoutJsonEntry
{
    public int counterId;
    public Vector3 position;
    public Vector3 rotation;
}

/// <summary>柜台布局 JSON 根对象（JsonUtility 需要可序列化根类型）。</summary>
[Serializable]
public class CounterLayoutJsonRoot
{
    public CounterLayoutJsonEntry[] entries = Array.Empty<CounterLayoutJsonEntry>();
}

/// <summary>
/// 柜台布局 JSON 读写（静态工具）。
/// 编辑器：<see cref="Application.dataPath"/>/CounterLayouts（即项目 Assets/CounterLayouts，可进版本库）。
/// 正式玩家包：无法写入 Assets，目录落在 <see cref="Application.persistentDataPath"/>/CounterLayouts。
/// </summary>
public static class CounterJson
{
    public const string LayoutSubFolderName = "CounterLayouts";

    private static readonly Regex SceneLayoutFileNameRegex = new Regex(@"^Scene(\d{2})\.json$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>布局 JSON 所在文件夹绝对路径；<paramref name="createIfMissing"/> 为 true 时自动创建。</summary>
    public static string GetLayoutDirectoryPath(bool createIfMissing = false)
    {
#if UNITY_EDITOR
        string dir = Path.Combine(Application.dataPath, LayoutSubFolderName);
#else
        string dir = Path.Combine(Application.persistentDataPath, LayoutSubFolderName);
#endif
        if (createIfMissing && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        return dir;
    }

    /// <summary>将布局写入当前平台布局目录下的 <paramref name="fileName"/>。</summary>
    public static bool SaveLayout(string fileName, CounterLayoutJsonRoot data)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            Debug.LogError("CounterJson.SaveLayout: fileName 为空。");
            return false;
        }

        if (data == null)
            data = new CounterLayoutJsonRoot();

        if (data.entries == null)
            data.entries = Array.Empty<CounterLayoutJsonEntry>();

        try
        {
            string dir = GetLayoutDirectoryPath(true);
            string path = Path.Combine(dir, fileName);
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"CounterJson.SaveLayout 失败: {e.Message}");
            return false;
        }
    }

    /// <summary>从当前平台布局目录读取 <paramref name="fileName"/>。</summary>
    public static bool TryLoadLayout(string fileName, out CounterLayoutJsonRoot data)
    {
        data = new CounterLayoutJsonRoot { entries = Array.Empty<CounterLayoutJsonEntry>() };

        if (string.IsNullOrEmpty(fileName))
            return false;

        string path = Path.Combine(GetLayoutDirectoryPath(false), fileName);
        if (!File.Exists(path))
            return false;

        try
        {
            string json = File.ReadAllText(path);
            CounterLayoutJsonRoot parsed = JsonUtility.FromJson<CounterLayoutJsonRoot>(json);
            if (parsed == null)
                return false;

            if (parsed.entries == null)
                parsed.entries = Array.Empty<CounterLayoutJsonEntry>();

            data = parsed;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"CounterJson.TryLoadLayout 失败: {e.Message}");
            return false;
        }
    }

    /// <summary>统计布局目录下名称符合 Scene01.json … Scene99.json 的文件个数。</summary>
    public static int CountSceneLayoutJsonFiles()
    {
        string dir = GetLayoutDirectoryPath(false);
        if (!Directory.Exists(dir))
            return 0;

        int count = 0;
        foreach (string path in Directory.GetFiles(dir, "*.json"))
        {
            if (SceneLayoutFileNameRegex.IsMatch(Path.GetFileName(path)))
                count++;
        }

        return count;
    }

    /// <summary>选取布局目录中编号最大的 SceneNN.json 文件名（不含路径）。</summary>
    public static bool TryGetLatestSceneLayoutFileName(out string fileName)
    {
        fileName = null;
        string dir = GetLayoutDirectoryPath(false);
        if (!Directory.Exists(dir))
            return false;

        int best = -1;
        foreach (string path in Directory.GetFiles(dir, "*.json"))
        {
            string fn = Path.GetFileName(path);
            Match m = SceneLayoutFileNameRegex.Match(fn);
            if (!m.Success)
                continue;

            int num = int.Parse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture);
            if (num > best)
            {
                best = num;
                fileName = fn;
            }
        }

        return fileName != null;
    }

    /// <summary>下一次保存应使用的文件名：Scene(已有 SceneNN.json 数量 + 1).json。</summary>
    public static string GetNextSceneLayoutFileName()
    {
        int n = CountSceneLayoutJsonFiles() + 1;
        n = Mathf.Clamp(n, 1, 99);
        return $"Scene{n:D2}.json";
    }
}
