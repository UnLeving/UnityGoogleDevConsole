using System;
using System.IO;
using System.Linq;
using gConsoleAPI;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using static UnityEditor.EditorGUILayout;
using static UnityEngine.GUILayout;

public class LevingGPC : EditorWindow
{
    string packageName;
    string apkBuildName;
    string jsonKey;
    string apkPath;
    FileInfo jsonFileInfo;
    FileInfo apkFileInfo;
    GUIStyle btnStyle;
    int buildType = 1;
    int uploadType;
    int androidArchitecture;
    bool flag;
    string releaseNotes = string.Empty;
    string logs;

    void Init()
    {
        PlayerSettings.Android.androidIsGame = true;
        PlayerSettings.Android.androidTVCompatibility = false;
        PlayerSettings.Android.ARCoreEnabled = false;
        PlayerSettings.Android.buildApkPerCpuArchitecture = false;
        btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.fixedWidth = 180;
    }

    [MenuItem("Window/Leving consol")]
    public static void ShowWindow()
    {
        GetWindow(typeof(LevingGPC));
    }

    void BaseAndroidSettingsFields()
    {
        Label("Base android settings", EditorStyles.boldLabel);
        packageName = TextField("Package Name", Application.identifier);
        PlayerSettings.productName = TextField("Application Name", Application.productName);

        EditorGUI.BeginDisabledGroup(true);
        TextField("Current build", PlayerSettings.Android.bundleVersionCode.ToString());
        TextField("Application identifier", PlayerSettings.applicationIdentifier);
        TextField("Current version", Application.version);
        EditorGUI.EndDisabledGroup();
        apkBuildName = TextField("APK name, .apk", apkBuildName);
        Label("Deploy settings", EditorStyles.boldLabel);
        Label("Select scenes for build:");
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        for (int i = 0; i < scenes.Length; ++i)
        {
            FileInfo fileInfo = new FileInfo(scenes[i].path);
            scenes[i].enabled = ToggleLeft(fileInfo.Name.Replace(".unity", ""), EditorBuildSettings.scenes[i].enabled);
        }

        EditorBuildSettings.scenes = scenes;
    }

    void OnGUI()
    {
        Init();
        BaseAndroidSettingsFields();

        buildType = SelectionGrid(buildType, new[] { "Debug build", "Publish build" }, 2, EditorStyles.toolbarButton);
        if (buildType == 0)
        {
            DebugBuildUI();
        }
        else
            PublishBuildUI();
        TextField("Logs:", logs);
    }

    void PublishBuildUI()
    {
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
        PlayerSettings.Android.keystorePass = PasswordField("Keystore password", PlayerSettings.Android.keystorePass);
        PlayerSettings.Android.keyaliasPass = PasswordField("Alias password", PlayerSettings.Android.keyaliasPass);

        EditorGUILayout.BeginHorizontal();
        if (Button("Select .json credentials file", btnStyle))
        {
            jsonKey = FileDialog("Load json file", "json");
            jsonFileInfo = new FileInfo(jsonKey);
        }
        TextField("", jsonFileInfo?.Name);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (Button("Select .apk file", btnStyle))
        {
            apkPath = FileDialog("Load apk file", "apk");
            apkFileInfo = new FileInfo(apkPath);
        }
        TextField("", apkFileInfo?.Name);
        EditorGUILayout.EndHorizontal();

        Label("Release management:");
        uploadType = SelectionGrid(uploadType, new[] { "Internal", "Alpha", "", /*"Beta",*/ "Artifact library" }, 4, EditorStyles.toolbarButton);
        Label("Release notes (can be empty):");
        releaseNotes = TextArea(releaseNotes, 200, GUILayout.MaxHeight(70f));

        if (jsonKey != string.Empty && SceneCount() != 0 && apkPath != string.Empty
            && PlayerSettings.Android.keystorePass != string.Empty && PlayerSettings.Android.keyaliasPass != string.Empty)
            if (Button("DEPLOY"))
            {
                Logs("build started");
                Build(uploadType);
                Logs("build finished");
            }
    }

    void DebugBuildUI()
    {
        EditorGUILayout.BeginHorizontal();
        PrefixLabel("Android architecture");
        androidArchitecture = SelectionGrid(androidArchitecture, new[] { AndroidArchitecture.ARMv7.ToString(), AndroidArchitecture.X86.ToString() }, 2, EditorStyles.toolbarButton, GUILayout.MaxWidth(100f));
        EditorGUILayout.EndHorizontal();
        if (SceneCount() == 0) return;
        if (Button("Build"))
        {
            BuildPipeline.BuildPlayer(BuildType(AndroidBuildType.Debug, androidArchitecture == 0 ? AndroidArchitecture.ARMv7 : AndroidArchitecture.X86));
        }
    }

    void Build(int uploadType)
    {
        int buildNumber = GetLastBuildNumber();
        if (buildNumber == -1)
            return;

        PlayerSettings.Android.bundleVersionCode = ++buildNumber;

        BuildFile buildReport = BuildPipeline.BuildPlayer(BuildType(AndroidBuildType.Release, AndroidArchitecture.ARMv7)).files.Where(x => x.path.EndsWith(".apk")).FirstOrDefault();
        Logs("deploy started");
        Deploy(uploadType, buildReport.path);
        Logs("deploy finished");
    }

    BuildPlayerOptions BuildType(AndroidBuildType type, AndroidArchitecture architecture)
    {
        BuildPlayerOptions options = new BuildPlayerOptions();
        string[] sc = (from scene in EditorBuildSettings.scenes where scene.enabled == true select scene.path).ToArray();
        options.scenes = sc;
        options.target = BuildTarget.Android;

        switch (type)
        {
            case AndroidBuildType.Debug:
                options.locationPathName = apkBuildName + $"-debug_{architecture}.apk";
                options.options = BuildOptions.AllowDebugging | BuildOptions.Development;
                break;
            case AndroidBuildType.Development:
                break;
            case AndroidBuildType.Release:
                options.locationPathName = apkBuildName + $"-release_{architecture}.apk";
                break;
            default:
                break;
        }

        return options;
    }

    int GetLastBuildNumber() => new GAPublisher(jsonKey, PlayerSettings.productName, packageName, null).RetreiveLastBuildNumber();

    int SceneCount() => (from scene in EditorBuildSettings.scenes where scene.enabled == true select scene).Count();

    void Logs(string str) => logs = str;

    string FileDialog(string fileType, string fileExt) => EditorUtility.OpenFilePanel(fileType, Environment.CurrentDirectory, fileExt);

    void Deploy(int uploadType, string apk)
    {
        gConsoleAPI.Helpers.UploadType type = gConsoleAPI.Helpers.UploadType.INTERNAL;
        switch (uploadType)
        {
            case 0:
                type = gConsoleAPI.Helpers.UploadType.INTERNAL;
                break;
            case 1:
                type = gConsoleAPI.Helpers.UploadType.alpha;
                break;
            case 2:
                //type = gConsoleAPI.Helpers.UploadType.beta;
                break;
            case 3:
                type = gConsoleAPI.Helpers.UploadType.library;
                break;
        }
        GAPublisher publisher = new GAPublisher(jsonKey, Application.productName, packageName, releaseNotes, type, apk);
        publisher.Publish();
    }
}
