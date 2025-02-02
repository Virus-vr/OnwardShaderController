using Onward.CustomContent;
using Onward.CustomContent.Assets;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ShaderGlobalControlWindow : EditorWindow
{
    #region Config
    private const string ShaderBoostKeyword = "_NVBoost_ON";
    private const string ShaderScanlinesKeyword = "_PPEffect_Scanlines";
    #endregion

    #region Setup
    private SharedLevelSettings sharedLevelSettings;
    private readonly Dictionary<NightVisionColor, Texture> lutTextureDict = new();
    #endregion

    #region Variables
    private bool lutStrengthToggle = false;
    private bool lutBoostToggle = false;
    private bool nvScanlines = true;
    private float nvBoostAmount = 1.0f;
    private float preProcessDesaturation = 0.0f;
    private Color preProcessColor = Color.white;
    private NightVisionColor activeNightVisionColor = NightVisionColor.Green;
    #endregion

    #region Initialization
    [MenuItem("Tools/PP_Shader Control")]
    public static void ShowWindow()
    {
        GetWindow<ShaderGlobalControlWindow>("PP_Shader Control");
    }

    private void OnEnable()
    {
        LevelDescriptor levelDescriptor = Object.FindObjectOfType<LevelDescriptor>();
        sharedLevelSettings = levelDescriptor.SharedSettings;

        nvBoostAmount = sharedLevelSettings.SceneNightVisionBoost;
        if (sharedLevelSettings.SceneLUTTextureNV_Green == null)
            FindNightVisionTexture();

        lutTextureDict[NightVisionColor.Green] = sharedLevelSettings.SceneLUTTextureNV_Green;
        lutTextureDict[NightVisionColor.White] = sharedLevelSettings.SceneLUTTextureNV_White;
        lutTextureDict[NightVisionColor.Amber] = sharedLevelSettings.SceneLUTTextureNV_Amber;

        preProcessColor.a = 0;

        void FindNightVisionTexture()
        {
            string[] guids = AssetDatabase.FindAssets("NightVisionLUT_32 t:Texture");
            if (guids.Length == 0)
                Debug.LogWarning("No Night vision texture green has been found.");

            else if (guids.Length > 1)
                Debug.LogWarning("No Night vision texture green was available and multiple assets with the name 'NightVisionLUT_32' have been found.");

            else
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                sharedLevelSettings.SceneLUTTextureNV_Green = AssetDatabase.LoadAssetAtPath<Texture>(path);
            }
        }
    }

    private void OnDisable()
    {
        Shader.SetGlobalFloat("_PreProcessLUTStrength", 0.0f);
        Shader.DisableKeyword(ShaderBoostKeyword);
        Shader.DisableKeyword(ShaderScanlinesKeyword);
        Shader.SetGlobalFloat("_PreProcessDesaturation", 0.0f);
        Shader.SetGlobalColor("_PreProcessColor", new(1,1,1,0));
    }
    #endregion

    private void OnGUI()
    {
        GUILayout.Label("PP_Shader Global Variables", EditorStyles.boldLabel);

        bool changes = false;

        // NVLUT
        NightVisionColor newNightVisionColor = (NightVisionColor)EditorGUILayout.EnumPopup("Night Vision Color", activeNightVisionColor);
        if (newNightVisionColor != activeNightVisionColor)
        {
            activeNightVisionColor = newNightVisionColor;
            Texture nightVisionTexture = lutTextureDict[activeNightVisionColor];
            Shader.SetGlobalTexture("_PreProcessLUT", nightVisionTexture);
            changes = true;
        }

        bool nvLutMissing = false;
        if (lutTextureDict[activeNightVisionColor] == null)
        {
            nvLutMissing = true;
            EditorGUILayout.HelpBox($"Lut texture is null for {activeNightVisionColor}. You will need to change this in your shared settings.", MessageType.Error);
            lutStrengthToggle = false;
            Shader.SetGlobalFloat("_PreProcessLUTStrength", 0.0f);
            changes = true;
        }

        EditorGUI.BeginDisabledGroup(nvLutMissing);
        bool newLutStrengthToggle = EditorGUILayout.Toggle("Night Vision Preview", lutStrengthToggle);
        if (newLutStrengthToggle != lutStrengthToggle)
        {
            lutStrengthToggle = newLutStrengthToggle;
            Shader.SetGlobalFloat("_PreProcessLUTStrength", lutStrengthToggle ? 1.0f : 0.0f);
            if (!newLutStrengthToggle)
            {
                lutBoostToggle = false;
                Shader.DisableKeyword(ShaderBoostKeyword);
            }
            changes = true;
        }
        EditorGUI.EndDisabledGroup();

        // _PPEffect_Scanlines
        bool newNVScanlinese = EditorGUILayout.Toggle("Night Vision Scanlines Preview", nvScanlines);
        if (newNVScanlinese != nvScanlines)
        {
            nvScanlines = newNVScanlinese;
            if (lutBoostToggle)
                Shader.EnableKeyword(ShaderScanlinesKeyword);
            else
                Shader.DisableKeyword(ShaderScanlinesKeyword);
            changes = true;
        }

        // _PreProcessNVBoost
        GUIContent lutBoostContent = new("Night Vision Boost Preview", "Might not function correctly in Onward.");
        bool newLutBoostToggle = EditorGUILayout.Toggle(lutBoostContent, lutBoostToggle);
        if (newLutBoostToggle != lutBoostToggle)
        {
            lutBoostToggle = newLutBoostToggle;
            if (lutBoostToggle)
                Shader.EnableKeyword(ShaderBoostKeyword);
            else
                Shader.DisableKeyword(ShaderBoostKeyword);
            changes = true;
        }

        GUIContent lutBoostAmountContent = new("Night Vision Boost Amount", "Might not function correctly in Onward.");
        float newNVBoostAmount = EditorGUILayout.FloatField(lutBoostAmountContent, nvBoostAmount);
        if (!Mathf.Approximately(newNVBoostAmount, nvBoostAmount))
        {
            nvBoostAmount = newNVBoostAmount;
            sharedLevelSettings.SceneNightVisionBoost = nvBoostAmount;
            Shader.SetGlobalFloat("_PreProcessNVBoostAmount", nvBoostAmount);
            changes = true;
        }


        // _PreProcessColor
        Color newPreProcessColor = EditorGUILayout.ColorField("PreProcess Tint", preProcessColor);
        float newPreprocessColorMix = EditorGUILayout.Slider("PreProcess Color Mix", preProcessColor.a, 0f, 1f);
        if (newPreProcessColor != preProcessColor || newPreprocessColorMix != preProcessColor.a)
        {
            preProcessColor = newPreProcessColor;
            preProcessColor.a = newPreprocessColorMix;
            Shader.SetGlobalColor("_PreProcessColor", preProcessColor);
            changes = true;
        }

        float newPreProcessDesaturation = EditorGUILayout.Slider("PreProcess Desaturation", preProcessDesaturation, 0f, 1f);
        if (newPreProcessDesaturation != preProcessDesaturation)
        {
            preProcessDesaturation = newPreProcessDesaturation;
            Shader.SetGlobalFloat("_PreProcessDesaturation", preProcessDesaturation);
            changes = true;
        }

        if (changes)
            SceneView.RepaintAll();
    }

    private enum NightVisionColor
    {
        Green,
        White,
        Amber
    }
}
